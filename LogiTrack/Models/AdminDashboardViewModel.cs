using System.Collections.Generic;

namespace LogiTrack.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int AdminUsers { get; set; }
        public int ManagerUsers { get; set; }
        public int StaffUsers { get; set; }
        public int DriverUsers { get; set; }
        public int FinanceUsers { get; set; }
        public int TotalShipments { get; set; }
        public int InTransitShipments { get; set; }
        public int PendingShipments { get; set; }
        public int TotalCarriers { get; set; }
        public int TotalWarehouses { get; set; }
        public int TotalRoutes { get; set; }
        public int TotalInvoices { get; set; }
        public int TotalTrackingEvents { get; set; }
        public List<ActivityLog> RecentActivities { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public Dictionary<int, List<string>> UserAssignedModules { get; set; } = new();
    }
}
