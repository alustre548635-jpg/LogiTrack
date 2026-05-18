using LogiTrack.Data;
using LogiTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Controllers
{
    public class ManagerController : Controller
    {
        private readonly LogiTrackDbContext _db;

        public ManagerController(LogiTrackDbContext db)
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

            if (!string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Client");

            if (!int.TryParse(userIdText, out var userId))
                return RedirectToAction("Login", "Account");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new ManagerDashboardViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                TotalShipments = await _db.Shipments.CountAsync(),
                PendingShipments = await _db.Shipments.CountAsync(s => s.Status == "Pending"),
                InTransitShipments = await _db.Shipments.CountAsync(s => s.Status == "In Transit"),
                DeliveredShipments = await _db.Shipments.CountAsync(s => s.Status == "Delivered"),
                RecentShipments = await _db.Shipments
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(8)
                    .ToListAsync(),
                RecentActivities = await _db.ActivityLogs
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(50)
                    .ToListAsync()
            };

            return View(model);
        }
    }
}

