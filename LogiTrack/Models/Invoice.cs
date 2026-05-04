using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models
{
    [Table("Invoices")]
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        [Required]
        [StringLength(30)]
        public string InvoiceCode { get; set; }

        public int CarrierId { get; set; }

        public decimal Amount { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime? PaidAt { get; set; }

        [ForeignKey("CarrierId")]
        public Carrier Carrier { get; set; }
    }
}