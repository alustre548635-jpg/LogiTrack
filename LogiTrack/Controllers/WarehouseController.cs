using LogiTrack.Data;
using LogiTrack.Models;
using LogiTrack.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Controllers
{
    public class WarehouseController : Controller
    {
        private readonly LogiTrackDbContext _db;
        private readonly IAuditLogService _auditLogService;

        public WarehouseController(LogiTrackDbContext db, IAuditLogService auditLogService)
        {
            _db = db;
            _auditLogService = auditLogService;
        }

        private int? GetCurrentUserId()
        {
            var idStr = HttpContext.Session.GetString("UserId");
            if (int.TryParse(idStr, out int userId)) return userId;
            return null;
        }

        public async Task<IActionResult> Index(int? warehouseId, bool includeDeactivated = false)
        {
            var userIdText = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrWhiteSpace(userIdText))
                return RedirectToAction("Login", "Account");

            var role = HttpContext.Session.GetString("Role") ?? string.Empty;
            ViewData["ActiveNav"] = "warehouse";
            ViewBag.IncludeDeactivated = includeDeactivated;

            var query = _db.Warehouses.AsQueryable();
            if (!includeDeactivated)
            {
                query = query.Where(w => w.IsActive);
            }

            var warehouses = await query
                .OrderBy(w => w.Name)
                .ToListAsync();

            if (warehouses.Count == 0 && !includeDeactivated)
            {
                // If nothing found and not showing deactivated, try showing deactivated automatically or return empty
                return View(new WarehouseDashboardViewModel
                {
                    Warehouses = new List<Warehouse>(),
                    SelectedWarehouse = null
                });
            }

            // If user is Staff, auto-detect their assigned warehouse
            if (string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                var userId = int.Parse(userIdText);
                var staffRecord = await _db.Staff.FirstOrDefaultAsync(s => s.UserId == userId);
                if (staffRecord != null)
                {
                    warehouseId = staffRecord.WarehouseId;
                }
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
                .Where(s => s.WarehouseId == selectedId && s.Status == "Received at Warehouse")
                .OrderByDescending(s => s.CreatedAt)
                .Take(12)
                .ToListAsync();

            var dispatchedToday = await _db.Shipments.CountAsync(s =>
                s.WarehouseId == selectedId &&
                s.Status == "In Transit");

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
                OutboundInTransit = await _db.Shipments.CountAsync(s => s.WarehouseId == selectedId && s.Status == "Received at Warehouse"),
                DeliveredToday = dispatchedToday,
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

            await _auditLogService.LogAsync(
                GetCurrentUserId(),
                $"Deactivated warehouse: {warehouse.Name}",
                "Warehouses",
                HttpContext.Connection.RemoteIpAddress?.ToString());

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

            await _auditLogService.LogAsync(
                GetCurrentUserId(),
                $"Activated warehouse: {warehouse.Name}",
                "Warehouses",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new { success = true, message = $"Warehouse \"{warehouse.Name}\" activated." });
        }

        [HttpPost]
        public async Task<JsonResult> UpdateShipmentStatus(int shipmentId, string newStatus)
        {
            try
            {
                var shipment = await _db.Shipments.FirstOrDefaultAsync(s => s.ShipmentId == shipmentId);
                if (shipment == null)
                    return Json(new { success = false, message = "Shipment not found." });

                shipment.Status = newStatus;
                await _db.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    GetCurrentUserId(),
                    $"Updated shipment status to '{newStatus}' for {shipment.ShipmentCode}",
                    "Shipments",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return Json(new { success = true, message = $"Shipment {shipment.ShipmentCode} status updated to {newStatus}." });
            }
            catch (Exception ex)
            {
                var baseException = ex.GetBaseException();
                return Json(new { success = false, message = "DB Error: " + baseException.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Create(string name, string location, int capacity)
        {
            var role = HttpContext.Session.GetString("Role");
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "Only Super Admins can add new warehouses." });

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(location))
                return Json(new { success = false, message = "Name and Location are required." });

            var warehouse = new Warehouse
            {
                Name = name,
                Location = location,
                TotalCapacity = capacity,
                IsActive = true
            };
            _db.Warehouses.Add(warehouse);
            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                GetCurrentUserId(),
                $"Created new warehouse: {warehouse.Name} ({warehouse.Location})",
                "Warehouses",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new { success = true, message = "Warehouse created successfully.", warehouseId = warehouse.WarehouseId });
        }

        [HttpPost]
        public async Task<JsonResult> Update(int warehouseId, string name, string location, int capacity)
        {
            var warehouse = await _db.Warehouses.FindAsync(warehouseId);
            if (warehouse == null) return Json(new { success = false, message = "Warehouse not found." });

            warehouse.Name = name;
            warehouse.Location = location;
            warehouse.TotalCapacity = capacity;

            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                GetCurrentUserId(),
                $"Updated warehouse details: {warehouse.Name}",
                "Warehouses",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new { success = true, message = "Warehouse updated successfully." });
        }

        [HttpPost]
        public async Task<JsonResult> AddZone(int warehouseId, string name, string type, int slots)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
                return Json(new { success = false, message = "Zone name and type are required." });

            var zone = new WarehouseZone
            {
                WarehouseId = warehouseId,
                ZoneName = name,
                ZoneType = type,
                TotalSlots = slots,
                UsedSlots = 0
            };
            _db.WarehouseZones.Add(zone);
            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                GetCurrentUserId(),
                $"Added new zone '{zone.ZoneName}' to Warehouse ID {warehouseId}",
                "WarehouseZones",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new { success = true, message = "Zone added successfully." });
        }

        [HttpPost]
        public async Task<JsonResult> DeleteZone(int zoneId)
        {
            var zone = await _db.WarehouseZones.FindAsync(zoneId);
            if (zone == null) return Json(new { success = false, message = "Zone not found." });

            if (zone.UsedSlots > 0)
                return Json(new { success = false, message = "Cannot delete zone that still has items stored." });

            _db.WarehouseZones.Remove(zone);
            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                GetCurrentUserId(),
                $"Deleted zone '{zone.ZoneName}' from Warehouse ID {zone.WarehouseId}",
                "WarehouseZones",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new { success = true, message = "Zone deleted successfully." });
        }
    }
}
