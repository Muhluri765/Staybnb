using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Models.Enums;
using Staybnb.ViewModels.Booking;

namespace Staybnb.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BookingController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // ==========================================
        // 1. BOOKING CHECKOUT & CALCULATION ENGINE
        // ==========================================

        // GET: /Booking/Checkout
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> Checkout(int propertyId, DateTime? checkInDate, DateTime? checkOutDate)
        {
            var property = await _context.HostProperties
                .Include(p => p.Host)
                .Include(p => p.PropertyImages)
                .FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive);

            if (property == null)
            {
                return NotFound("The requested property is not currently active or available.");
            }

            var inDate = checkInDate ?? DateTime.Today.AddDays(1);
            var outDate = checkOutDate ?? DateTime.Today.AddDays(3);

            if (outDate <= inDate)
            {
                outDate = inDate.AddDays(1);
            }

            var model = new BookingCheckoutViewModel
            {
                PropertyId = property.Id,
                PropertyTitle = property.Title,
                PropertyAddress = property.Address,
                PricePerNight = property.PricePerNight,
                HostName = property.Host?.FullName ?? "Host",
                PropertyImageUrl = property.PropertyImages.FirstOrDefault()?.ImageUrl,
                CheckInDate = inDate,
                CheckOutDate = outDate,
                CleaningFee = 350.00m
            };

            return View(model);
        }

        // POST: /Booking/ConfirmBooking
        [HttpPost]
        [Authorize(Roles = "Guest")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(BookingCheckoutViewModel model)
        {
            var guestId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(guestId))
            {
                return Challenge();
            }

            var property = await _context.HostProperties
                .Include(p => p.CheckInProcess)
                .Include(p => p.Host)
                .FirstOrDefaultAsync(p => p.Id == model.PropertyId && p.IsActive);

            if (property == null)
            {
                return NotFound("Selected property was not found or is inactive.");
            }

            if (model.CheckOutDate <= model.CheckInDate)
            {
                ModelState.AddModelError("CheckOutDate", "Check-out date must be after check-in date.");
            }

            if (model.CheckInDate < DateTime.Today)
            {
                ModelState.AddModelError("CheckInDate", "Check-in date cannot be in the past.");
            }

            // CRITICAL BUSINESS LOGIC: Pricing Formula
            // TotalPrice = (PricePerNight * Nights) + CleaningFee + ServiceFee;
            int nights = (model.CheckOutDate.Date - model.CheckInDate.Date).Days;
            if (nights <= 0) nights = 1;

            decimal roomTotal = property.PricePerNight * nights;
            decimal cleaningFee = 350.00m;
            decimal serviceFee = Math.Round(roomTotal * 0.12m, 2);
            decimal calculatedTotalPrice = roomTotal + cleaningFee + serviceFee;

            if (!ModelState.IsValid)
            {
                model.PropertyTitle = property.Title;
                model.PropertyAddress = property.Address;
                model.PricePerNight = property.PricePerNight;
                model.HostName = property.Host?.FullName ?? "Host";
                return View("Checkout", model);
            }

            // 1. Create Booking
            var booking = new Booking
            {
                PropertyId = property.Id,
                GuestId = guestId,
                CheckInDate = model.CheckInDate,
                CheckOutDate = model.CheckOutDate,
                TotalPrice = calculatedTotalPrice,
                Status = BookingStatus.Pending
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // 2. Create Payment (1:1 with Booking)
            var payment = new Payment
            {
                BookingId = booking.Id,
                Amount = calculatedTotalPrice,
                Status = PaymentStatus.Completed, // Simulated card payment successful at checkout
                CreatedAt = DateTime.UtcNow
            };
            _context.Payments.Add(payment);

            // 3. Link GuestCheckIn record if Property has CheckInProcess
            if (property.CheckInProcess != null)
            {
                var checkIn = new GuestCheckIn
                {
                    BookingId = booking.Id,
                    CheckInProcessId = property.CheckInProcess.Id,
                    Status = CheckInStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                _context.GuestCheckIns.Add(checkIn);
            }

            // 4. ActivityLog Entries
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = guestId,
                Action = $"Created Booking #{booking.Id} for '{property.Title}' (Total: R{calculatedTotalPrice:N2})",
                ActivityType = "Booking",
                CreatedAt = DateTime.UtcNow
            });

            // 5. Notifications for Host & Guest
            _context.Notifications.Add(new Notification
            {
                UserId = property.HostId,
                Title = $"New Booking Request #{booking.Id} received for '{property.Title}'.",
                Type = "BookingRequest",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            _context.Notifications.Add(new Notification
            {
                UserId = guestId,
                Title = $"Booking #{booking.Id} submitted and payment processed. Awaiting Host confirmation.",
                Type = "BookingSubmitted",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Confirmation), new { id = booking.Id });
        }

        // GET: /Booking/Confirmation/5
        public async Task<IActionResult> Confirmation(int id)
        {
            var guestId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.Property)
                    .ThenInclude(p => p!.Host)
                .Include(b => b.Payment)
                .Include(b => b.GuestCheckIn)
                .FirstOrDefaultAsync(b => b.Id == id && b.GuestId == guestId);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: /Booking/MyBookings
        public async Task<IActionResult> MyBookings()
        {
            var guestId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookings = await _context.Bookings
                .Include(b => b.Property)
                    .ThenInclude(p => p!.PropertyImages)
                .Include(b => b.Property)
                    .ThenInclude(p => p!.Host)
                .Include(b => b.Payment)
                .Include(b => b.GuestCheckIn)
                    .ThenInclude(g => g!.GuestDocuments)
                .Where(b => b.GuestId == guestId)
                .OrderByDescending(b => b.CheckInDate)
                .ToListAsync();

            return View(bookings);
        }

        // ==========================================
        // 2. GUEST CHECK-IN ENGINE (Document Upload)
        // ==========================================

        // GET: /Booking/CheckIn/5
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> CheckIn(int bookingId)
        {
            var guestId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.Property)
                    .ThenInclude(p => p!.Host)
                .Include(b => b.Property)
                    .ThenInclude(p => p!.CheckInProcess)
                .Include(b => b.GuestCheckIn)
                    .ThenInclude(g => g!.GuestDocuments)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == guestId);

            if (booking == null)
            {
                return NotFound("Booking not found or unauthorized.");
            }

            // CRITICAL BUSINESS LOGIC: Guest Check-in Engine
            // "When a booking is Approved, the Guest gains access to a check-in workflow"
            if (booking.Status != BookingStatus.Approved &&
                booking.Status != BookingStatus.CheckedIn &&
                booking.Status != BookingStatus.Completed)
            {
                TempData["ErrorMessage"] = "Check-in workflow is only accessible after your booking has been Approved by the host.";
                return RedirectToAction(nameof(MyBookings));
            }

            // Ensure GuestCheckIn exists
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
                await _context.SaveChangesAsync();
                booking.GuestCheckIn = checkIn;
            }

            var model = new GuestCheckInViewModel
            {
                BookingId = booking.Id,
                PropertyTitle = booking.Property?.Title ?? "Property",
                PropertyAddress = booking.Property?.Address ?? "",
                HostName = booking.Property?.Host?.FullName ?? "Host",
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                Status = booking.GuestCheckIn?.Status ?? CheckInStatus.Pending,
                HostRemarks = booking.GuestCheckIn?.HostRemarks,
                ProcessTitle = booking.Property?.CheckInProcess?.Title ?? "Check-in Instructions",
                ProcessStepsJson = booking.Property?.CheckInProcess?.StepsJson ?? "[]",
                UploadedDocuments = booking.GuestCheckIn?.GuestDocuments.ToList() ?? new System.Collections.Generic.List<GuestDocument>()
            };

            return View(model);
        }

        // POST: /Booking/UploadDocument
        [HttpPost]
        [Authorize(Roles = "Guest")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(GuestCheckInViewModel model)
        {
            var guestId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.Property)
                .Include(b => b.GuestCheckIn)
                    .ThenInclude(g => g!.GuestDocuments)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId && b.GuestId == guestId);

            if (booking == null || booking.GuestCheckIn == null)
            {
                return NotFound("Booking or check-in process was not found.");
            }

            if (model.DocumentFile == null || model.DocumentFile.Length == 0)
            {
                ModelState.AddModelError("DocumentFile", "Please select a valid document file (ID or Passport) to upload.");
            }

            if (!ModelState.IsValid)
            {
                model.PropertyTitle = booking.Property?.Title ?? "Property";
                model.PropertyAddress = booking.Property?.Address ?? "";
                model.UploadedDocuments = booking.GuestCheckIn.GuestDocuments.ToList();
                return View("CheckIn", model);
            }

            // Save document locally to wwwroot/uploads/documents/
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "documents");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.DocumentFile!.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.DocumentFile.CopyToAsync(stream);
            }

            // Create GuestDocument entity
            var document = new GuestDocument
            {
                GuestCheckInId = booking.GuestCheckIn.Id,
                DocumentType = model.DocumentType.Trim(),
                FileName = $"/uploads/documents/{uniqueFileName}",
                Status = DocumentStatus.Pending,
                UploadedAt = DateTime.UtcNow
            };

            _context.GuestDocuments.Add(document);

            // Update CheckInStatus to InReview
            booking.GuestCheckIn.Status = CheckInStatus.InReview;

            // ActivityLog
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = guestId!,
                Action = $"Uploaded check-in verification document ({model.DocumentType}) for Booking #{booking.Id}",
                ActivityType = "CheckInDocument",
                CreatedAt = DateTime.UtcNow
            });

            // Notify Host
            if (booking.Property != null)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = booking.Property.HostId,
                    Title = $"Guest has submitted verification documents for Booking #{booking.Id}. Review needed.",
                    Type = "CheckInReview",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your document has been securely submitted! Your host will review and verify your check-in.";
            return RedirectToAction(nameof(CheckIn), new { bookingId = booking.Id });
        }
    }
}
