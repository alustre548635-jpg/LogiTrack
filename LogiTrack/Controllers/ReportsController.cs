using LogiTrack.Data;
using LogiTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LogiTrack.Controllers
{
    public class ReportsController : Controller
    {
        private readonly LogiTrackDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ReportsController(LogiTrackDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            var userIdText = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrWhiteSpace(userIdText))
                return RedirectToAction("Login", "Account");

            ViewData["Title"] = "Reports";
            ViewData["ActiveNav"] = "reports";

            var now = DateTime.Now;
            var fromDate = from?.Date ?? now.Date.AddDays(-13);
            var toDate = (to?.Date ?? now.Date).AddDays(1).AddTicks(-1);
            if (toDate < fromDate)
            {
                (fromDate, toDate) = (toDate.Date, fromDate.AddDays(1).AddTicks(-1));
            }

            var shipmentsQuery = _db.Shipments.Where(s => s.CreatedAt >= fromDate && s.CreatedAt <= toDate);
            var invoicesQuery = _db.Invoices.Where(i => i.IssueDate >= fromDate && i.IssueDate <= toDate);
            var trackingQuery = _db.TrackingEvents.Where(e => e.EventTime >= fromDate && e.EventTime <= toDate);
            var activityQuery = _db.ActivityLogs.Where(a => a.Timestamp >= fromDate && a.Timestamp <= toDate);

            var totalShipments = await shipmentsQuery.CountAsync();
            var pendingShipments = await shipmentsQuery.CountAsync(s => s.Status == "Pending");
            var inTransitShipments = await shipmentsQuery.CountAsync(s => s.Status == "In Transit");
            var deliveredShipments = await shipmentsQuery.CountAsync(s => s.Status == "Delivered" || s.Status == "Completed");

            var totalInvoices = await invoicesQuery.CountAsync();
            var pendingInvoices = await invoicesQuery.CountAsync(i => i.Status == "Pending");
            var paidInvoices = await invoicesQuery.CountAsync(i => i.Status == "Paid");
            var totalInvoicedAmount = await invoicesQuery.SumAsync(i => (decimal?)i.Amount) ?? 0m;

            var totalTrackingEvents = await trackingQuery.CountAsync();
            var totalActivities = await activityQuery.CountAsync();

            var shipmentsByDayRaw = await shipmentsQuery
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new DailyCountPoint { Day = g.Key, Count = g.Count() })
                .OrderBy(p => p.Day)
                .ToListAsync();

            var invoiceAmountByDayRaw = await invoicesQuery
                .GroupBy(i => i.IssueDate.Date)
                .Select(g => new DailyAmountPoint { Day = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderBy(p => p.Day)
                .ToListAsync();

            // Fill missing days for smoother charts
            var days = Enumerable.Range(0, (int)Math.Max(0, (toDate.Date - fromDate.Date).TotalDays) + 1)
                .Select(i => fromDate.Date.AddDays(i))
                .ToList();

            var shipmentsByDay = days
                .Select(d => new DailyCountPoint
                {
                    Day = d,
                    Count = shipmentsByDayRaw.FirstOrDefault(x => x.Day == d)?.Count ?? 0
                })
                .ToList();

            var invoiceAmountByDay = days
                .Select(d => new DailyAmountPoint
                {
                    Day = d,
                    Amount = invoiceAmountByDayRaw.FirstOrDefault(x => x.Day == d)?.Amount ?? 0m
                })
                .ToList();

            // Financial Calculations
            var totalRevenue = await shipmentsQuery.SumAsync(s => (decimal?)s.ShippingFee) ?? 0m;
            var totalOpCost = await shipmentsQuery.SumAsync(s => (decimal?)s.EstimatedCost) ?? 0m;
            var netProfit = totalRevenue - totalOpCost;

            var model = new ReportsViewModel
            {
                From = fromDate,
                To = toDate,
                TotalShipments = totalShipments,
                PendingShipments = pendingShipments,
                InTransitShipments = inTransitShipments,
                DeliveredShipments = deliveredShipments,
                TotalInvoices = totalInvoices,
                PendingInvoices = pendingInvoices,
                PaidInvoices = paidInvoices,
                TotalInvoicedAmount = totalInvoicedAmount,
                TotalTrackingEvents = totalTrackingEvents,
                TotalActivities = totalActivities,
                TotalRevenue = totalRevenue,
                TotalOperationalCost = totalOpCost,
                NetProfit = netProfit,
                ShipmentsByDay = shipmentsByDay,
                InvoiceAmountByDay = invoiceAmountByDay,
                RecentShipments = await shipmentsQuery
                    .Include(s => s.Carrier)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(8)
                    .ToListAsync(),
                RecentInvoices = await invoicesQuery
                    .Include(i => i.Carrier)
                    .OrderByDescending(i => i.IssueDate)
                    .Take(8)
                    .ToListAsync(),
                RecentActivities = await activityQuery
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(8)
                    .ToListAsync()
            };
            return View(model);
        }

        public async Task<IActionResult> ExportPdf(DateTime? from, DateTime? to)
        {
            var userIdText = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrWhiteSpace(userIdText))
                return RedirectToAction("Login", "Account");

            var userId = int.Parse(userIdText);
            var user = await _db.Users.FindAsync(userId);
            var managerName = user?.FullName ?? "Manager";

            QuestPDF.Settings.License = LicenseType.Community;

            var now = DateTime.Now;
            var fromDate = from?.Date ?? now.Date.AddDays(-13);
            var toDate = (to?.Date ?? now.Date).AddDays(1).AddTicks(-1);
            if (toDate < fromDate)
            {
                (fromDate, toDate) = (toDate.Date, fromDate.AddDays(1).AddTicks(-1));
            }

            var shipmentsQuery = _db.Shipments.Include(s => s.Carrier).Where(s => s.CreatedAt >= fromDate && s.CreatedAt <= toDate);
            var invoicesQuery = _db.Invoices.Where(i => i.IssueDate >= fromDate && i.IssueDate <= toDate);
            var trackingQuery = _db.TrackingEvents.Where(e => e.EventTime >= fromDate && e.EventTime <= toDate);

            var totalShipments = await shipmentsQuery.CountAsync();
            var pendingShipments = await shipmentsQuery.CountAsync(s => s.Status == "Pending");
            var inTransitShipments = await shipmentsQuery.CountAsync(s => s.Status == "In Transit");
            var deliveredShipments = await shipmentsQuery.CountAsync(s => s.Status == "Delivered" || s.Status == "Completed");

            // Financial Calculations
            var totalRevenue = await shipmentsQuery.SumAsync(s => (decimal?)s.ShippingFee) ?? 0m;
            var totalOpCost = await shipmentsQuery.SumAsync(s => (decimal?)s.EstimatedCost) ?? 0m;
            var netProfit = totalRevenue - totalOpCost;

            var totalInvoices = await invoicesQuery.CountAsync();
            var totalInvoicedAmount = await invoicesQuery.SumAsync(i => (decimal?)i.Amount) ?? 0m;
            
            var totalTrackingEvents = await trackingQuery.CountAsync();

            var recentShipments = await shipmentsQuery
                .Include(s => s.Carrier)
                .OrderByDescending(s => s.CreatedAt)
                .Take(15)
                .ToListAsync();

            var logoPath = Path.Combine(_env.WebRootPath, "Images", "Logo.png");
            byte[] logoBytes = null;
            if (System.IO.File.Exists(logoPath))
            {
                logoBytes = System.IO.File.ReadAllBytes(logoPath);
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Element(c => ComposeHeader(c, logoBytes, fromDate, toDate));
                    page.Content().Element(c => ComposeContent(c, totalShipments, pendingShipments, inTransitShipments, deliveredShipments, totalRevenue, totalOpCost, netProfit, totalInvoices, totalInvoicedAmount, totalTrackingEvents, recentShipments, managerName));
                    page.Footer().Element(ComposeFooter);
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"LogiTrack_Operational_Report_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        private void ComposeHeader(IContainer container, byte[] logoBytes, DateTime fromDate, DateTime toDate)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("LogiTrack Systems Inc.").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken3);
                    column.Item().Text("123 Logistics Way, Metro Manila, Philippines").FontSize(8).FontColor(Colors.Grey.Medium);
                    column.Item().Text("Contact: +63 912 345 6789 | support@logitrack.com").FontSize(8).FontColor(Colors.Grey.Medium);
                    
                    column.Item().PaddingTop(10).Text("OPERATIONAL PERFORMANCE REPORT").FontSize(12).SemiBold().FontColor(Colors.Grey.Darken3);
                    column.Item().Text($"Reporting Period: {fromDate:MMM dd, yyyy} - {toDate:MMM dd, yyyy}").FontSize(9);
                });

                row.ConstantItem(100).Column(column => {
                    if (logoBytes != null)
                    {
                        column.Item().AlignRight().Image(logoBytes).FitWidth();
                    }
                    column.Item().AlignRight().Text($"Generated: {DateTime.Now:MM/dd/yyyy HH:mm}").FontSize(8).Italic();
                });
            });
        }

        private void ComposeContent(IContainer container, int totalShipments, int pendingShipments, int inTransitShipments, int deliveredShipments, decimal totalRevenue, decimal totalOpCost, decimal netProfit, int totalInvoices, decimal totalInvoicedAmount, int totalTrackingEvents, List<Shipment> recentShipments, string managerName)
        {
            var phCulture = new System.Globalization.CultureInfo("en-PH");

            container.PaddingVertical(20).Column(column =>
            {
                column.Spacing(15);

                // KPI Section
                column.Item().Row(row => {
                    row.RelativeItem().Column(c => {
                        c.Item().Text("Shipment Volume").FontSize(11).SemiBold().Underline();
                        c.Item().Text($"Total Shipments: {totalShipments}");
                        c.Item().Text($"Delivered/Completed: {deliveredShipments}").FontColor(Colors.Green.Darken2);
                        c.Item().Text($"In Transit: {inTransitShipments}").FontColor(Colors.Blue.Medium);
                        c.Item().Text($"Pending: {pendingShipments}").FontColor(Colors.Orange.Medium);
                    });

                    row.RelativeItem().Column(c => {
                        c.Item().Text("Financial Summary").FontSize(11).SemiBold().Underline();
                        c.Item().Text($"Total Revenue: {totalRevenue.ToString("C2", phCulture)}");
                        c.Item().Text($"Operational Cost: {totalOpCost.ToString("C2", phCulture)}").FontColor(Colors.Red.Medium);
                        c.Item().Text($"Net Profit: {netProfit.ToString("C2", phCulture)}").SemiBold().FontColor(netProfit >= 0 ? Colors.Green.Darken3 : Colors.Red.Darken3);
                    });

                    row.RelativeItem().Column(c => {
                        c.Item().Text("Invoicing & Events").FontSize(11).SemiBold().Underline();
                        c.Item().Text($"Invoices Issued: {totalInvoices}");
                        c.Item().Text($"Invoiced Amount: {totalInvoicedAmount.ToString("C2", phCulture)}");
                        c.Item().Text($"System Events: {totalTrackingEvents}");
                    });
                });

                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Shipment Table
                column.Item().Text("Detailed Shipment Overview").FontSize(12).SemiBold().FontColor(Colors.Blue.Darken2);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(85);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.ConstantColumn(80);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Code");
                        header.Cell().Element(CellStyle).Text("Route (From-To)");
                        header.Cell().Element(CellStyle).Text("Carrier");
                        header.Cell().Element(CellStyle).Text("Status");
                        header.Cell().Element(CellStyle).AlignRight().Text("Fee");

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        }
                    });

                    foreach (var s in recentShipments)
                    {
                        table.Cell().Element(ValueStyle).Text(s.ShipmentCode);
                        table.Cell().Element(ValueStyle).Text($"{s.Origin} → {s.Destination}");
                        table.Cell().Element(ValueStyle).Text(s.Carrier?.Name ?? "N/A");
                        table.Cell().Element(ValueStyle).Text(s.Status);
                        table.Cell().Element(ValueStyle).AlignRight().Text(s.ShippingFee.ToString("C2", phCulture));

                        static IContainer ValueStyle(IContainer container)
                        {
                            return container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                        }
                    }
                });

                // Signature Area
                column.Item().PaddingTop(30).Row(row => {
                    row.RelativeItem().Column(c => {
                        c.Item().PaddingTop(20).BorderTop(1).AlignCenter().Text($"Prepared By ({managerName})").FontSize(8);
                    });
                    row.ConstantItem(50);
                    row.RelativeItem().Column(c => {
                        c.Item().PaddingTop(20).BorderTop(1).AlignCenter().Text("Approved By (Finance Director)").FontSize(8);
                    });
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(x =>
            {
                x.Span("Page ");
                x.CurrentPageNumber();
                x.Span(" of ");
                x.TotalPages();
            });
        }
    }
}
