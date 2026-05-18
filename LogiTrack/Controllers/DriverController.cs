using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogiTrack.Data;
using LogiTrack.Models;

namespace LogiTrack.Controllers
{
    public class DriverController : Controller
    {
        private readonly LogiTrackDbContext _db;

        public DriverController(LogiTrackDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var driver = await _db.Drivers
                .Include(d => d.Warehouse)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var activeRoute = await _db.Routes
                .Include(r => r.Shipments)
                .Where(r => r.DriverId == driver.DriverId && (r.Status == "In Transit" || r.Status == "Planned"))
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            var model = new DriverDashboardViewModel
            {
                Driver = driver,
                ActiveRoute = activeRoute,
                Shipments = activeRoute?.Shipments.ToList() ?? new List<Shipment>(),
                TotalDeliveries = await _db.Shipments.CountAsync(s => s.Route != null && s.Route.DriverId == driver.DriverId && s.Status == "Delivered"),
                WeeklyOnTimeRate = driver.OnTimeDeliveryRate,
                SafetyScore = driver.SafetyScore,
                RecentActivity = await _db.ActivityLogs
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRouteStatus(int routeId, string status)
        {
            var route = await _db.Routes.FindAsync(routeId);
            if (route != null)
            {
                route.Status = status;
                await _db.SaveChangesAsync();
                TempData["DriverSuccess"] = $"Route status updated to {status}.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
