using LogiTrack.Models;
using System.Collections.Generic;

namespace LogiTrack.Models
{
    public class DriverDashboardViewModel
    {
        public Driver Driver { get; set; }
        public ShipmentRoute? ActiveRoute { get; set; }
        public List<Shipment> Shipments { get; set; } = new List<Shipment>();
        public int TotalDeliveries { get; set; }
        public decimal WeeklyOnTimeRate { get; set; }
        public int SafetyScore { get; set; }
        public List<ActivityLog> RecentActivity { get; set; } = new List<ActivityLog>();
    }
}
