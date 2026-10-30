using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Models.Enums;
using Staybnb.ViewModels.Admin;

namespace Staybnb.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ==========================================
        // 1. ADMIN DASHBOARD & METRICS
        // ==========================================

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var pendingApps = await _context.HostApplications
                .Include(h => h.ApplicationUser)
                .Include(h => h.Property)
                    .ThenInclude(p => p!.PropertyImages)
                .Where(h => h.Status == ApplicationStatus.Pending)
                .OrderByDescending(h => h.AppliedAt)
                .ToListAsync();

            var recentLogs = await _context.ActivityLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .Take(15)
                .ToListAsync();

            var activeListings = await _context.HostProperties
                .Include(p => p.Host)
                .Include(p => p.PropertyImages)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Id)
                .Take(6)
                .ToListAsync();

            var totalRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount);

            // Explicitly query Approved Hosts count via UserManager
            var hostsList = await _userManager.GetUsersInRoleAsync("Host");

            var viewModel = new AdminDashboardViewModel
            {
                PendingApplicationsCount = pendingApps.Count,
                TotalUsersCount = await _userManager.Users.CountAsync(),
                ActivePropertiesCount = await _context.HostProperties.CountAsync(p => p.IsActive),
                TotalPropertiesCount = await _context.HostProperties.CountAsync(),
                ApprovedHostsCount = hostsList.Count,
                TotalBookingsCount = await _context.Bookings.CountAsync(),
                PlatformRevenue = totalRevenue,
                PendingApplications = pendingApps,
                RecentActivityLogs = recentLogs,
                ActiveListings = activeListings
            };

            return View(viewModel);
        }

        // ==========================================
        // 2. HOST APPLICATION REVIEWS (Role Swap Logic)
        // ==========================================

        // GET: /Admin/Applications
        public async Task<IActionResult> Applications(ApplicationStatus? status)
        {
            var query = _context.HostApplications
                .Include(h => h.ApplicationUser)
                .Include(h => h.Property)
                    .ThenInclude(p => p!.PropertyImages)
                .Include(h => h.Property)
                    .ThenInclude(p => p!.Amenities)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(h => h.Status == status.Value);
            }

            var applications = await query.OrderByDescending(h => h.AppliedAt).ToListAsync();
            ViewBag.SelectedStatus = status;

            return View(applications);
        }

        // GET: /Admin/ApplicationDetails/5
        public async Task<IActionResult> ApplicationDetails(int id)
        {
            var app = await _context.HostApplications
                .Include(h => h.ApplicationUser)
                .Include(h => h.Property)
                    .ThenInclude(p => p!.PropertyImages)
                .Include(h => h.Property)
                    .ThenInclude(p => p!.Amenities)
                .Include(h => h.Property)
                    .ThenInclude(p => p!.CheckInProcess)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (app == null)
            {
                return NotFound();
            }

            return View(app);
        }

        // POST: /Admin/ApproveApplication/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveApplication(int id, string? adminNotes)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var application = await _context.HostApplications
                .Include(h => h.ApplicationUser)
                .Include(h => h.Property)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status != ApplicationStatus.Pending)
            {
                TempData["ErrorMessage"] = "This application has already been processed.";
                return RedirectToAction(nameof(Applications));
            }

            var user = application.ApplicationUser;
            if (user == null)
            {
                return BadRequest("Applicant user account not found.");
            }

            // CRITICAL BUSINESS LOGIC:
            // a. Programmatically add "Host" role to the applicant
            if (!await _userManager.IsInRoleAsync(user, "Host"))
            {
                if (!await _roleManager.RoleExistsAsync("Host"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Host"));
                }
                await _userManager.AddToRoleAsync(user, "Host");
            }

            // b. Programmatically REMOVE "Guest" role (strictly Guest OR Host, never both)
            if (await _userManager.IsInRoleAsync(user, "Guest"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Guest");
            }

            // c. Set HostProperty.IsActive = true and HostApplication.Status = Approved
            application.Status = ApplicationStatus.Approved;
            application.AdminNotes = adminNotes ?? "Application verified and approved by Administration.";

            if (application.Property != null)
            {
                application.Property.IsActive = true;
            }

            // Log activity: Approval and Role Swap
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = adminId!,
                Action = $"Approved Host App #{application.Id} for {user.FullName}. Listing '{application.Property?.Title}' active.".Truncate(100),
                ActivityType = "AdminReview",
                CreatedAt = DateTime.UtcNow
            });

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = user.Id,
                Action = "Role transitioned from Guest to Host (Mutual Exclusivity Enforced)".Truncate(100),
                ActivityType = "RoleChange",
                CreatedAt = DateTime.UtcNow
            });

            // Send notification to applicant
            _context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = $"Host App Approved for '{application.Property?.Title}'".Truncate(100),
                Type = "ApplicationApproval",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Host application #{application.Id} approved! {user.FullName} is now upgraded to Host, and property '{application.Property?.Title}' is live.";
            return RedirectToAction(nameof(Applications));
        }

        // POST: /Admin/RejectApplication/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectApplication(int id, string? adminNotes)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var application = await _context.HostApplications
                .Include(h => h.ApplicationUser)
                .Include(h => h.Property)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            application.Status = ApplicationStatus.Rejected;
            application.AdminNotes = adminNotes ?? "Application declined during administrative review.";

            if (application.Property != null)
            {
                application.Property.IsActive = false;
            }

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = adminId!,
                Action = $"Rejected Host Application #{application.Id} for {application.ApplicationUser?.FullName}".Truncate(100),
                ActivityType = "AdminReview",
                CreatedAt = DateTime.UtcNow
            });

            _context.Notifications.Add(new Notification
            {
                UserId = application.ApplicationUserId,
                Title = $"Host Application Declined: {application.AdminNotes}".Truncate(100),
                Type = "ApplicationRejection",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Host application #{application.Id} has been rejected.";
            return RedirectToAction(nameof(Applications));
        }

        // ==========================================
        // 3. SYSTEM AUDIT / ACTIVITY LOG VIEWER
        // ==========================================

        // GET: /Admin/ActivityLogs
        public async Task<IActionResult> ActivityLogs(string? type, string? searchUser)
        {
            var query = _context.ActivityLogs
                .Include(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(a => a.ActivityType == type);
            }

            if (!string.IsNullOrWhiteSpace(searchUser))
            {
                var term = searchUser.Trim().ToLower();
                query = query.Where(a => a.User!.Email!.ToLower().Contains(term) ||
                                         a.User.FirstName.ToLower().Contains(term) ||
                                         a.User.LastName.ToLower().Contains(term));
            }

            var logs = await query.OrderByDescending(a => a.CreatedAt).Take(100).ToListAsync();

            ViewBag.SelectedType = type;
            ViewBag.SearchUser = searchUser;
            ViewBag.Types = await _context.ActivityLogs.Select(a => a.ActivityType).Distinct().ToListAsync();

            return View(logs);
        }

        // ==========================================
        // 4. USER MANAGEMENT (SuperAdmin Only)
        // ==========================================

        // GET: /Admin/Users
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var modelList = new List<UserManagementViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                modelList.Add(new UserManagementViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    Roles = string.Join(", ", roles),
                    IsAdmin = roles.Contains("Admin"),
                    IsSuperAdmin = roles.Contains("SuperAdmin"),
                    IsHost = roles.Contains("Host"),
                    IsGuest = roles.Contains("Guest")
                });
            }

            return View(modelList);
        }

        // POST: /Admin/PromoteToAdmin
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToAdmin(string userId)
        {
            var superAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var targetUser = await _userManager.FindByIdAsync(userId);
            if (targetUser == null)
            {
                return NotFound("Target user not found.");
            }

            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            if (!await _userManager.IsInRoleAsync(targetUser, "Admin"))
            {
                await _userManager.AddToRoleAsync(targetUser, "Admin");

                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = superAdminId!,
                    Action = $"SuperAdmin promoted {targetUser.FullName} ({targetUser.Email}) to Admin role".Truncate(100),
                    ActivityType = "RolePromotion",
                    CreatedAt = DateTime.UtcNow
                });

                _context.Notifications.Add(new Notification
                {
                    UserId = targetUser.Id,
                    Title = "Privilege Update: You have been promoted to Platform Administrator.".Truncate(100),
                    Type = "AdminPromotion",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{targetUser.FullName} has been granted Admin privileges.";
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: /Admin/DemoteFromAdmin
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoteFromAdmin(string userId)
        {
            var superAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var targetUser = await _userManager.FindByIdAsync(userId);
            if (targetUser == null)
            {
                return NotFound("Target user not found.");
            }

            // Cannot demote SuperAdmin
            if (await _userManager.IsInRoleAsync(targetUser, "SuperAdmin"))
            {
                TempData["ErrorMessage"] = "Cannot demote a SuperAdmin.";
                return RedirectToAction(nameof(Users));
            }

            if (await _userManager.IsInRoleAsync(targetUser, "Admin"))
            {
                await _userManager.RemoveFromRoleAsync(targetUser, "Admin");

                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = superAdminId!,
                    Action = $"SuperAdmin demoted {targetUser.FullName} ({targetUser.Email}) from Admin role".Truncate(100),
                    ActivityType = "RoleDemotion",
                    CreatedAt = DateTime.UtcNow
                });

                _context.Notifications.Add(new Notification
                {
                    UserId = targetUser.Id,
                    Title = "Privilege Update: Admin privileges have been removed.".Truncate(100),
                    Type = "AdminDemotion",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Admin privileges removed from {targetUser.FullName}.";
            }

            return RedirectToAction(nameof(Users));
        }
    }
}
