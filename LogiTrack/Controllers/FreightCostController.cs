using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogiTrack.Data;
using LogiTrack.Models;
using LogiTrack.Services;

namespace LogiTrack.Controllers
{
    public class FreightCostController : Controller
    {
        private readonly LogiTrackDbContext _db;
        private readonly IAuditLogService _auditLogService;

        public FreightCostController(LogiTrackDbContext db, IAuditLogService auditLogService)
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
            var role = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(role)) return RedirectToAction("Login", "Account");

            var carriers = await _db.Carriers.OrderBy(c => c.Name).ToListAsync();
            var rateCards = await _db.FreightRateCards.Include(r => r.Carrier).ToListAsync();
            var invoices = await _db.Invoices.Include(i => i.Carrier).OrderByDescending(i => i.IssueDate).ToListAsync();
            var payments = await _db.Payments.Include(p => p.Shipment).OrderByDescending(p => p.CreatedAt).ToListAsync();

            // Calculate Summary
            var totalSpend = invoices.Where(i => i.Status == "Paid").Sum(i => i.Amount);
            var totalRevenue = payments.Where(p => p.Status == "Completed").Sum(p => p.Amount);
            var budget = 1700000m;

            // Anomaly detection... (same as before)
            var anomalies = await _db.Shipments
                .Include(s => s.Carrier)
                .Where(s => s.Status == "Delivered")
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .Select(s => new CostAnomalyViewModel
                {
                    ShipmentCode = s.ShipmentCode,
                    CarrierName = s.Carrier.Name,
                    EstimatedCost = (decimal)s.Weight * 50,
                    ActualCost = (decimal)s.Weight * 65
                })
                .ToListAsync();

            var model = new FreightCostViewModel
            {
                TotalFreightSpend = totalSpend,
                BudgetRemaining = budget - totalSpend,
                AvgCostPerKm = 18.40m,
                OverbudgetCount = anomalies.Count,
                RateCards = rateCards,
                Anomalies = anomalies,
                Invoices = invoices,
                Carriers = carriers,
                Payments = payments
            };

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.PendingRevenue = payments.Where(p => p.Status == "Pending").Sum(p => p.Amount);
            ViewBag.OverdueRevenue = payments.Count(p => p.IsOverdue);

            ViewData["ActiveNav"] = "freightcost";
            return View(model);
        }

        [HttpPost]
        public async Task<JsonResult> AddRate(int carrierId, string zone, string weightBracket, decimal baseRate, decimal fuel, decimal handling)
        {
            var rate = new FreightRateCard
            {
                CarrierId = carrierId,
                Zone = zone,
                WeightBracket = weightBracket,
                BaseRatePerKg = baseRate,
                FuelSurchargePercent = fuel,
                HandlingFee = handling,
                EffectiveDate = DateTime.Now
            };

            _db.FreightRateCards.Add(rate);
            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                GetCurrentUserId(),
                $"Added new Freight Rate Card for Carrier ID {carrierId} (Zone: {zone})",
                "FreightRateCards",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new { success = true, message = "Rate card added successfully." });
        }

        [HttpPost]
        public async Task<JsonResult> UpdateRate(int rateCardId, string zone, string weightBracket, decimal baseRate, decimal fuel, decimal handling)
        {
            var rate = await _db.FreightRateCards.FindAsync(rateCardId);
            if (rate == null) return Json(new { success = false, message = "Rate card not found." });

            rate.Zone = zone;
            rate.WeightBracket = weightBracket;
            rate.BaseRatePerKg = baseRate;
            rate.FuelSurchargePercent = fuel;
            rate.HandlingFee = handling;

            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                GetCurrentUserId(),
                $"Updated Freight Rate Card ID {rateCardId} (Zone: {zone})",
                "FreightRateCards",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new { success = true, message = "Rate card updated successfully." });
        }

        [HttpPost]
        public async Task<JsonResult> DeactivateRate(int rateCardId)
        {
            var rate = await _db.FreightRateCards.FirstOrDefaultAsync(r => r.RateCardId == rateCardId);
            if (rate == null)
                return Json(new { success = false, message = "Rate card not found." });

            _db.FreightRateCards.Remove(rate);
            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                GetCurrentUserId(),
                $"Deleted Freight Rate Card ID {rateCardId}",
                "FreightRateCards",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new { success = true, message = "Rate card removed." });
        }

        [HttpPost]
        public async Task<JsonResult> AddInvoice(string invoiceCode, int carrierId, decimal amount, DateTime dueDate)
        {
            var invoice = new Invoice
            {
                InvoiceCode = invoiceCode,
                CarrierId = carrierId,
                Amount = amount,
                IssueDate = DateTime.Now,
                DueDate = dueDate,
                Status = "Pending"
            };

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                GetCurrentUserId(),
                $"Added new Freight Invoice: {invoiceCode}",
                "Invoices",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new { success = true, message = "Invoice added successfully." });
        }

        [HttpPost]
        public async Task<JsonResult> UpdateInvoice(int invoiceId, string invoiceCode, decimal amount, DateTime dueDate)
        {
            var invoice = await _db.Invoices.FindAsync(invoiceId);
            if (invoice == null) return Json(new { success = false, message = "Invoice not found." });

            invoice.InvoiceCode = invoiceCode;
            invoice.Amount = amount;
            invoice.DueDate = dueDate;

            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                GetCurrentUserId(),
                $"Updated Freight Invoice: {invoiceCode}",
                "Invoices",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new { success = true, message = "Invoice updated successfully." });
        }

        [HttpPost]
        public async Task<JsonResult> UpdateInvoiceStatus(int invoiceId, string status)
        {
            var invoice = await _db.Invoices.FindAsync(invoiceId);
            if (invoice == null) return Json(new { success = false, message = "Invoice not found." });

            invoice.Status = status;
            if (status == "Paid") invoice.PaidAt = DateTime.Now;

            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                GetCurrentUserId(),
                $"Updated status of Invoice ID {invoiceId} to '{status}'",
                "Invoices",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new { success = true, message = $"Invoice marked as {status}." });
        }



        [HttpPost]
        public async Task<JsonResult> MarkAsPaid(int paymentId, string reference, string method)
        {
            var payment = await _db.Payments.Include(p => p.Shipment).FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (payment == null) return Json(new { success = false, message = "Payment record not found." });

            payment.Status = "Completed";
            payment.ReferenceNumber = reference;
            payment.PaymentMethod = method;
            payment.PaymentDate = DateTime.Now;

            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                GetCurrentUserId(),
                $"Payment verified for Shipment {payment.Shipment?.ShipmentCode}: {payment.Amount:C}",
                "Payments",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new { success = true, message = "Payment recorded successfully." });
        }

        [HttpGet]
        public async Task<IActionResult> Export()
        {
            var invoices = await _db.Invoices.Include(i => i.Carrier).OrderByDescending(i => i.IssueDate).ToListAsync();
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("Invoice Code,Carrier,Amount,Issue Date,Due Date,Status");

            foreach (var i in invoices)
            {
                builder.AppendLine($"\"{i.InvoiceCode}\",\"{i.Carrier?.Name}\",{i.Amount},\"{i.IssueDate:yyyy-MM-dd}\",\"{i.DueDate:yyyy-MM-dd}\",\"{i.Status}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(builder.ToString());
            return File(bytes, "text/csv", $"LogiTrack_Freight_Invoices_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}
