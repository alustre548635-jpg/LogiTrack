using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int ShipmentId { get; set; }
        
        [ForeignKey("ShipmentId")]
        public Shipment? Shipment { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed, Refunded

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Bank Transfer"; // Cash, Bank Transfer, Card, COD

        [StringLength(100)]
        public string? ReferenceNumber { get; set; } // OR Number or Transaction ID

        public DateTime? PaymentDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Notes { get; set; }

        // Logic Helpers
        [NotMapped]
        public bool IsOverdue => Status == "Pending" && CreatedAt.AddDays(7) < DateTime.Now;
    }
}
