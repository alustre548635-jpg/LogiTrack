using LogiTrack.Data;
using LogiTrack.Models;
using LogiTrack.Services;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace LogiTrack.Controllers
{
    public class AccountController : Controller
    {
        private readonly LogiTrackDbContext _db;
        private readonly IAuditLogService _auditLogService;
        private readonly IEmailService _emailService;

        public AccountController(LogiTrackDbContext db, IAuditLogService auditLogService, IEmailService emailService)
        {
            _db = db;
            _auditLogService = auditLogService;
            _emailService = emailService;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string FullName, string Email, string Password, string ConfirmPassword, bool AgreeTerms)
        {
            return RedirectToAction("Login");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserId") != null)
                return RedirectLoggedInUser();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Email, string Password, bool RememberMe)
        {
            try
            {
                if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
                {
                    ViewBag.Error = "Email and password are required.";
                    return View();
                }

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == Email && u.IsActive == true);

                if (user == null)
                {
                    ViewBag.Error = "Invalid email or password.";
                    return View();
                }

                // Check if password is a BCrypt hash (starts with "$2")
                // or a legacy plain-text password that needs migration
                bool passwordValid = false;

                if (user.PasswordHash.StartsWith("$2"))
                {
                    // BCrypt hashed password — verify normally
                    passwordValid = BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash);
                }
                else
                {
                    // Legacy plain-text password — compare directly
                    passwordValid = (user.PasswordHash == Password);

                    // Auto-migrate to BCrypt hash if password matches
                    if (passwordValid)
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);
                    }
                }

                if (!passwordValid)
                {
                    ViewBag.Error = "Invalid email or password.";
                    return View();
                }

                // Log in directly — set session and cookie auth
                user.LastLogin = DateTime.Now;
                await _db.SaveChangesAsync();

                HttpContext.Session.SetString("UserId", user.UserId.ToString());
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("Role", user.Role);

                // Claims-based Cookie Auth for "Keep me signed in"
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, "LogiTrackCookies");

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = RememberMe,
                    ExpiresUtc = RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
                };

                await HttpContext.SignInAsync("LogiTrackCookies", new ClaimsPrincipal(claimsIdentity), authProperties);

                await _auditLogService.LogAsync(
                    user.UserId,
                    $"User login: {user.Email}",
                    "Users",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                // Role-based redirect
                if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Index", "Admin");

                if (string.Equals(user.Role, "Manager", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Index", "Manager");

                if (string.Equals(user.Role, "Staff", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(user.Role, "Warehouse Staff", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Index", "Warehouse");

                if (string.Equals(user.Role, "Driver", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Index", "Driver");

                return RedirectToAction("Index", "Client");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Login error: " + ex.Message;
                return View();
            }
        }

        // GET: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var userIdText = HttpContext.Session.GetString("UserId");
            var email = HttpContext.Session.GetString("Email");
            if (int.TryParse(userIdText, out var userId))
            {
                await _auditLogService.LogAsync(
                    userId,
                    $"User logout: {email ?? "Unknown"}",
                    "Users",
                    HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync("LogiTrackCookies");
            return RedirectToAction("Login");
        }
        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string Email, string NewPassword, string ConfirmPassword)
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword))
            {
                ViewBag.Error = "All fields are required.";
                return View();
            }

            if (NewPassword != ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            if (NewPassword.Length < 8)
            {
                ViewBag.Error = "Password must be at least 8 characters long.";
                return View();
            }

            if (!HasPasswordComplexity(NewPassword))
            {
                ViewBag.Error = "Password must contain uppercase, lowercase, numbers, and symbols.";
                return View();
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (user == null)
            {
                ViewBag.Error = "Email address not found.";
                return View();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Password has been successfully reset! You can now sign in.";
            return RedirectToAction("Login");
        }

        // Helper method
        private bool HasPasswordComplexity(string password)
        {
            return password.Any(char.IsUpper) && 
                   password.Any(char.IsLower) && 
                   password.Any(char.IsDigit) &&
                   password.Any(c => !char.IsLetterOrDigit(c));
        }

        private static string? NormalizePlan(string? plan)
        {
            if (string.IsNullOrWhiteSpace(plan))
                return null;

            if (string.Equals(plan, "starter", StringComparison.OrdinalIgnoreCase))
                return "Starter";

            if (string.Equals(plan, "professional", StringComparison.OrdinalIgnoreCase))
                return "Professional";

            if (string.Equals(plan, "enterprise", StringComparison.OrdinalIgnoreCase))
                return "Enterprise";

            return null;
        }

        private IActionResult RedirectLoggedInUser()
        {
            var role = HttpContext.Session.GetString("Role");
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Admin");

            if (string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Manager");

            if (string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(role, "Warehouse Staff", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Warehouse");

            if (string.Equals(role, "Driver", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Driver");

            return RedirectToAction("Index", "Client");
        }

    }
}
