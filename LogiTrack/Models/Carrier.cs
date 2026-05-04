using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models
{
    [Table("Carriers")]
    public class Carrier
    {
        [Key]
        public int CarrierId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [StringLength(100)]
        public string? ContactEmail { get; set; }

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        public bool IsActive { get; set; } = true;
    }
}