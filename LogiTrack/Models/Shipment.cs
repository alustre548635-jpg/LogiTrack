using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models
{
    [Table("Shipments")]
    public class Shipment
    {
        [Key]
        public int ShipmentId { get; set; }

        [Required]
        [StringLength(30)]
        public string ShipmentCode { get; set; }

        public int CreatedByUserId { get; set; }
        public int CarrierId { get; set; }
        public int WarehouseId { get; set; }

        [Required]
        [StringLength(100)]
        public string Origin { get; set; }

        [Required]
        [StringLength(100)]
        public string Destination { get; set; }

        [StringLength(50)]
        public string CargoType { get; set; }

        public decimal Weight { get; set; }
        public decimal? Volume { get; set; }

        [StringLength(20)]
        public string Priority { get; set; } = "Standard";

        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime ScheduledDate { get; set; }

        [StringLength(200)]
        public string? SpecialHandling { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public int? RouteId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedCost { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("CreatedByUserId")]
        public User CreatedBy { get; set; }

        [ForeignKey("CarrierId")]
        public Carrier Carrier { get; set; }

        [ForeignKey("WarehouseId")]
        public Warehouse Warehouse { get; set; }

        [ForeignKey("RouteId")]
        public ShipmentRoute? Route { get; set; }
    }
}