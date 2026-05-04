using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models
{
    [Table("DockSchedules")]
    public class DockSchedule
    {
        [Key]
        public int DockScheduleId { get; set; }

        public int WarehouseId { get; set; }
        public int ShipmentId { get; set; }
        public int DockNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Scheduled";

        [ForeignKey("WarehouseId")]
        public Warehouse Warehouse { get; set; }

        [ForeignKey("ShipmentId")]
        public Shipment Shipment { get; set; }
    }
}