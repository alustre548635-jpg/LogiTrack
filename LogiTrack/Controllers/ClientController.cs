using LogiTrack.Data;
using LogiTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Controllers
{
    public class ClientController : Controller
    {
        private readonly LogiTrackDbContext _db;

        public ClientController(LogiTrackDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var userIdText = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrWhiteSpace(userIdText))
                return RedirectToAction("Login", "Account");

            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Admin");

            if (string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Manager");

            if (!int.TryParse(userIdText, out var userId))
                return RedirectToAction("Login", "Account");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return RedirectToAction("Login", "Account");

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

            var hasDashboard = assignedModules.Any(m => string.Equals(m, "Dashboard", StringComparison.OrdinalIgnoreCase));
            if (!hasDashboard)
            {
                // If they don't have dashboard access, try to send them to warehouse
                if (assignedModules.Any(m => string.Equals(m, "Warehouses", StringComparison.OrdinalIgnoreCase)))
                    return RedirectToAction("Index", "Warehouse");
                
                // Fallback to shipments or tracking
                if (assignedModules.Any(m => string.Equals(m, "Shipments", StringComparison.OrdinalIgnoreCase)))
                    return RedirectToAction("Index", "Shipment");

                return RedirectToAction("Index", "Tracking");
            }

            var canViewAuditLogs =
                assignedModules.Any(m => string.Equals(m, "Audit Logs", StringComparison.OrdinalIgnoreCase));

            var model = new ClientDashboardViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                CanViewAuditLogs = canViewAuditLogs,
                AssignedModules = assignedModules,
                TotalMyShipments = await _db.Shipments.CountAsync(s => s.CreatedByUserId == userId),
                MyPendingShipments = await _db.Shipments.CountAsync(s => s.CreatedByUserId == userId && s.Status == "Pending"),
                MyInTransitShipments = await _db.Shipments.CountAsync(s => s.CreatedByUserId == userId && s.Status == "In Transit"),
                MyDeliveredShipments = await _db.Shipments.CountAsync(s => s.CreatedByUserId == userId && s.Status == "Delivered"),
                MyCompletedShipments = await _db.Shipments.CountAsync(s => s.CreatedByUserId == userId && s.Status == "Completed"),
                MyCancelledShipments = await _db.Shipments.CountAsync(s => s.CreatedByUserId == userId && s.Status == "Cancelled"),
                RecentShipments = await _db.Shipments
                    .Where(s => s.CreatedByUserId == userId)
                    .Include(s => s.Carrier)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(6)
                    .ToListAsync(),
                RecentActivities = canViewAuditLogs
                    ? await _db.ActivityLogs
                        .Where(a => a.UserId == userId)
                        .OrderByDescending(a => a.Timestamp)
                        .Take(8)
                        .ToListAsync()
                    : new List<ActivityLog>()
            };

            return View(model);
        }
    }
}
