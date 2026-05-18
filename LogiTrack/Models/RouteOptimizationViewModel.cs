namespace LogiTrack.Models
{
    public class RouteOptimizationViewModel
    {
        public List<string> HubOptions { get; set; } = new();
        public List<RouteHistoryItemViewModel> RecentRoutes { get; set; } = new();
        public List<ShipmentOptionViewModel> AvailableShipments { get; set; } = new();
        public List<DriverOptionViewModel> AvailableDrivers { get; set; } = new();
    }

    public class DriverOptionViewModel
    {
        public int DriverId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string VehiclePlate { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
    }

    public class ShipmentOptionViewModel
    {
        public int ShipmentId { get; set; }
        public string ShipmentCode { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
    }

    public class RouteHistoryItemViewModel
    {
        public int RouteId { get; set; }
        public string StartHub { get; set; } = string.Empty;
        public string EndHub { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public int EstimatedMinutes { get; set; }
        public string OptimizationType { get; set; } = "Fastest";
        public string Status { get; set; } = "Planned";
        public DateTime CreatedAt { get; set; }
    }
}
