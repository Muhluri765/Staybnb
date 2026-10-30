using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;

namespace Staybnb.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Wishlist
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var wishlists = await _context.WishlistItems
                .Include(w => w.Property)
                    .ThenInclude(p => p!.PropertyImages)
                .Include(w => w.Property)
                    .ThenInclude(p => p!.Reviews)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            return View(wishlists);
        }

        // POST: /Wishlist/Toggle/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int propertyId, string? returnUrl = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var existing = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.PropertyId == propertyId);

            if (existing != null)
            {
                _context.WishlistItems.Remove(existing);
                TempData["SuccessMessage"] = "Removed from your Wishlist.";
            }
            else
            {
                _context.WishlistItems.Add(new WishlistItem
                {
                    UserId = userId,
                    PropertyId = propertyId,
                    CreatedAt = DateTime.UtcNow
                });
                TempData["SuccessMessage"] = "Saved to your Wishlist!";
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
