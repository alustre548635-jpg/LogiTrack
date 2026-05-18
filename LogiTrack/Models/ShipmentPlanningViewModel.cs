namespace LogiTrack.Models
{
    public class ShipmentPlanningViewModel
    {
        public int TotalToday { get; set; }
        public int PendingApproval { get; set; }
        public int InTransit { get; set; }
        public int Completed { get; set; }
        public List<ShipmentListItemViewModel> Shipments { get; set; } = new();
        public List<Carrier> Carriers { get; set; } = new();
        public List<Warehouse> Warehouses { get; set; } = new();
        public List<FreightRateCard> RateCards { get; set; } = new();
    }

    public class ShipmentListItemViewModel
    {
        public int ShipmentId { get; set; }
        public string ShipmentCode { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string CarrierName { get; set; } = string.Empty;
        public int CarrierId { get; set; }
        public string CargoType { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public string Priority { get; set; } = "Standard";
        public string Status { get; set; } = "Pending";
        public DateTime ScheduledDate { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal ShippingFee { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal Profit => ShippingFee - EstimatedCost;
    }
}
