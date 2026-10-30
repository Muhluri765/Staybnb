using System.Collections.Generic;
using Staybnb.Models;
using Staybnb.ViewModels.Booking;

namespace Staybnb.ViewModels.Home
{
    public class PropertyDetailsViewModel
    {
        public HostProperty Property { get; set; } = null!;
        public List<Review> Reviews { get; set; } = new List<Review>();
        public double AverageRating { get; set; }
        public bool IsInWishlist { get; set; }
        public BookingCheckoutViewModel BookingForm { get; set; } = new BookingCheckoutViewModel();
        public Review NewReview { get; set; } = new Review();
    }
}
