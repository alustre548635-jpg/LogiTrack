using LogiTrack.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace LogiTrack.Controllers
{
    public class TrackingController : Controller
    {
        private readonly LogiTrackDbContext _db;

        public TrackingController(LogiTrackDbContext db)
        {
            _db = db;
        }

        public IActionResult Index(string trackingNumber = null)
        {
            ViewBag.TargetTrackingNumber = trackingNumber;
            ViewBag.HideSidebar = !string.IsNullOrEmpty(trackingNumber);
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> ActiveShipments(string trackingNumber = null)
        {
            var query = _db.Shipments
                .Include(s => s.Route)
                    .ThenInclude(r => r.Driver)
                .AsQueryable();

            if (!string.IsNullOrEmpty(trackingNumber))
            {
                query = query.Where(s => s.ShipmentCode == trackingNumber);
            }
            else
            {
                query = query.OrderByDescending(s => s.CreatedAt);
            }

            var shipments = await query.Select(s => new {
                    id = s.ShipmentCode,
                    driver = s.Route != null && s.Route.Driver != null ? s.Route.Driver.FullName : "System Assigned",
                    plate = s.Route != null && s.Route.Driver != null ? s.Route.Driver.VehiclePlate : "N/A",
                    origin = s.Origin,
                    dest = s.Destination,
                    progress = s.Status == "Pending" ? 10 : (s.Status == "In Transit" ? 50 : 80),
                    eta = s.Route != null && s.Route.EstimatedMinutes.HasValue ? $"{s.Route.EstimatedMinutes / 60}h {s.Route.EstimatedMinutes % 60}m" : "TBD",
                    status = s.Status
                }).ToListAsync();

            return Json(shipments);
        }

        [HttpGet]
        public async Task<JsonResult> Timeline(string id)
        {
            if (string.IsNullOrEmpty(id)) return Json(new object[0]);

            var shipment = await _db.Shipments.FirstOrDefaultAsync(s => s.ShipmentCode == id);
            if (shipment == null) return Json(new object[0]);

            var events = await _db.TrackingEvents
                .Where(e => e.ShipmentId == shipment.ShipmentId)
                .OrderBy(e => e.EventTime)
                .Select(e => new {
                    label = e.EventType,
                    location = e.Location ?? "System",
                    time = e.EventTime.ToString("yyyy-MM-dd HH:mm"),
                    done = true,
                    active = false
                })
                .ToListAsync();

            var steps = new List<object>();

            // Always add Order Created
            steps.Add(new {
                label = "Order Created",
                location = "System",
                time = shipment.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                done = true,
                active = events.Count == 0 && shipment.Status != "Delivered"
            });

            foreach(var e in events)
            {
                steps.Add(e);
            }

            var isCancelled = string.Equals(shipment.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
            var isDelivered = string.Equals(shipment.Status, "Delivered", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(shipment.Status, "Completed", StringComparison.OrdinalIgnoreCase);

            // Mark last event as active only if still in progress
            if (events.Count > 0 && !isDelivered && !isCancelled)
            {
                var last = events.Last();
                steps[steps.Count - 1] = new {
                    label = last.label,
                    location = last.location,
                    time = last.time,
                    done = true,
                    active = true,
                    cancelled = false
                };
            }

            if (isCancelled)
            {
                // Add a Cancelled terminal step
                steps.Add(new {
                    label = "Shipment Cancelled",
                    location = "System",
                    time = shipment.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    done = true,
                    active = true,
                    cancelled = true
                });
            }
            else if (!isDelivered)
            {
                steps.Add(new {
                    label = "Out for Delivery",
                    location = "Destination Hub",
                    time = "—",
                    done = false,
                    active = false,
                    cancelled = false
                });
                steps.Add(new {
                    label = "Delivered",
                    location = shipment.Destination,
                    time = "—",
                    done = false,
                    active = false,
                    cancelled = false
                });
            }

            return Json(steps);
        }

        [HttpGet]
        public async Task<JsonResult> Alerts()
        {
            var events = await _db.TrackingEvents
                .Include(e => e.Shipment)
                .Where(e => e.EventType == "Alert" || e.EventType == "Delayed")
                .OrderByDescending(e => e.EventTime)
                .Take(5)
                .Select(e => new {
                    id = e.Shipment.ShipmentCode,
                    msg = e.Notes ?? "An issue was reported.",
                    severity = e.EventType == "Alert" ? "critical" : "warning",
                    time = e.EventTime.ToString("yyyy-MM-dd HH:mm")
                })
                .ToListAsync();

            return Json(events);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AcknowledgeAlert([FromForm] string shipmentId)
        {
            // For the mockup, we just acknowledge without DB change
            return Json(new { success = true });
        }
    }
}
