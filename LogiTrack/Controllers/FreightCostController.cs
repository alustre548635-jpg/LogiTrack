using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogiTrack.Data;

namespace LogiTrack.Controllers
{
    public class FreightCostController : Controller
    {
        private readonly LogiTrackDbContext _db;

        public FreightCostController(LogiTrackDbContext db)
        {
            _db = db;
        }

        public IActionResult Index() => View();
        public JsonResult Summary() { /* TODO */ return Json(new { }); }
        public JsonResult RateCards() { /* TODO */ return Json(new { }); }
        public JsonResult Anomalies() { /* TODO */ return Json(new { }); }
        public JsonResult Invoices() { /* TODO */ return Json(new { }); }
        [HttpPut] public JsonResult UpdateRate(RateModel model) { return Json(new { success = true }); }

        [HttpPost]
        public async Task<JsonResult> DeactivateRate(int rateCardId)
        {
            var rate = await _db.FreightRateCards.FirstOrDefaultAsync(r => r.RateCardId == rateCardId);
            if (rate == null)
                return Json(new { success = false, message = "Rate card not found." });

            _db.FreightRateCards.Remove(rate);
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Rate card removed." });
        }
    }

    public class RateModel
    {
    }
}
