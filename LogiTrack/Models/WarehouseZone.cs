using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models
{
    [Table("WarehouseZones")]
    public class WarehouseZone
    {
        [Key]
        public int ZoneId { get; set; }

        public int WarehouseId { get; set; }

        [Required]
        [StringLength(50)]
        public string ZoneName { get; set; }

        public int TotalSlots { get; set; } = 0;
        public int UsedSlots { get; set; } = 0;

        [Required]
        [StringLength(50)]
        public string ZoneType { get; set; }

        [ForeignKey("WarehouseId")]
        public Warehouse Warehouse { get; set; }
    }
}