using System.Collections.Generic;

namespace LogiTrack.Models
{
    public class ClientDashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TotalMyShipments { get; set; }
        public int MyPendingShipments { get; set; }
        public int MyInTransitShipments { get; set; }
        public int MyDeliveredShipments { get; set; }
        public int MyCompletedShipments { get; set; }
        public int MyCancelledShipments { get; set; }
        public bool CanViewAuditLogs { get; set; }
        public List<string> AssignedModules { get; set; } = new();
        public List<Shipment> RecentShipments { get; set; } = new();
        public List<ActivityLog> RecentActivities { get; set; } = new();
    }
}
