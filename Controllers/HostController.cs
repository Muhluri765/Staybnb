using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Models.Enums;
using Staybnb.ViewModels.Host;

namespace Staybnb.Controllers
{
    public class HostController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HostController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // ==========================================
        // 1. BECOME A HOST WORKFLOW (Guest Access)
        // ==========================================

        // GET: /Host/BecomeHost
        [Authorize(Roles = "Guest,Host")]
        public async Task<IActionResult> BecomeHost()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isHost = User.IsInRole("Host");
            if (isHost)
            {
                // Already host, redirect to dashboard
                return RedirectToAction(nameof(Dashboard));
            }

            // Check if there is an existing pending application
            var existingApp = await _context.HostApplications
                .Include(h => h.Property)
                .FirstOrDefaultAsync(h => h.ApplicationUserId == userId && h.Status == ApplicationStatus.Pending);

            if (existingApp != null)
            {
                ViewBag.PendingApplication = existingApp;
                return View("ApplicationPending", existingApp);
            }

            var model = new BecomeHostViewModel
            {
                AvailableAmenities = await _context.Amenities.ToListAsync()
            };

            return View(model);
        }

        // POST: /Host/BecomeHost
        [HttpPost]
        [Authorize(Roles = "Guest,Host")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BecomeHost(BecomeHostViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Critical Business Logic Rule: Server-side validation MUST reject submissions that lack an uploaded image.
            if (model.ImageFiles == null || !model.ImageFiles.Any() || model.ImageFiles.All(f => f == null || f.Length == 0))
            {
                ModelState.AddModelError("ImageFiles", "Server validation error: At least one mandatory property photo is required to submit a hosting application.");
            }

            if (!ModelState.IsValid)
            {
                model.AvailableAmenities = await _context.Amenities.ToListAsync();
                return View(model);
            }

            // 1. Create Initial Inactive HostProperty
            var property = new HostProperty
            {
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                PricePerNight = model.PricePerNight,
                Address = model.Address.Trim(),
                HostId = userId,
                IsActive = false // Initially inactive until approved by Admin
            };

            // Associate amenities
            if (model.SelectedAmenityIds != null && model.SelectedAmenityIds.Any())
            {
                var selectedAmenities = await _context.Amenities
                    .Where(a => model.SelectedAmenityIds.Contains(a.Id))
                    .ToListAsync();
                property.Amenities = selectedAmenities;
            }

            // 2. Save uploaded photos locally to wwwroot/uploads/properties/
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "properties");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            foreach (var file in model.ImageFiles!)
            {
                if (file != null && file.Length > 0)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Store relative URL in database
                    property.PropertyImages.Add(new PropertyImage
                    {
                        ImageUrl = $"/uploads/properties/{uniqueFileName}"
                    });
                }
            }

            // 3. Create Default CheckInProcess for the property
            var defaultSteps = new[]
            {
                new { step = 1, title = "Identity Verification", instruction = "Upload your government-issued ID or passport on Staybnb before arrival." },
                new { step = 2, title = "Keycard / Key Collection", instruction = "Check in directly with host or retrieve keys from property lockbox." }
            };

            property.CheckInProcess = new CheckInProcess
            {
                Title = $"{property.Title} Check-in Guide",
                StepsJson = JsonSerializer.Serialize(defaultSteps),
                CreatedAt = DateTime.UtcNow
            };

            _context.HostProperties.Add(property);
            await _context.SaveChangesAsync();

            // 4. Create HostApplication
            var application = new HostApplication
            {
                ApplicationUserId = userId,
                PropertyId = property.Id,
                Status = ApplicationStatus.Pending,
                AppliedAt = DateTime.UtcNow
            };
            _context.HostApplications.Add(application);

            // 5. ActivityLog & Notification
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = $"Submitted Host Application for listing '{property.Title}'",
                ActivityType = "HostApplication",
                CreatedAt = DateTime.UtcNow
            });

            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = "Host Application Submitted: Under Review by Admins",
                Type = "Application",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your host application has been successfully submitted! An administrator will review your property shortly.";
            return RedirectToAction(nameof(BecomeHost));
        }

        // ==========================================
        // 2. HOST DASHBOARD & MANAGEMENT (Host Role)
        // ==========================================

        // GET: /Host/Dashboard
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var properties = await _context.HostProperties
                .Include(p => p.PropertyImages)
                .Include(p => p.Bookings)
                    .ThenInclude(b => b.Payment)
                .Include(p => p.Bookings)
                    .ThenInclude(b => b.Guest)
                .Include(p => p.CheckInProcess)
                .Where(p => p.HostId == userId)
                .ToListAsync();

            var propertyIds = properties.Select(p => p.Id).ToList();

            var allBookings = await _context.Bookings
                .Include(b => b.Property)
                .Include(b => b.Guest)
                .Include(b => b.Payment)
                .Include(b => b.GuestCheckIn)
                    .ThenInclude(g => g!.GuestDocuments)
                .Where(b => propertyIds.Contains(b.PropertyId))
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            var pendingCheckIns = await _context.GuestCheckIns
                .Include(g => g.Booking)
                    .ThenInclude(b => b!.Property)
                .Include(g => g.Booking)
                    .ThenInclude(b => b!.Guest)
                .Include(g => g.GuestDocuments)
                .Include(g => g.CheckInProcess)
                .Where(g => propertyIds.Contains(g.Booking!.PropertyId) && g.Status == CheckInStatus.InReview)
                .ToListAsync();

            var completedPayments = allBookings
                .Where(b => b.Payment != null && b.Payment.Status == PaymentStatus.Completed)
                .Sum(b => b.TotalPrice);

            var viewModel = new HostDashboardViewModel
            {
                Properties = properties,
                PendingBookings = allBookings.Where(b => b.Status == BookingStatus.Pending).ToList(),
                ConfirmedBookings = allBookings.Where(b => b.Status == BookingStatus.Approved || b.Status == BookingStatus.CheckedIn).ToList(),
                PendingCheckIns = pendingCheckIns,
                TotalEarnings = completedPayments,
                TotalBookingsCount = allBookings.Count,
                ActiveListingsCount = properties.Count(p => p.IsActive)
            };

            return View(viewModel);
        }

        // GET: /Host/Properties
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> Properties()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var properties = await _context.HostProperties
                .Include(p => p.PropertyImages)
                .Include(p => p.Amenities)
                .Include(p => p.CheckInProcess)
                .Where(p => p.HostId == userId)
                .ToListAsync();

            return View(properties);
        }

        // POST: /Host/ToggleActive/5
        [HttpPost]
        [Authorize(Roles = "Host")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var property = await _context.HostProperties.FirstOrDefaultAsync(p => p.Id == id && p.HostId == userId);
            if (property == null)
            {
                return NotFound();
            }

            property.IsActive = !property.IsActive;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId!,
                Action = $"Toggled listing '{property.Title}' to {(property.IsActive ? "Active" : "Inactive")}",
                ActivityType = "Listing",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Listing status updated to {(property.IsActive ? "Active (Visible in search)" : "Inactive (Hidden from search)")}.";
            return RedirectToAction(nameof(Properties));
        }

        // GET: /Host/Bookings
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> Bookings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var myPropertyIds = await _context.HostProperties
                .Where(p => p.HostId == userId)
                .Select(p => p.Id)
                .ToListAsync();

            var bookings = await _context.Bookings
                .Include(b => b.Property)
                .Include(b => b.Guest)
                .Include(b => b.Payment)
                .Include(b => b.GuestCheckIn)
                    .ThenInclude(g => g!.GuestDocuments)
                .Where(b => myPropertyIds.Contains(b.PropertyId))
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            return View(bookings);
        }

        // POST: /Host/ApproveBooking/5
        [HttpPost]
        [Authorize(Roles = "Host")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveBooking(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.Property)
                    .ThenInclude(p => p!.CheckInProcess)
                .Include(b => b.Guest)
                .Include(b => b.GuestCheckIn)
                .FirstOrDefaultAsync(b => b.Id == id && b.Property!.HostId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = BookingStatus.Approved;

            // Ensure GuestCheckIn record exists so the guest can complete check-in
            if (booking.GuestCheckIn == null && booking.Property?.CheckInProcess != null)
            {
                var checkIn = new GuestCheckIn
                {
                    BookingId = booking.Id,
                    CheckInProcessId = booking.Property.CheckInProcess.Id,
                    Status = CheckInStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                _context.GuestCheckIns.Add(checkIn);
            }

            // Notification for Guest
            _context.Notifications.Add(new Notification
            {
                UserId = booking.GuestId,
                Title = $"Booking Approved! Your stay at '{booking.Property?.Title}' is confirmed.",
                Type = "Booking",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            // ActivityLog
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId!,
                Action = $"Approved Booking #{booking.Id} for {booking.Guest?.FullName}",
                ActivityType = "Booking",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Booking #{booking.Id} has been Approved.";
            return RedirectToAction(nameof(Bookings));
        }

        // POST: /Host/RejectBooking/5
        [HttpPost]
        [Authorize(Roles = "Host")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.Property)
                .Include(b => b.Guest)
                .FirstOrDefaultAsync(b => b.Id == id && b.Property!.HostId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = BookingStatus.Rejected;

            // Notification for Guest
            _context.Notifications.Add(new Notification
            {
                UserId = booking.GuestId,
                Title = $"Booking Request Declined for '{booking.Property?.Title}'.",
                Type = "Booking",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            // ActivityLog
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId!,
                Action = $"Declined Booking #{booking.Id}",
                ActivityType = "Booking",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Booking #{booking.Id} was rejected.";
            return RedirectToAction(nameof(Bookings));
        }

        // ==========================================
        // 3. CHECK-IN CONFIGURATION & VERIFICATION
        // ==========================================

        // GET: /Host/ConfigureCheckIn/5
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> ConfigureCheckIn(int propertyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var property = await _context.HostProperties
                .Include(p => p.CheckInProcess)
                .FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == userId);

            if (property == null)
            {
                return NotFound();
            }

            var model = new CheckInProcessViewModel
            {
                PropertyId = property.Id,
                PropertyTitle = property.Title,
                Title = property.CheckInProcess?.Title ?? $"{property.Title} Check-in Guide",
                StepsJson = property.CheckInProcess?.StepsJson ?? "[]"
            };

            return View(model);
        }

        // POST: /Host/ConfigureCheckIn
        [HttpPost]
        [Authorize(Roles = "Host")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigureCheckIn(CheckInProcessViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var property = await _context.HostProperties
                .Include(p => p.CheckInProcess)
                .FirstOrDefaultAsync(p => p.Id == model.PropertyId && p.HostId == userId);

            if (property == null)
            {
                return NotFound();
            }

            if (property.CheckInProcess == null)
            {
                property.CheckInProcess = new CheckInProcess
                {
                    PropertyId = property.Id,
                    Title = model.Title.Trim(),
                    StepsJson = model.StepsJson.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.CheckInProcesses.Add(property.CheckInProcess);
            }
            else
            {
                property.CheckInProcess.Title = model.Title.Trim();
                property.CheckInProcess.StepsJson = model.StepsJson.Trim();
            }

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId!,
                Action = $"Updated Check-in process for '{property.Title}'",
                ActivityType = "CheckIn",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Check-in instructions and steps have been saved successfully!";
            return RedirectToAction(nameof(Properties));
        }

        // GET: /Host/VerifyCheckIn/5
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> VerifyCheckIn(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var checkIn = await _context.GuestCheckIns
                .Include(g => g.Booking)
                    .ThenInclude(b => b!.Property)
                .Include(g => g.Booking)
                    .ThenInclude(b => b!.Guest)
                .Include(g => g.GuestDocuments)
                .Include(g => g.CheckInProcess)
                .FirstOrDefaultAsync(g => g.Id == id && g.Booking!.Property!.HostId == userId);

            if (checkIn == null)
            {
                return NotFound();
            }

            return View(checkIn);
        }

        // POST: /Host/ApproveGuestCheckIn/5
        [HttpPost]
        [Authorize(Roles = "Host")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveGuestCheckIn(int id, string? remarks)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var checkIn = await _context.GuestCheckIns
                .Include(g => g.Booking)
                    .ThenInclude(b => b!.Property)
                .Include(g => g.Booking)
                    .ThenInclude(b => b!.Guest)
                .Include(g => g.GuestDocuments)
                .FirstOrDefaultAsync(g => g.Id == id && g.Booking!.Property!.HostId == userId);

            if (checkIn == null)
            {
                return NotFound();
            }

            checkIn.Status = CheckInStatus.Approved;
            checkIn.HostRemarks = remarks ?? "ID Verification approved by Host.";

            // Update all attached documents to Verified
            foreach (var doc in checkIn.GuestDocuments)
            {
                doc.Status = DocumentStatus.Verified;
            }

            // Also mark booking as CheckedIn
            if (checkIn.Booking != null)
            {
                checkIn.Booking.Status = BookingStatus.CheckedIn;
            }

            // Notification to Guest
            _context.Notifications.Add(new Notification
            {
                UserId = checkIn.Booking!.GuestId,
                Title = $"Check-in Verified & Approved! Welcome to '{checkIn.Booking.Property?.Title}'",
                Type = "CheckIn",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            // ActivityLog
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId!,
                Action = $"Verified ID and Approved Check-in #{checkIn.Id} for {checkIn.Booking.Guest?.FullName}",
                ActivityType = "CheckIn",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Guest check-in and identity documents verified and approved!";
            return RedirectToAction(nameof(Dashboard));
        }

        // POST: /Host/RejectGuestCheckIn/5
        [HttpPost]
        [Authorize(Roles = "Host")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectGuestCheckIn(int id, string? remarks)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var checkIn = await _context.GuestCheckIns
                .Include(g => g.Booking)
                    .ThenInclude(b => b!.Property)
                .Include(g => g.Booking)
                    .ThenInclude(b => b!.Guest)
                .Include(g => g.GuestDocuments)
                .FirstOrDefaultAsync(g => g.Id == id && g.Booking!.Property!.HostId == userId);

            if (checkIn == null)
            {
                return NotFound();
            }

            checkIn.Status = CheckInStatus.Rejected;
            checkIn.HostRemarks = remarks ?? "ID document could not be verified. Please re-upload a clear copy.";

            foreach (var doc in checkIn.GuestDocuments)
            {
                doc.Status = DocumentStatus.Rejected;
            }

            // Notification to Guest
            _context.Notifications.Add(new Notification
            {
                UserId = checkIn.Booking!.GuestId,
                Title = $"Check-in Verification Rejected: {checkIn.HostRemarks}",
                Type = "CheckIn",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId!,
                Action = $"Rejected Check-in #{checkIn.Id} for {checkIn.Booking.Guest?.FullName}",
                ActivityType = "CheckIn",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Check-in document was marked as rejected. Guest has been notified to re-submit.";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
