using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogiTrack.Data;
using LogiTrack.Models;

namespace LogiTrack.Controllers
{
    public class ShipmentController : Controller
    {
        private readonly LogiTrackDbContext _db;

        public ShipmentController(LogiTrackDbContext db)
        {
            _db = db;
        }

        private bool CanCancelShipment()
        {
            var role = HttpContext.Session.GetString("Role");
            return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Super Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Logistics Manager", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IActionResult> Index()
        {
            var userIdText = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("Role");
            var fullName = HttpContext.Session.GetString("FullName");

            if (string.IsNullOrWhiteSpace(userIdText))
                return RedirectToAction("Login", "Account");

            if (!int.TryParse(userIdText, out var userId))
                return RedirectToAction("Login", "Account");

            // Fetch assigned modules for sidebar
            var latestModuleAssignment = await _db.ActivityLogs
                .Where(a => a.UserId == userId && a.TableAffected == "UserModules")
                .OrderByDescending(a => a.Timestamp)
                .Select(a => a.Action)
                .FirstOrDefaultAsync();

            var assignedModules = new List<string>();
            if (!string.IsNullOrWhiteSpace(latestModuleAssignment))
            {
                var marker = "Assigned modules:";
                var idx = latestModuleAssignment.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                var modulesPart = idx >= 0
                    ? latestModuleAssignment[(idx + marker.Length)..]
                    : latestModuleAssignment;

                assignedModules = modulesPart
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(m => !string.Equals(m, "No modules selected", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            ViewBag.AssignedModules = assignedModules;
            ViewBag.UserRole = role;
            ViewBag.UserFullName = fullName;

            var today = DateTime.Today;
            var carriers = await _db.Carriers.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            var warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync();
            var rateCards = await _db.FreightRateCards.Include(r => r.Carrier).ToListAsync();
            var shipments = await _db.Shipments
                .Include(s => s.Carrier)
                .Include(s => s.Warehouse)
                .OrderByDescending(s => s.ShipmentId)
                .Take(200)
                .Select(s => new ShipmentListItemViewModel
                {
                    ShipmentId = s.ShipmentId,
                    ShipmentCode = s.ShipmentCode,
                    Origin = s.Origin,
                    Destination = s.Destination,
                    CarrierName = s.Carrier != null ? s.Carrier.Name : "Unassigned",
                    CarrierId = s.CarrierId,
                    CargoType = s.CargoType ?? "N/A",
                    Weight = s.Weight,
                    Priority = s.Priority ?? "Standard",
                    Status = s.Status ?? "Pending",
                    ScheduledDate = s.ScheduledDate,
                    WarehouseId = s.WarehouseId,
                    WarehouseName = s.Warehouse != null ? s.Warehouse.Name : "N/A",
                    ShippingFee = s.ShippingFee,
                    EstimatedCost = s.EstimatedCost
                })
                .ToListAsync();

            var model = new ShipmentPlanningViewModel
            {
                TotalToday = await _db.Shipments.CountAsync(s => s.CreatedAt >= today),
                PendingApproval = await _db.Shipments.CountAsync(s => s.Status == "Pending"),
                InTransit = await _db.Shipments.CountAsync(s => s.Status == "In Transit"),
                Completed = await _db.Shipments.CountAsync(s => s.Status == "Delivered" || s.Status == "Completed"),
                Shipments = shipments,
                Carriers = carriers,
                Warehouses = warehouses,
                RateCards = rateCards
            };

            return View(model);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<JsonResult> Create([FromBody] ShipmentCreateRequest model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid request body." });
            if (string.IsNullOrWhiteSpace(model.Origin) ||
                string.IsNullOrWhiteSpace(model.Destination) ||
                string.IsNullOrWhiteSpace(model.CargoType) ||
                model.Weight == null || model.Weight <= 0 ||
                model.CarrierId == null || model.CarrierId <= 0 ||
                model.ScheduledDate == null || model.ScheduledDate == default)
            {
                return Json(new { success = false, message = "Please complete all required shipment fields." });
            }

            var carrierExists = await _db.Carriers.AnyAsync(c => c.CarrierId == model.CarrierId && c.IsActive);
            if (!carrierExists)
            {
                return Json(new { success = false, message = "Selected carrier is not available." });
            }

            var warehouseId = await _db.Warehouses
                .Where(w => w.IsActive)
                .Select(w => w.WarehouseId)
                .FirstOrDefaultAsync();

            if (warehouseId == 0)
            {
                return Json(new { success = false, message = "No active warehouse found for shipment assignment." });
            }

            var userIdString = HttpContext.Session.GetString("UserId");
            var createdByUserId = int.TryParse(userIdString, out var parsedUserId)
                ? parsedUserId
                : await _db.Users.Select(u => u.UserId).FirstOrDefaultAsync();

            if (createdByUserId == 0)
            {
                return Json(new { success = false, message = "No active user context found. Please log in again." });
            }


            var shipment = new Shipment
            {
                ShipmentCode = "TMP-" + Guid.NewGuid().ToString("N")[..12].ToUpper(),
                CreatedByUserId = createdByUserId,
                CarrierId = model.CarrierId.Value,
                WarehouseId = model.WarehouseId != null && model.WarehouseId > 0 ? model.WarehouseId.Value : warehouseId,
                Origin = model.Origin.Trim(),
                Destination = model.Destination.Trim(),
                CargoType = model.CargoType.Trim(),
                Weight = model.Weight ?? 0m,
                Volume = model.Volume,
                ShippingFee = model.ShippingFee ?? 0m,
                EstimatedCost = model.EstimatedCost ?? 0m,
                Priority = string.IsNullOrWhiteSpace(model.Priority) ? "Standard" : model.Priority.Trim(),
                Status = "Pending",
                ScheduledDate = model.ScheduledDate.Value,
                SpecialHandling = model.Handling != null && model.Handling.Length > 0 ? string.Join(", ", model.Handling) : null,
                Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
                CreatedAt = DateTime.Now
            };

            try
            {
                _db.Shipments.Add(shipment);
                await _db.SaveChangesAsync();

                // Use the real auto-increment ID so ShipmentCode always matches ShipmentId
                shipment.ShipmentCode = $"SHP-{DateTime.Now:yyyy}-{shipment.ShipmentId:D4}";
                await _db.SaveChangesAsync();

                return Json(new { success = true, shipmentCode = shipment.ShipmentCode, message = "Shipment created successfully." });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "DB Error: " + msg });
            }
        }

        private decimal? ParseDecimal(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            var cleanVal = val.Replace(",", ".");
            if (decimal.TryParse(cleanVal, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Approve(int shipmentId)
        {
            if (!CanCancelShipment())
                return Json(new { success = false, message = "You are not allowed to approve shipments." });

            if (shipmentId <= 0)
                return Json(new { success = false, message = "Invalid shipment id." });

            var shipment = await _db.Shipments.FirstOrDefaultAsync(s => s.ShipmentId == shipmentId);
            if (shipment == null)
            {
                return Json(new { success = false, message = "Shipment not found." });
            }

            if (string.Equals(shipment.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                shipment.Status = "In Transit";
                await _db.SaveChangesAsync();
            }

            return Json(new { success = true, message = $"Shipment {shipment.ShipmentCode} approved." });
        }

        public async Task<JsonResult> GetAll()
        {
            var shipments = await _db.Shipments
                .Include(s => s.Carrier)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.ShipmentId,
                    s.ShipmentCode,
                    s.Origin,
                    s.Destination,
                    Carrier = s.Carrier != null ? s.Carrier.Name : "Unassigned",
                    s.CargoType,
                    s.Weight,
                    s.Priority,
                    s.Status,
                    s.ShippingFee,
                    s.EstimatedCost,
                    WarehouseName = s.Warehouse != null ? s.Warehouse.Name : "N/A",
                    ScheduledDate = s.ScheduledDate.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Json(new { success = true, items = shipments });
        }

        public async Task<JsonResult> KpiSummary()
        {
            var today = DateTime.Today;
            var totalToday = await _db.Shipments.CountAsync(s => s.CreatedAt >= today);
            var pending = await _db.Shipments.CountAsync(s => s.Status == "Pending");
            var inTransit = await _db.Shipments.CountAsync(s => s.Status == "In Transit");
            var completed = await _db.Shipments.CountAsync(s => s.Status == "Delivered" || s.Status == "Completed");
            return Json(new
            {
                success = true,
                totalToday,
                pending,
                inTransit,
                completed
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Update([FromBody] ShipmentUpdateRequest model)
        {
            var shipment = await _db.Shipments.FirstOrDefaultAsync(s => s.ShipmentId == model.ShipmentId);
            if (shipment == null)
                return Json(new { success = false, message = "Shipment not found." });

            if (string.IsNullOrWhiteSpace(model.Origin) ||
                string.IsNullOrWhiteSpace(model.Destination) ||
                string.IsNullOrWhiteSpace(model.CargoType) ||
                model.Weight == null || model.Weight <= 0 ||
                model.CarrierId <= 0 ||
                model.ScheduledDate == default)
            {
                return Json(new { success = false, message = "Please complete all required fields." });
            }

            var warehouseId = model.WarehouseId;
            if (warehouseId <= 0)
            {
                warehouseId = await _db.Warehouses.Where(w => w.IsActive).Select(w => w.WarehouseId).FirstOrDefaultAsync();
                if (warehouseId == 0)
                    return Json(new { success = false, message = "No active warehouse available." });
            }

            shipment.Origin = model.Origin.Trim();
            shipment.Destination = model.Destination.Trim();
            shipment.CargoType = model.CargoType.Trim();
            shipment.Weight = model.Weight ?? 0m;
            shipment.Volume = model.Volume;
            shipment.Priority = string.IsNullOrWhiteSpace(model.Priority) ? "Standard" : model.Priority.Trim();
            shipment.ScheduledDate = model.ScheduledDate;
            shipment.CarrierId = model.CarrierId;
            shipment.WarehouseId = model.WarehouseId > 0 ? model.WarehouseId : warehouseId;
            shipment.ShippingFee = model.ShippingFee ?? 0m;
            shipment.EstimatedCost = model.EstimatedCost ?? 0m;
            shipment.SpecialHandling = model.Handling != null && model.Handling.Length > 0 ? string.Join(", ", model.Handling) : null;
            shipment.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();

            await _db.SaveChangesAsync();
            return Json(new { success = true, message = $"Shipment {shipment.ShipmentCode} updated." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Cancel([FromForm] int shipmentId)
        {
            if (!CanCancelShipment())
                return Json(new { success = false, message = "You are not allowed to cancel shipments." });

            if (shipmentId <= 0)
                return Json(new { success = false, message = "Invalid shipment id." });

            var shipment = await _db.Shipments.FirstOrDefaultAsync(s => s.ShipmentId == shipmentId);
            if (shipment == null)
                return Json(new { success = false, message = "Shipment not found." });

            shipment.Status = "Cancelled";
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = $"Shipment {shipment.ShipmentCode} cancelled." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export()
        {
            var shipments = await _db.Shipments
                .Include(s => s.Carrier)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("ShipmentCode,Origin,Destination,Carrier,CargoType,Weight,Status,ScheduledDate,ShippingFee,EstimatedCost");

            foreach (var s in shipments)
            {
                builder.AppendLine($"{s.ShipmentCode},{s.Origin},{s.Destination},{s.Carrier?.Name ?? "N/A"},{s.CargoType},{s.Weight},{s.Status},{s.ScheduledDate:yyyy-MM-dd},{s.ShippingFee},{s.EstimatedCost}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"Shipments_Export_{DateTime.Now:yyyyMMdd}.csv");
        }
    }

    public class ShipmentCreateRequest
    {
        public string? Origin { get; set; }
        public string? Destination { get; set; }
        public string? CargoType { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Volume { get; set; }
        public string? Priority { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public int? CarrierId { get; set; }
        public int? WarehouseId { get; set; }
        public decimal? ShippingFee { get; set; }
        public decimal? EstimatedCost { get; set; }
        public string[]? Handling { get; set; }
        public string? Notes { get; set; }
    }

    public class ShipmentUpdateRequest
    {
        public int ShipmentId { get; set; }
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string CargoType { get; set; } = string.Empty;
        public decimal? Weight { get; set; }
        public decimal? Volume { get; set; }
        public string Priority { get; set; } = "Standard";
        public DateTime ScheduledDate { get; set; }
        public int CarrierId { get; set; }
        public int WarehouseId { get; set; }
        public decimal? ShippingFee { get; set; }
        public decimal? EstimatedCost { get; set; }
        public string[] Handling { get; set; } = Array.Empty<string>();
        public string? Notes { get; set; }
    }
}
