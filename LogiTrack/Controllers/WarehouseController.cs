using LogiTrack.Data;
using LogiTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Controllers
{
    public class WarehouseController : Controller
    {
        private readonly LogiTrackDbContext _db;

        public WarehouseController(LogiTrackDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(int? warehouseId)
        {
            var userIdText = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrWhiteSpace(userIdText))
                return RedirectToAction("Login", "Account");

            var role = HttpContext.Session.GetString("Role") ?? string.Empty;
            ViewData["ActiveNav"] = "warehouse";

            var warehouses = await _db.Warehouses
                .Where(w => w.IsActive)
                .OrderBy(w => w.Name)
                .ToListAsync();

            if (warehouses.Count == 0)
            {
                return View(new WarehouseDashboardViewModel
                {
                    Warehouses = new List<Warehouse>(),
                    SelectedWarehouse = null
                });
            }

            var selectedId = warehouseId.HasValue && warehouses.Any(w => w.WarehouseId == warehouseId.Value)
                ? warehouseId.Value
                : warehouses[0].WarehouseId;

            var selectedWarehouse = warehouses.First(w => w.WarehouseId == selectedId);

            var zones = await _db.WarehouseZones
                .Where(z => z.WarehouseId == selectedId)
                .OrderBy(z => z.ZoneName)
                .ToListAsync();

            var today = DateTime.Today;

            var inboundShipments = await _db.Shipments
                .Where(s => s.WarehouseId == selectedId && s.Status == "Pending")
                .OrderBy(s => s.ScheduledDate)
                .Take(12)
                .ToListAsync();

            var outboundShipments = await _db.Shipments
                .Where(s => s.WarehouseId == selectedId && s.Status == "In Transit")
                .OrderByDescending(s => s.CreatedAt)
                .Take(12)
                .ToListAsync();

            var deliveredToday = await _db.Shipments.CountAsync(s =>
                s.WarehouseId == selectedId &&
                s.Status == "Delivered" &&
                s.CreatedAt >= today);

            var dockSchedules = await _db.DockSchedules
                .Include(d => d.Shipment)
                .Where(d => d.WarehouseId == selectedId && d.StartTime >= today && d.StartTime < today.AddDays(1))
                .OrderBy(d => d.DockNumber)
                .ThenBy(d => d.StartTime)
                .ToListAsync();

            var staffOnShift = await _db.Staff
                .Where(s => s.WarehouseId == selectedId && s.IsOnShift)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            var model = new WarehouseDashboardViewModel
            {
                Warehouses = warehouses,
                SelectedWarehouse = selectedWarehouse,
                Zones = zones,
                TotalCapacity = selectedWarehouse.TotalCapacity,
                UsedCapacity = selectedWarehouse.UsedCapacity,
                InboundPending = await _db.Shipments.CountAsync(s => s.WarehouseId == selectedId && s.Status == "Pending"),
                OutboundInTransit = await _db.Shipments.CountAsync(s => s.WarehouseId == selectedId && s.Status == "In Transit"),
                DeliveredToday = deliveredToday,
                OnShiftStaff = staffOnShift.Count,
                InboundShipments = inboundShipments,
                OutboundShipments = outboundShipments,
                DockSchedules = dockSchedules,
                StaffOnShift = staffOnShift
            };

            // Simple role-based guardrails (UI also respects module assignments via shared layout)
            ViewBag.UserRole = role;

            return View(model);
        }
        [HttpPost]
        public async Task<JsonResult> Deactivate(int warehouseId)
        {
            var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.WarehouseId == warehouseId);
            if (warehouse == null)
                return Json(new { success = false, message = "Warehouse not found." });

            warehouse.IsActive = false;
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = $"Warehouse \"{warehouse.Name}\" deactivated." });
        }

        [HttpPost]
        public async Task<JsonResult> Activate(int warehouseId)
        {
            var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.WarehouseId == warehouseId);
            if (warehouse == null)
                return Json(new { success = false, message = "Warehouse not found." });

            warehouse.IsActive = true;
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = $"Warehouse \"{warehouse.Name}\" activated." });
        }
    }
}
