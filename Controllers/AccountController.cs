using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.ViewModels.Account;

namespace Staybnb.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Business Logic Rule: All newly registered users are automatically added to the "Guest" role
                    if (!await _roleManager.RoleExistsAsync("Guest"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Guest"));
                    }
                    await _userManager.AddToRoleAsync(user, "Guest");

                    // Log activity
                    _context.ActivityLogs.Add(new ActivityLog
                    {
                        UserId = user.Id,
                        Action = "User Registered as Guest",
                        ActivityType = "Identity",
                        CreatedAt = DateTime.UtcNow
                    });

                    // Send welcome notification
                    _context.Notifications.Add(new Notification
                    {
                        UserId = user.Id,
                        Title = "Welcome to Staybnb!",
                        Type = "Welcome",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    });

                    await _context.SaveChangesAsync();

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToLocal(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(
                        model.Email,
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        // Helper method / audit requirement: Log every login into ActivityLog
                        _context.ActivityLogs.Add(new ActivityLog
                        {
                            UserId = user.Id,
                            Action = "User Logged In",
                            ActivityType = "Auth",
                            CreatedAt = DateTime.UtcNow
                        });
                        await _context.SaveChangesAsync();

                        // Redirect based on role for smooth UX
                        var roles = await _userManager.GetRolesAsync(user);
                        if (roles.Contains("SuperAdmin") || roles.Contains("Admin"))
                        {
                            return RedirectToAction("Dashboard", "Admin");
                        }
                        else if (roles.Contains("Host"))
                        {
                            return RedirectToAction("Dashboard", "Host");
                        }

                        return RedirectToLocal(returnUrl);
                    }
                }

                ModelState.AddModelError(string.Empty, "Invalid login credentials. Please check your email and password.");
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userId,
                    Action = "User Logged Out",
                    ActivityType = "Auth",
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
