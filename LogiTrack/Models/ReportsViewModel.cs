using System;
using System.Collections.Generic;

namespace LogiTrack.Models
{
    public class ReportsViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public int TotalShipments { get; set; }
        public int PendingShipments { get; set; }
        public int InTransitShipments { get; set; }
        public int DeliveredShipments { get; set; }

        public int TotalInvoices { get; set; }
        public int PendingInvoices { get; set; }
        public int PaidInvoices { get; set; }
        public decimal TotalInvoicedAmount { get; set; }

        public int TotalTrackingEvents { get; set; }
        public int TotalActivities { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal TotalOperationalCost { get; set; }
        public decimal NetProfit { get; set; }

        public List<DailyCountPoint> ShipmentsByDay { get; set; } = new();
        public List<DailyAmountPoint> InvoiceAmountByDay { get; set; } = new();

        public List<Shipment> RecentShipments { get; set; } = new();
        public List<Invoice> RecentInvoices { get; set; } = new();
        public List<ActivityLog> RecentActivities { get; set; } = new();
    }

    public class DailyCountPoint
    {
        public DateTime Day { get; set; }
        public int Count { get; set; }
    }

    public class DailyAmountPoint
    {
        public DateTime Day { get; set; }
        public decimal Amount { get; set; }
    }
}

