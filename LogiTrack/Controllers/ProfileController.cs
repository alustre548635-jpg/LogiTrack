using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogiTrack.Data;
using LogiTrack.Models;
using LogiTrack.Models.ViewModels;
using BCrypt.Net;

namespace LogiTrack.Controllers
{
    public class ProfileController : Controller
    {
        private readonly LogiTrackDbContext _context;

        public ProfileController(LogiTrackDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return RedirectToAction("Login", "Account");

            var staff = await _context.Staff
                .Include(s => s.Warehouse)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            var driver = await _context.Drivers
                .Include(d => d.Warehouse)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            var viewModel = new ProfileIndexViewModel
            {
                User = user,
                Staff = staff,
                Driver = driver
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIdentity(string fullName, string email, string phoneNumber)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Json(new { success = false, message = "Session expired." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return Json(new { success = false, message = "User not found." });

            // Basic validation
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, message = "Name and Email are required." });

            user.FullName = fullName;
            user.Email = email;
            user.PhoneNumber = phoneNumber;

            await _context.SaveChangesAsync();

            // Update session if name changed
            HttpContext.Session.SetString("FullName", fullName);
            HttpContext.Session.SetString("Email", email);

            return Json(new { success = true, message = "Profile updated successfully!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Json(new { success = false, message = "Session expired." });

            if (newPassword != confirmPassword)
                return Json(new { success = false, message = "Passwords do not match." });

            if (newPassword.Length < 8)
                return Json(new { success = false, message = "Password must be at least 8 characters." });

            if (!HasPasswordComplexity(newPassword))
                return Json(new { success = false, message = "Password must contain uppercase, lowercase, numbers, and symbols." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return Json(new { success = false, message = "User not found." });

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return Json(new { success = false, message = "Current password is incorrect." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Password changed successfully!" });
        }

        [HttpPost]
        public IActionResult UpdatePreferences(bool emailNotifications, bool smsNotifications, string language)
        {
            // Mock persistence for demo
            return Json(new { success = true, message = "Preferences saved!" });
        }
        private bool HasPasswordComplexity(string password)
        {
            return password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(c => !char.IsLetterOrDigit(c));
        }
    }
}
