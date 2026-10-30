using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Staybnb.Models;
using Staybnb.Models.Enums;

namespace Staybnb.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Ensure Database exists
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            string[] roles = new[] { "SuperAdmin", "Admin", "Host", "Guest" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed SuperAdmin
            var superAdminEmail = "admin@staybnb.com";
            var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
            if (superAdmin == null)
            {
                superAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    FirstName = "Super",
                    LastName = "Admin",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(superAdmin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                    await userManager.AddToRoleAsync(superAdmin, "Admin");

                    context.ActivityLogs.Add(new ActivityLog
                    {
                        UserId = superAdmin.Id,
                        Action = "System Initialized",
                        ActivityType = "System",
                        CreatedAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
            }

            // 3. Seed Sample Host
            var hostEmail = "host@staybnb.com";
            var hostUser = await userManager.FindByEmailAsync(hostEmail);
            if (hostUser == null)
            {
                hostUser = new ApplicationUser
                {
                    UserName = hostEmail,
                    Email = hostEmail,
                    FirstName = "Thabo",
                    LastName = "Molefe",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                };
                var result = await userManager.CreateAsync(hostUser, "Host@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(hostUser, "Host");
                }
            }

            // 4. Seed Sample Guest
            var guestEmail = "guest@staybnb.com";
            var guestUser = await userManager.FindByEmailAsync(guestEmail);
            if (guestUser == null)
            {
                guestUser = new ApplicationUser
                {
                    UserName = guestEmail,
                    Email = guestEmail,
                    FirstName = "Lerato",
                    LastName = "Khumalo",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-1)
                };
                var result = await userManager.CreateAsync(guestUser, "Guest@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(guestUser, "Guest");
                }
            }

            // 5. Seed Amenities
            if (!await context.Amenities.AnyAsync())
            {
                var amenities = new List<Amenity>
                {
                    new Amenity { Name = "Fast Wi-Fi", IconClass = "bi-wifi" },
                    new Amenity { Name = "Swimming Pool", IconClass = "bi-water" },
                    new Amenity { Name = "Free Parking", IconClass = "bi-car-front" },
                    new Amenity { Name = "Air Conditioning", IconClass = "bi-snow" },
                    new Amenity { Name = "Chef's Kitchen", IconClass = "bi-cup-hot" },
                    new Amenity { Name = "Dedicated Workspace", IconClass = "bi-laptop" },
                    new Amenity { Name = "HD Smart TV", IconClass = "bi-tv" },
                    new Amenity { Name = "Washer & Dryer", IconClass = "bi-shield-check" },
                    new Amenity { Name = "Patio & Braai Area", IconClass = "bi-fire" },
                    new Amenity { Name = "Sea View", IconClass = "bi-binoculars" }
                };
                await context.Amenities.AddRangeAsync(amenities);
                await context.SaveChangesAsync();
            }

            // 6. Seed Properties & Listings
            if (!await context.HostProperties.AnyAsync() && hostUser != null)
            {
                var allAmenities = await context.Amenities.ToListAsync();

                // Property 1: Camps Bay Luxury Villa
                var property1 = new HostProperty
                {
                    Title = "Modern Camps Bay Sunset Villa",
                    Description = "Spectacular architectural masterpiece overlooking Camps Bay beach and the Twelve Apostles mountain range. Features an infinity pool, state-of-the-art kitchen, and expansive outdoor terrace for unforgettable sunsets.",
                    PricePerNight = 4500.00m,
                    Address = "Victoria Road, Camps Bay, Cape Town, 8005",
                    HostId = hostUser.Id,
                    IsActive = true,
                    Amenities = allAmenities.Where(a => new[] { "Fast Wi-Fi", "Swimming Pool", "Air Conditioning", "Chef's Kitchen", "Sea View" }.Contains(a.Name)).ToList(),
                    PropertyImages = new List<PropertyImage>
                    {
                        new PropertyImage { ImageUrl = "https://images.unsplash.com/photo-1580587771525-78b9dba3b914?auto=format&fit=crop&w=1200&q=80" },
                        new PropertyImage { ImageUrl = "https://images.unsplash.com/photo-1512917774080-9991f1c4c750?auto=format&fit=crop&w=1200&q=80" },
                        new PropertyImage { ImageUrl = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1200&q=80" }
                    }
                };

                // CheckInProcess for Property 1
                var steps1 = new[]
                {
                    new { step = 1, title = "Security Check", instruction = "Show your reservation confirmation and ID to the 24/7 security booth at the main gate." },
                    new { step = 2, title = "Smart Lock", instruction = "Use digital access keypad code 4921# on the front door to unlock." },
                    new { step = 3, title = "Welcome Packet", instruction = "Find the high-speed Wi-Fi details and house guide on the kitchen island tablet." }
                };
                property1.CheckInProcess = new CheckInProcess
                {
                    Title = "Camps Bay Sunset Villa Self Check-in",
                    StepsJson = JsonSerializer.Serialize(steps1),
                    CreatedAt = DateTime.UtcNow
                };

                // Property 2: Sandton Executive Penthouse
                var property2 = new HostProperty
                {
                    Title = "Executive Penthouse in Sandton CBD",
                    Description = "High-rise penthouse situated in Africa's richest square mile. Walking distance to Sandton City and the Gautrain station. Features a dedicated work office, panoramic city views, and gym access.",
                    PricePerNight = 2200.00m,
                    Address = "5th Street, Sandton, Johannesburg, 2196",
                    HostId = hostUser.Id,
                    IsActive = true,
                    Amenities = allAmenities.Where(a => new[] { "Fast Wi-Fi", "Dedicated Workspace", "Free Parking", "HD Smart TV", "Air Conditioning" }.Contains(a.Name)).ToList(),
                    PropertyImages = new List<PropertyImage>
                    {
                        new PropertyImage { ImageUrl = "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80" },
                        new PropertyImage { ImageUrl = "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1200&q=80" }
                    }
                };

                var steps2 = new[]
                {
                    new { step = 1, title = "Concierge Welcome", instruction = "Check in with the lobby concierge on the ground floor; they will hand over your RFID lift tag." },
                    new { step = 2, title = "Penthouse Keycard", instruction = "Tap tag on the 24th floor elevator reader and door reader #2401." }
                };
                property2.CheckInProcess = new CheckInProcess
                {
                    Title = "Sandton Penthouse Concierge Check-in",
                    StepsJson = JsonSerializer.Serialize(steps2),
                    CreatedAt = DateTime.UtcNow
                };

                // Property 3: Umhlanga Rocks Coastal Haven
                var property3 = new HostProperty
                {
                    Title = "Luxury Oceanfront Suite in Umhlanga",
                    Description = "Direct beach access with panoramic Indian Ocean views. Relax on the private balcony with ocean breeze, walk along the Umhlanga Promenade, or dine at world-class nearby restaurants.",
                    PricePerNight = 2850.00m,
                    Address = "Lagoon Drive, Umhlanga Rocks, Durban, 4320",
                    HostId = hostUser.Id,
                    IsActive = true,
                    Amenities = allAmenities.Where(a => new[] { "Sea View", "Swimming Pool", "Fast Wi-Fi", "Patio & Braai Area", "Free Parking" }.Contains(a.Name)).ToList(),
                    PropertyImages = new List<PropertyImage>
                    {
                        new PropertyImage { ImageUrl = "https://images.unsplash.com/photo-1571896349842-33c89424de2d?auto=format&fit=crop&w=1200&q=80" },
                        new PropertyImage { ImageUrl = "https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?auto=format&fit=crop&w=1200&q=80" }
                    }
                };

                var steps3 = new[]
                {
                    new { step = 1, title = "Lockbox Entry", instruction = "Locate lockbox 7 beside the main entrance door. Enter code 7733 to retrieve physical keys." },
                    new { step = 2, title = "Underground Parking", instruction = "Park in reserved bay #32 in the secure basement." }
                };
                property3.CheckInProcess = new CheckInProcess
                {
                    Title = "Umhlanga Coastal Suite Key Check-in",
                    StepsJson = JsonSerializer.Serialize(steps3),
                    CreatedAt = DateTime.UtcNow
                };

                // Property 4: Stellenbosch Wine Country Cottage
                var property4 = new HostProperty
                {
                    Title = "Charming Cape Dutch Cottage in Stellenbosch",
                    Description = "Nestled among heritage vineyards and oak trees. Enjoy private wine tastings, cozy fireplace evenings, and authentic Cape hospitality. Ideal romantic retreat or weekend getaway.",
                    PricePerNight = 1950.00m,
                    Address = "Helshoogte Pass, Stellenbosch, Western Cape, 7600",
                    HostId = hostUser.Id,
                    IsActive = true,
                    Amenities = allAmenities.Where(a => new[] { "Free Parking", "Chef's Kitchen", "Patio & Braai Area", "Fast Wi-Fi" }.Contains(a.Name)).ToList(),
                    PropertyImages = new List<PropertyImage>
                    {
                        new PropertyImage { ImageUrl = "https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?auto=format&fit=crop&w=1200&q=80" },
                        new PropertyImage { ImageUrl = "https://images.unsplash.com/photo-1568605117036-5fe5e7bab0b7?auto=format&fit=crop&w=1200&q=80" }
                    }
                };

                var steps4 = new[]
                {
                    new { step = 1, title = "Farm Gate Access", instruction = "Gate automatically opens on reservation date using registered phone number call." },
                    new { step = 2, title = "Cottage Key in Door", instruction = "The front door is unlocked with keys on the dining room table." }
                };
                property4.CheckInProcess = new CheckInProcess
                {
                    Title = "Stellenbosch Cottage Self Check-in",
                    StepsJson = JsonSerializer.Serialize(steps4),
                    CreatedAt = DateTime.UtcNow
                };

                await context.HostProperties.AddRangeAsync(property1, property2, property3, property4);
                await context.SaveChangesAsync();

                // 7. Seed Reviews
                if (guestUser != null)
                {
                    var review1 = new Review
                    {
                        PropertyId = property1.Id,
                        ReviewerId = guestUser.Id,
                        Rating = 5,
                        Comment = "Absolutely breathtaking view of Camps Bay! The host was extremely responsive and the check-in was seamless. Will definitely return!",
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    };

                    var review2 = new Review
                    {
                        PropertyId = property2.Id,
                        ReviewerId = guestUser.Id,
                        Rating = 5,
                        Comment = "Prime location in Sandton. Fast Wi-Fi and high security made this business trip comfortable and productive.",
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    };

                    await context.Reviews.AddRangeAsync(review1, review2);
                    await context.SaveChangesAsync();
                }

                // 8. Seed Sample Booking for Guest
                if (guestUser != null)
                {
                    // Booking calculation:
                    // PricePerNight * 3 = 4500 * 3 = 13500
                    // CleaningFee = 350
                    // ServiceFee = 13500 * 0.12 = 1620
                    // Total = 15470
                    var sampleBooking = new Booking
                    {
                        PropertyId = property1.Id,
                        GuestId = guestUser.Id,
                        CheckInDate = DateTime.UtcNow.AddDays(7),
                        CheckOutDate = DateTime.UtcNow.AddDays(10),
                        TotalPrice = 15470.00m,
                        Status = BookingStatus.Approved
                    };

                    await context.Bookings.AddAsync(sampleBooking);
                    await context.SaveChangesAsync();

                    // Seed Payment
                    var payment = new Payment
                    {
                        BookingId = sampleBooking.Id,
                        Amount = sampleBooking.TotalPrice,
                        Status = PaymentStatus.Completed,
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    };
                    await context.Payments.AddAsync(payment);

                    // Seed GuestCheckIn
                    var checkIn = new GuestCheckIn
                    {
                        BookingId = sampleBooking.Id,
                        CheckInProcessId = property1.CheckInProcess.Id,
                        Status = CheckInStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.GuestCheckIns.AddAsync(checkIn);

                    // Seed WishlistItem
                    var wishlist = new WishlistItem
                    {
                        UserId = guestUser.Id,
                        PropertyId = property3.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.WishlistItems.AddAsync(wishlist);

                    // Seed Notifications
                    await context.Notifications.AddRangeAsync(
                        new Notification
                        {
                            UserId = guestUser.Id,
                            Title = "Booking Approved",
                            Type = "Booking",
                            CreatedAt = DateTime.UtcNow.AddDays(-1),
                            IsRead = false
                        },
                        new Notification
                        {
                            UserId = hostUser.Id,
                            Title = "New Booking Confirmed",
                            Type = "Booking",
                            CreatedAt = DateTime.UtcNow.AddDays(-1),
                            IsRead = true
                        }
                    );

                    // Seed ActivityLog
                    await context.ActivityLogs.AddRangeAsync(
                        new ActivityLog
                        {
                            UserId = guestUser.Id,
                            Action = "User Registered",
                            ActivityType = "Identity",
                            CreatedAt = DateTime.UtcNow.AddMonths(-1)
                        },
                        new ActivityLog
                        {
                            UserId = hostUser.Id,
                            Action = "Property Listed: Modern Camps Bay Sunset Villa",
                            ActivityType = "Listing",
                            CreatedAt = DateTime.UtcNow.AddMonths(-2)
                        },
                        new ActivityLog
                        {
                            UserId = guestUser.Id,
                            Action = $"Booking #{sampleBooking.Id} Created",
                            ActivityType = "Booking",
                            CreatedAt = DateTime.UtcNow.AddDays(-1)
                        }
                    );

                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
