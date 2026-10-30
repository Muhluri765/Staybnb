using System.Collections.Generic;
using Staybnb.Models;

namespace Staybnb.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        public int PendingApplicationsCount { get; set; }
        public int TotalUsersCount { get; set; }
        public int TotalPropertiesCount { get; set; }
        public int TotalBookingsCount { get; set; }
        public decimal PlatformRevenue { get; set; }

        public List<HostApplication> PendingApplications { get; set; } = new List<HostApplication>();
        public List<ActivityLog> RecentActivityLogs { get; set; } = new List<ActivityLog>();
        public List<HostProperty> ActiveListings { get; set; } = new List<HostProperty>();
    }

    public class UserManagementViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsSuperAdmin { get; set; }
        public bool IsHost { get; set; }
        public bool IsGuest { get; set; }
    }
}
