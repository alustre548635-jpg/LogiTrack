using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models
{
    [Table("Drivers")]
    public class Driver
    {
        [Key]
        public int DriverId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [StringLength(50)]
        public string LicenseNumber { get; set; }

        [Required]
        [StringLength(20)]
        public string VehiclePlate { get; set; }

        [Required]
        [StringLength(50)]
        public string VehicleType { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Available";

        public int? UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public DateTime? LicenseExpiry { get; set; }

        public decimal OnTimeDeliveryRate { get; set; } = 100;

        public int SafetyScore { get; set; } = 100;

        public int? AssignedWarehouseId { get; set; }

        [ForeignKey("AssignedWarehouseId")]
        public Warehouse? Warehouse { get; set; }
    }
}