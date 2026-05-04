using LogiTrack.Data;
using LogiTrack.Models;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace LogiTrack.Controllers
{
    public class AccountController : Controller
    {
        private readonly LogiTrackDbContext _db;

        public AccountController(LogiTrackDbContext db)
        {
            _db = db;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register(string? plan = null, string? returnUrl = null)
        {
            if (HttpContext.Session.GetString("UserId") != null)
                return RedirectLoggedInUser();

            ViewBag.SelectedPlan = NormalizePlan(plan);
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string FullName, string Email, string Password, string ConfirmPassword, bool AgreeTerms, string? SelectedPlan = null, string? ReturnUrl = null)
        {
            try
            {
                var selectedPlan = NormalizePlan(SelectedPlan);
                ViewBag.SelectedPlan = selectedPlan;
                ViewBag.ReturnUrl = ReturnUrl;

                // Validation
                if (string.IsNullOrEmpty(FullName) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(ConfirmPassword))
                {
                    ViewBag.Error = "All fields are required.";
                    return View();
                }

                if (Password != ConfirmPassword)
                {
                    ViewBag.Error = "Passwords do not match.";
                    return View();
                }

                if (Password.Length < 8)
                {
                    ViewBag.Error = "Password must be at least 8 characters long.";
                    return View();
                }

                if (!HasPasswordComplexity(Password))
                {
                    ViewBag.Error = "Password must contain uppercase, lowercase, and numbers.";
                    return View();
                }

                if (!AgreeTerms)
                {
                    ViewBag.Error = "You must agree to Terms and Conditions.";
                    return View();
                }

                var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == Email);
                if (existingUser != null)
                {
                    ViewBag.Error = "Email already registered.";
                    return View();
                }

                var newUser = new User
                {
                    FullName = FullName,
                    Email = Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                    Role = "Staff",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _db.Users.Add(newUser);
                await _db.SaveChangesAsync();

                HttpContext.Session.SetString("UserId", newUser.UserId.ToString());
                HttpContext.Session.SetString("FullName", newUser.FullName);
                HttpContext.Session.SetString("Email", newUser.Email);
                HttpContext.Session.SetString("Role", newUser.Role);

                if (!string.IsNullOrWhiteSpace(selectedPlan))
                    return RedirectToAction("Checkout", "Payment", new { plan = selectedPlan, returnUrl = ReturnUrl });

                ViewBag.Success = "Account created! You can now sign in.";
                return View();
            }
            catch (SqlException)
            {
                ViewBag.Error = "Registration is temporarily unavailable due to a database connection issue. Please try again in a moment.";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Registration error: " + ex.Message;
                return View();
            }
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserId") != null)
                return RedirectLoggedInUser();

            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Email, string Password)
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

                HttpContext.Session.SetString("UserId", user.UserId.ToString());
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("Role", user.Role);

                user.LastLogin = DateTime.Now;
                await _db.SaveChangesAsync();

                // Role-based redirect
                if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Index", "Admin");

                if (string.Equals(user.Role, "Manager", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Index", "Manager");

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
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Helper method
        private bool HasPasswordComplexity(string password)
        {
            return password.Any(char.IsUpper) && 
                   password.Any(char.IsLower) && 
                   password.Any(char.IsDigit);
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

            return RedirectToAction("Index", "Client");
        }

    }
}