using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models
{
    [Table("TrackingEvents")]
    public class TrackingEvent
    {
        [Key]
        public int EventId { get; set; }

        public int ShipmentId { get; set; }
        public int DriverId { get; set; }

        [Required]
        [StringLength(50)]
        public string EventType { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        [StringLength(300)]
        public string? Notes { get; set; }

        public DateTime EventTime { get; set; } = DateTime.Now;

        [ForeignKey("ShipmentId")]
        public Shipment Shipment { get; set; }

        [ForeignKey("DriverId")]
        public Driver Driver { get; set; }
    }
}