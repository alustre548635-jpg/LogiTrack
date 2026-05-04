using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models
{
    [Table("Routes")]
    public class ShipmentRoute  // ← renamed from Route to ShipmentRoute
    {
        [Key]
        public int RouteId { get; set; }

        public int ShipmentId { get; set; }

        [Required]
        [StringLength(100)]
        public string StartHub { get; set; }

        [Required]
        [StringLength(100)]
        public string EndHub { get; set; }

        [StringLength(500)]
        public string? Waypoints { get; set; }

        [StringLength(50)]
        public string VehicleType { get; set; }

        public decimal? LoadCapacity { get; set; }
        public decimal? DistanceKm { get; set; }
        public int? EstimatedMinutes { get; set; }
        public decimal? FuelCostEstimate { get; set; }
        public decimal? TollCost { get; set; } = 0;

        [StringLength(20)]
        public string OptimizationType { get; set; } = "Fastest";

        [StringLength(20)]
        public string Status { get; set; } = "Planned";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ShipmentId")]
        public Shipment Shipment { get; set; }
    }
}