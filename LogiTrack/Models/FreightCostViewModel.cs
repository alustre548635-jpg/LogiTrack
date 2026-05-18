using System.Collections.Generic;

namespace LogiTrack.Models
{
    public class FreightCostViewModel
    {
        public decimal TotalFreightSpend { get; set; }
        public decimal BudgetRemaining { get; set; }
        public decimal AvgCostPerKm { get; set; }
        public int OverbudgetCount { get; set; }

        public List<FreightRateCard> RateCards { get; set; } = new();
        public List<CostAnomalyViewModel> Anomalies { get; set; } = new();
        public List<Invoice> Invoices { get; set; } = new();
        public List<Carrier> Carriers { get; set; } = new();
        public List<Payment> Payments { get; set; } = new();
    }

    public class CostAnomalyViewModel
    {
        public string ShipmentCode { get; set; }
        public string CarrierName { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal ActualCost { get; set; }
        public decimal Variance => ActualCost - EstimatedCost;
        public double VariancePercent => EstimatedCost == 0 ? 0 : (double)((ActualCost - EstimatedCost) / EstimatedCost * 100);
    }
}
