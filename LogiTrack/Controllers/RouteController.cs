using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogiTrack.Data;
using LogiTrack.Models;
using LogiTrack.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LogiTrack.Controllers
{
    public class RouteController : Controller
    {
        private readonly LogiTrackDbContext _db;
        private readonly IAuditLogService _auditLogService;

        public RouteController(LogiTrackDbContext db, IAuditLogService auditLogService)
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

            var availableShipments = await _db.Shipments
                .Include(s => s.Warehouse)
                .Where(s => (s.Status == "Pending" || s.Status == "Received at Warehouse")
                         && s.RouteId == null)
                .OrderByDescending(s => s.ShipmentId)
                .Select(s => new ShipmentOptionViewModel
                {
                    ShipmentId = s.ShipmentId,
                    ShipmentCode = s.ShipmentCode,
                    Origin = s.Origin,
                    Destination = s.Destination,
                    Weight = s.Weight,
                    ScheduledDate = s.ScheduledDate,
                    WarehouseName = s.Warehouse != null ? s.Warehouse.Name : string.Empty
                })
                .ToListAsync();

            var availableDrivers = await _db.Drivers
                .Where(d => d.Status == "Available" || d.Status == "On Duty")
                .Select(d => new DriverOptionViewModel
                {
                    DriverId = d.DriverId,
                    FullName = d.FullName,
                    VehiclePlate = d.VehiclePlate,
                    VehicleType = d.VehicleType
                })
                .ToListAsync();

            return View(new RouteOptimizationViewModel
            {
                HubOptions = hubOptions,
                RecentRoutes = recentRoutes,
                AvailableShipments = availableShipments,
                AvailableDrivers = availableDrivers
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
                fuel = $"₱{(baseDistance * 3.8m * loadFactor):0}",
                toll = $"₱{(baseDistance * 0.45m):0}",
                carbon = "High",
                highlighted = true,
                rawDistance = baseDistance,
                rawMinutes = baseMinutes,
                rawFuel = baseDistance * 3.8m * loadFactor,
                rawToll = baseDistance * 0.45m
            };
            var cheapest = new
            {
                id = $"CHEAP-{(factor + 31) % 100000:D5}",
                label = "Cheapest",
                dist = $"{baseDistance + 42} km",
                eta = $"{Math.Max(1, (baseMinutes + 95) / 60)}h {(baseMinutes + 95) % 60}m",
                fuel = $"₱{((baseDistance + 42) * 2.9m * loadFactor):0}",
                toll = $"₱{((baseDistance + 42) * 0.25m):0}",
                carbon = "Medium",
                highlighted = false,
                rawDistance = baseDistance + 42,
                rawMinutes = baseMinutes + 95,
                rawFuel = (baseDistance + 42) * 2.9m * loadFactor,
                rawToll = (baseDistance + 42) * 0.25m
            };
            var eco = new
            {
                id = $"ECO-{(factor + 67) % 100000:D5}",
                label = "Eco",
                dist = $"{baseDistance + 25} km",
                eta = $"{Math.Max(1, (baseMinutes + 60) / 60)}h {(baseMinutes + 60) % 60}m",
                fuel = $"₱{((baseDistance + 25) * 2.5m * loadFactor):0}",
                toll = $"₱{((baseDistance + 25) * 0.30m):0}",
                carbon = "Low",
                highlighted = false,
                rawDistance = baseDistance + 25,
                rawMinutes = baseMinutes + 60,
                rawFuel = (baseDistance + 25) * 2.5m * loadFactor,
                rawToll = (baseDistance + 25) * 0.30m
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
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Select([FromForm] RouteSelectRequest model)
        {
            if (model.ShipmentIds == null || !model.ShipmentIds.Any())
            {
                return Json(new { success = false, message = "Please select at least one shipment for this route." });
            }

            var nextNumber = await _db.Routes.CountAsync() + 1;

            var route = new ShipmentRoute
            {
                RouteNumber = $"RT-{DateTime.Now:yyyy}-{nextNumber:D4}",
                DriverId = model.DriverId,
                StartHub = model.StartHub ?? "Unknown",
                EndHub = model.EndHub ?? "Unknown",
                VehicleType = model.VehicleType ?? "Truck",
                LoadCapacity = model.LoadCapacity,
                DistanceKm = model.DistanceKm,
                EstimatedMinutes = model.EstimatedMinutes,
                FuelCostEstimate = model.FuelCostEstimate,
                TollCost = model.TollCost,
                OptimizationType = model.OptimizationType ?? "Fastest",
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            _db.Routes.Add(route);
            await _db.SaveChangesAsync();

            var shipments = await _db.Shipments.Where(s => model.ShipmentIds.Contains(s.ShipmentId)).ToListAsync();
            foreach(var s in shipments)
            {
                s.RouteId = route.RouteId;
                s.Status = "In Transit";
            }
            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                GetCurrentUserId(),
                $"Generated new Route Manifest: {route.RouteNumber} ({shipments.Count} shipments)",
                "Routes",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new { success = true, message = "Route manifest successfully generated." });
        }
        [HttpGet]
        public async Task<IActionResult> Export()
        {
            var routes = await _db.Routes
                .Include(r => r.Driver)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var phCulture = new System.Globalization.CultureInfo("en-PH");

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("LOGITRACK — Route Manifest Report")
                                .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                            row.ConstantItem(180).AlignRight().Text($"Generated: {DateTime.Now:MM/dd/yyyy HH:mm}")
                                .FontSize(8).Italic();
                        });
                        column.Item().PaddingTop(4).Text($"Total Routes: {routes.Count}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                        column.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(60);   // Route #
                            columns.RelativeColumn();      // Start Hub
                            columns.RelativeColumn();      // End Hub
                            columns.RelativeColumn();      // Driver
                            columns.ConstantColumn(55);    // Distance
                            columns.ConstantColumn(55);    // ETA
                            columns.ConstantColumn(70);    // Fuel
                            columns.ConstantColumn(65);    // Toll
                            columns.ConstantColumn(60);    // Type
                            columns.ConstantColumn(50);    // Status
                        });

                        table.Header(header =>
                        {
                            string[] headers = { "Route #", "Start Hub", "End Hub", "Driver", "Dist (km)", "ETA", "Fuel Cost", "Toll Cost", "Type", "Status" };
                            foreach (var h in headers)
                            {
                                header.Cell().Element(CellStyle).Text(h);
                            }

                            static IContainer CellStyle(IContainer c)
                                => c.DefaultTextStyle(x => x.SemiBold().FontSize(8))
                                    .PaddingVertical(5).PaddingHorizontal(3)
                                    .BorderBottom(1).BorderColor(Colors.Black);
                        });

                        foreach (var r in routes)
                        {
                            var driverName = r.Driver?.FullName ?? "Unassigned";
                            var eta = r.EstimatedMinutes.HasValue ? $"{r.EstimatedMinutes / 60}h {r.EstimatedMinutes % 60}m" : "—";

                            table.Cell().Element(VS).Text(r.RouteNumber ?? $"RT-{r.RouteId}");
                            table.Cell().Element(VS).Text(r.StartHub);
                            table.Cell().Element(VS).Text(r.EndHub);
                            table.Cell().Element(VS).Text(driverName);
                            table.Cell().Element(VS).AlignRight().Text($"{r.DistanceKm:0.#}");
                            table.Cell().Element(VS).Text(eta);
                            table.Cell().Element(VS).AlignRight().Text(r.FuelCostEstimate?.ToString("C0", phCulture) ?? "—");
                            table.Cell().Element(VS).AlignRight().Text(r.TollCost?.ToString("C0", phCulture) ?? "—");
                            table.Cell().Element(VS).Text(r.OptimizationType ?? "—");
                            table.Cell().Element(VS).Text(r.Status);

                            static IContainer VS(IContainer c)
                                => c.PaddingVertical(4).PaddingHorizontal(3)
                                    .BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                        }
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("LogiTrack Route Manifest • Page ");
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"LogiTrack_Routes_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
    }

    public class RouteSelectRequest
    {
        public List<int> ShipmentIds { get; set; } = new();
        public int? DriverId { get; set; }
        public string StartHub { get; set; } = string.Empty;
        public string EndHub { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public decimal LoadCapacity { get; set; }
        public decimal DistanceKm { get; set; }
        public int EstimatedMinutes { get; set; }
        public decimal FuelCostEstimate { get; set; }
        public decimal TollCost { get; set; }
        public string OptimizationType { get; set; } = "Fastest";
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
