using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.ViewModels.Booking;
using Staybnb.ViewModels.Home;

namespace Staybnb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /
        // Property discovery browsing: only IsActive == true
        public async Task<IActionResult> Index(string? searchCity, decimal? maxPrice, int? amenityId)
        {
            var query = _context.HostProperties
                .Include(p => p.PropertyImages)
                .Include(p => p.Reviews)
                .Include(p => p.Amenities)
                .Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(searchCity))
            {
                var term = searchCity.Trim().ToLower();
                query = query.Where(p => p.Address.ToLower().Contains(term) ||
                                         p.Title.ToLower().Contains(term) ||
                                         p.Description.ToLower().Contains(term));
            }

            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                query = query.Where(p => p.PricePerNight <= maxPrice.Value);
            }

            if (amenityId.HasValue && amenityId.Value > 0)
            {
                query = query.Where(p => p.Amenities.Any(a => a.Id == amenityId.Value));
            }

            var properties = await query.OrderByDescending(p => p.Id).ToListAsync();

            ViewBag.SearchCity = searchCity;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SelectedAmenityId = amenityId;
            ViewBag.Amenities = await _context.Amenities.ToListAsync();

            // Check wishlists for logged-in user
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var wishlistPropertyIds = await _context.WishlistItems
                    .Where(w => w.UserId == currentUserId)
                    .Select(w => w.PropertyId)
                    .ToListAsync();
                ViewBag.WishlistPropertyIds = wishlistPropertyIds;
            }
            else
            {
                ViewBag.WishlistPropertyIds = new System.Collections.Generic.List<int>();
            }

            return View(properties);
        }

        // GET: /Home/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var property = await _context.HostProperties
                .Include(p => p.Host)
                .Include(p => p.PropertyImages)
                .Include(p => p.Amenities)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.Reviewer)
                .Include(p => p.CheckInProcess)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (property == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isWishlisted = false;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                isWishlisted = await _context.WishlistItems
                    .AnyAsync(w => w.UserId == currentUserId && w.PropertyId == id);
            }

            var avgRating = property.Reviews.Any() ? property.Reviews.Average(r => r.Rating) : 0.0;

            var viewModel = new PropertyDetailsViewModel
            {
                Property = property,
                Reviews = property.Reviews.OrderByDescending(r => r.CreatedAt).ToList(),
                AverageRating = Math.Round(avgRating, 1),
                IsInWishlist = isWishlisted,
                BookingForm = new BookingCheckoutViewModel
                {
                    PropertyId = property.Id,
                    PropertyTitle = property.Title,
                    PropertyAddress = property.Address,
                    PricePerNight = property.PricePerNight,
                    HostName = property.Host?.FullName ?? "Host",
                    PropertyImageUrl = property.PropertyImages.FirstOrDefault()?.ImageUrl
                }
            };

            return View(viewModel);
        }

        // POST: /Home/AddReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int propertyId, int rating, string comment)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Challenge();
            }

            if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(comment))
            {
                TempData["ErrorMessage"] = "Please select a valid rating (1-5 stars) and write a comment.";
                return RedirectToAction(nameof(Details), new { id = propertyId });
            }

            var review = new Review
            {
                PropertyId = propertyId,
                ReviewerId = currentUserId,
                Rating = rating,
                Comment = comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);

            // Log activity
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = currentUserId,
                Action = $"Added {rating}-star review for property #{propertyId}",
                ActivityType = "Review",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you! Your review has been published.";
            return RedirectToAction(nameof(Details), new { id = propertyId });
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
