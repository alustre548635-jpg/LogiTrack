using System;
using System.Collections.Generic;

namespace LogiTrack.Models
{
    public class WarehouseDashboardViewModel
    {
        public List<Warehouse> Warehouses { get; set; } = new();
        public Warehouse? SelectedWarehouse { get; set; }
        public List<WarehouseZone> Zones { get; set; } = new();

        public int TotalCapacity { get; set; }
        public int UsedCapacity { get; set; }
        public int FreeCapacity => Math.Max(0, TotalCapacity - UsedCapacity);
        public int UtilizationPct => TotalCapacity <= 0 ? 0 : (int)Math.Round((double)UsedCapacity * 100.0 / TotalCapacity);

        public int InboundPending { get; set; }
        public int OutboundInTransit { get; set; }
        public int DeliveredToday { get; set; }

        public int OnShiftStaff { get; set; }

        public List<Shipment> InboundShipments { get; set; } = new();
        public List<Shipment> OutboundShipments { get; set; } = new();

        public List<DockSchedule> DockSchedules { get; set; } = new();
        public List<Staff> StaffOnShift { get; set; } = new();
    }
}

