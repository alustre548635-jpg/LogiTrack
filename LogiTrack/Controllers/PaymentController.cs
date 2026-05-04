using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace LogiTrack.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private static readonly Dictionary<string, (string DisplayName, int AmountInCentavos)> Plans = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Starter"] = ("Starter", 34900),
            ["Professional"] = ("Professional", 89900),
            ["Enterprise"] = ("Enterprise", 179900)
        };

        public PaymentController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Checkout(string plan, string? returnUrl = null)
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToAction("Register", "Account", new { plan, returnUrl });
            if (!TryGetPlan(plan, out var normalizedPlan, out var planDetails))
                return RedirectToAction("Index", "Home");

            ViewBag.Plan = normalizedPlan;
            ViewBag.PlanName = planDetails.DisplayName;
            ViewBag.PlanAmount = planDetails.AmountInCentavos / 100m;
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckoutSession(string plan, string? returnUrl = null)
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToAction("Register", "Account", new { plan, returnUrl });
            if (!TryGetPlan(plan, out var normalizedPlan, out var planDetails))
                return RedirectToAction("Index", "Home");

            var secretKey = _configuration["PayMongo:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                TempData["PaymentError"] = "PayMongo is not configured yet. Please add your PayMongo secret key.";
                return RedirectToAction("Checkout", new { plan = normalizedPlan, returnUrl });
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var successUrl = string.IsNullOrWhiteSpace(_configuration["PayMongo:SuccessUrl"])
                ? $"{baseUrl}/Payment/Success"
                : _configuration["PayMongo:SuccessUrl"]!;
            var cancelUrl = string.IsNullOrWhiteSpace(_configuration["PayMongo:CancelUrl"])
                ? $"{baseUrl}/Payment/Checkout"
                : _configuration["PayMongo:CancelUrl"]!;

            successUrl = AddQueryParam(successUrl, "plan", normalizedPlan);
            successUrl = AddQueryParam(successUrl, "returnUrl", returnUrl);
            cancelUrl = AddQueryParam(cancelUrl, "plan", normalizedPlan);
            cancelUrl = AddQueryParam(cancelUrl, "returnUrl", returnUrl);

            var payload = new
            {
                data = new
                {
                    attributes = new
                    {
                        billing = new { },
                        send_email_receipt = true,
                        show_description = true,
                        show_line_items = true,
                        line_items = new[]
                        {
                            new
                            {
                                currency = "PHP",
                                amount = planDetails.AmountInCentavos,
                                name = $"LogiTrack {planDetails.DisplayName} Plan",
                                quantity = 1
                            }
                        },
                        payment_method_types = new[] { "card", "gcash", "paymaya" },
                        success_url = successUrl,
                        cancel_url = cancelUrl,
                        description = $"Subscription checkout for {planDetails.DisplayName} plan."
                    }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.paymongo.com/v1/checkout_sessions")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            var response = await _httpClientFactory.CreateClient().SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                TempData["PaymentError"] = "Unable to create PayMongo checkout session. Please verify API keys and try again.";
                return RedirectToAction("Checkout", new { plan = normalizedPlan, returnUrl });
            }

            using var document = JsonDocument.Parse(responseBody);
            var checkoutUrl = document.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("checkout_url").GetString();
            if (string.IsNullOrWhiteSpace(checkoutUrl))
            {
                TempData["PaymentError"] = "PayMongo did not return a checkout URL. Please try again.";
                return RedirectToAction("Checkout", new { plan = normalizedPlan, returnUrl });
            }

            return Redirect(checkoutUrl);
        }

        [HttpGet]
        public IActionResult Success(string plan, string? returnUrl = null)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            ViewBag.Plan = plan;
            return RedirectToAction("Index", "Home");
        }

        private static bool TryGetPlan(string? plan, out string normalizedPlan, out (string DisplayName, int AmountInCentavos) planDetails)
        {
            normalizedPlan = string.Empty;
            planDetails = default;
            if (string.IsNullOrWhiteSpace(plan) || !Plans.TryGetValue(plan, out planDetails))
                return false;
            normalizedPlan = planDetails.DisplayName;
            return true;
        }

        private static string AddQueryParam(string url, string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return url;
            var separator = url.Contains('?') ? "&" : "?";
            return $"{url}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
        }
    }
}
