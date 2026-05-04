using LogiTrack.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;

namespace LogiTrack.Data
{
    public class LogiTrackDbContext : DbContext
    {
        public LogiTrackDbContext(DbContextOptions<LogiTrackDbContext> options)
            : base(options)
        {
        }

        // All 13 tables
        public DbSet<User> Users { get; set; }
        public DbSet<Carrier> Carriers { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseZone> WarehouseZones { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<ShipmentRoute> Routes { get; set; }
        public DbSet<FreightRateCard> FreightRateCards { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<DockSchedule> DockSchedules { get; set; }
        public DbSet<TrackingEvent> TrackingEvents { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
    }
}