namespace LogiTrack.Models
{
    public class RouteOptimizationViewModel
    {
        public List<string> HubOptions { get; set; } = new();
        public List<RouteHistoryItemViewModel> RecentRoutes { get; set; } = new();
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
