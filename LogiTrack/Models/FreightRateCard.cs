using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models
{
    [Table("FreightRateCards")]
    public class FreightRateCard
    {
        [Key]
        public int RateCardId { get; set; }

        public int CarrierId { get; set; }

        [Required]
        [StringLength(100)]
        public string Zone { get; set; }

        [Required]
        [StringLength(50)]
        public string WeightBracket { get; set; }

        public decimal BaseRatePerKg { get; set; }
        public decimal FuelSurchargePercent { get; set; } = 0;
        public decimal HandlingFee { get; set; } = 0;
        public DateTime EffectiveDate { get; set; } = DateTime.Now;

        [ForeignKey("CarrierId")]
        public Carrier Carrier { get; set; }
    }
}