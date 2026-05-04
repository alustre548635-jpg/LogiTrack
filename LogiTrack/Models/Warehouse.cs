using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models
{
    [Table("Warehouses")]
    public class Warehouse
    {
        [Key]
        public int WarehouseId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; }

        public int TotalCapacity { get; set; } = 0;
        public int UsedCapacity { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}