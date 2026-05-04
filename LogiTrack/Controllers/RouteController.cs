using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogiTrack.Data;
using LogiTrack.Models;

namespace LogiTrack.Controllers
{
    public class RouteController : Controller
    {
        private readonly LogiTrackDbContext _db;

        public RouteController(LogiTrackDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var hubOptions = await _db.Warehouses
                .Where(w => w.IsActive)
                .OrderBy(w => w.Name)
                .Select(w => w.Name)
                .Distinct()
                .ToListAsync();

            if (hubOptions.Count == 0)
            {
                hubOptions = new List<string> { "Manila Central Hub", "QC Distribution Center", "Cebu City Hub", "Davao Distribution" };
            }

            var recentRoutes = await _db.Routes
                .OrderByDescending(r => r.CreatedAt)
                .Take(8)
                .Select(r => new RouteHistoryItemViewModel
                {
                    RouteId = r.RouteId,
                    StartHub = r.StartHub,
                    EndHub = r.EndHub,
                    DistanceKm = r.DistanceKm ?? 0,
                    EstimatedMinutes = r.EstimatedMinutes ?? 0,
                    OptimizationType = r.OptimizationType,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return View(new RouteOptimizationViewModel
            {
                HubOptions = hubOptions,
                RecentRoutes = recentRoutes
            });
        }

        [HttpPost]
        public JsonResult Optimize([FromForm] RouteOptimizeRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.StartHub) || string.IsNullOrWhiteSpace(model.EndHub))
            {
                return Json(new { success = false, message = "Start hub and end hub are required." });
            }

            var waypoints = model.Waypoints?.Where(w => !string.IsNullOrWhiteSpace(w)).ToList() ?? new List<string>();
            var factor = Math.Abs($"{model.StartHub}:{model.EndHub}:{string.Join('|', waypoints)}".GetHashCode());
            var baseDistance = 120 + (factor % 700);
            var baseMinutes = 140 + (factor % 480);
            var loadFactor = Math.Clamp(model.LoadCapacity <= 0 ? 75 : model.LoadCapacity, 10, 100) / 100m;

            var fastest = new
            {
                id = $"FAST-{factor % 100000:D5}",
                label = "Fastest",
                dist = $"{baseDistance} km",
                eta = $"{Math.Max(1, baseMinutes / 60)}h {baseMinutes % 60}m",
                fuel = $"PHP {(baseDistance * 3.8m * loadFactor):0}",
                toll = $"PHP {(baseDistance * 0.45m):0}",
                carbon = "High",
                highlighted = true
            };
            var cheapest = new
            {
                id = $"CHEAP-{(factor + 31) % 100000:D5}",
                label = "Cheapest",
                dist = $"{baseDistance + 42} km",
                eta = $"{Math.Max(1, (baseMinutes + 95) / 60)}h {(baseMinutes + 95) % 60}m",
                fuel = $"PHP {((baseDistance + 42) * 2.9m * loadFactor):0}",
                toll = $"PHP {((baseDistance + 42) * 0.25m):0}",
                carbon = "Medium",
                highlighted = false
            };
            var eco = new
            {
                id = $"ECO-{(factor + 67) % 100000:D5}",
                label = "Eco",
                dist = $"{baseDistance + 25} km",
                eta = $"{Math.Max(1, (baseMinutes + 60) / 60)}h {(baseMinutes + 60) % 60}m",
                fuel = $"PHP {((baseDistance + 25) * 2.5m * loadFactor):0}",
                toll = $"PHP {((baseDistance + 25) * 0.30m):0}",
                carbon = "Low",
                highlighted = false
            };

            return Json(new
            {
                success = true,
                summary = new
                {
                    distance = fastest.dist,
                    eta = fastest.eta,
                    fuel = fastest.fuel,
                    toll = fastest.toll
                },
                routes = new[] { fastest, cheapest, eco }
            });
        }

        [HttpPost]
        public async Task<JsonResult> Select([FromForm] string id)
        {
            if (!int.TryParse(id, out var routeId))
            {
                return Json(new { success = true, message = "Route option selected." });
            }

            var route = await _db.Routes.FirstOrDefaultAsync(r => r.RouteId == routeId);
            if (route == null)
            {
                return Json(new { success = false, message = "Route not found." });
            }

            route.Status = "Selected";
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Route selected." });
        }
    }

    public class RouteOptimizeRequest
    {
        public string StartHub { get; set; } = string.Empty;
        public string EndHub { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public DateTime? DepartureTime { get; set; }
        public int LoadCapacity { get; set; } = 75;
        public string OptPriority { get; set; } = "Fastest";
        public List<string> Waypoints { get; set; } = new();
    }
}
