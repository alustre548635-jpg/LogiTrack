using System.Collections.Generic;

namespace LogiTrack.Models
{
    public class ManagerDashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Manager";

        public int TotalShipments { get; set; }
        public int PendingShipments { get; set; }
        public int InTransitShipments { get; set; }
        public int DeliveredShipments { get; set; }

        public List<Shipment> RecentShipments { get; set; } = new();
        public List<ActivityLog> RecentActivities { get; set; } = new();
    }
}

