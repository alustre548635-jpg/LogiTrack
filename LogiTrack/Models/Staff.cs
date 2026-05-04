using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models
{
    [Table("Staff")]
    public class Staff
    {
        [Key]
        public int StaffId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        public int WarehouseId { get; set; }

        [StringLength(50)]
        public string? Zone { get; set; }

        [StringLength(200)]
        public string? CurrentTask { get; set; }

        public bool IsOnShift { get; set; } = false;

        [StringLength(20)]
        public string? ShiftType { get; set; }

        [ForeignKey("WarehouseId")]
        public Warehouse Warehouse { get; set; }
    }
}