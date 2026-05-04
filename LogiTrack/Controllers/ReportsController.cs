using LogiTrack.Data;
using LogiTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Controllers
{
    public class ReportsController : Controller
    {
        private readonly LogiTrackDbContext _db;

        public ReportsController(LogiTrackDbContext db)
        {
            _db = db;
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
                ShipmentsByDay = shipmentsByDay,
                InvoiceAmountByDay = invoiceAmountByDay,
                RecentShipments = await shipmentsQuery
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
    }
}

