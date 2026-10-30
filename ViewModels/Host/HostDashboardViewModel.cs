using System.Collections.Generic;
using Staybnb.Models;

namespace Staybnb.ViewModels.Host
{
    public class HostDashboardViewModel
    {
        public List<HostProperty> Properties { get; set; } = new List<HostProperty>();
        public List<Staybnb.Models.Booking> PendingBookings { get; set; } = new List<Staybnb.Models.Booking>();
        public List<Staybnb.Models.Booking> ConfirmedBookings { get; set; } = new List<Staybnb.Models.Booking>();
        public List<GuestCheckIn> PendingCheckIns { get; set; } = new List<GuestCheckIn>();
        public decimal TotalEarnings { get; set; }
        public int TotalBookingsCount { get; set; }
        public int ActiveListingsCount { get; set; }
    }
}
