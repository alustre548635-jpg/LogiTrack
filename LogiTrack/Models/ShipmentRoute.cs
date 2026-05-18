using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models
{
    [Table("Routes")]
    public class ShipmentRoute
    {
        [Key]
        public int RouteId { get; set; }

        [Required]
        [StringLength(30)]
        public string RouteNumber { get; set; }

        public int? DriverId { get; set; }

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

        [ForeignKey("DriverId")]
        public Driver? Driver { get; set; }

        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    }
}