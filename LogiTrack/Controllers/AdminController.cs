using LogiTrack.Data;
using LogiTrack.Models;
using LogiTrack.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Controllers
{
    public class AdminController : Controller
    {
        private readonly LogiTrackDbContext _db;
        private readonly IAuditLogService _auditLogService;
        private static readonly HashSet<string> AssignableRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Manager", "Staff", "Driver"
        };

        private static List<string> ParseAssignedModules(string? actionText)
        {
            if (string.IsNullOrWhiteSpace(actionText))
                return new List<string>();

            const string marker = "Assigned modules:";
            var idx = actionText.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            var modulesPart = idx >= 0 ? actionText[(idx + marker.Length)..] : actionText;

            return modulesPart
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(m => !string.Equals(m, "No modules selected", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public AdminController(LogiTrackDbContext db, IAuditLogService auditLogService)
        {
            _db = db;
            _auditLogService = auditLogService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrWhiteSpace(userId))
                return RedirectToAction("Login", "Account");

            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Home");

            var moduleLogs = await _db.ActivityLogs
                .Where(a => a.TableAffected == "UserModules")
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            var userAssignedModules = moduleLogs
                .GroupBy(a => a.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => ParseAssignedModules(g.First().Action)
                );

            var warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync();

            var staffLinks = await _db.Staff
                .Where(s => s.UserId != null)
                .ToDictionaryAsync(s => s.UserId!.Value, s => s.WarehouseId);

            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _db.Users.CountAsync(),
                ActiveUsers = await _db.Users.CountAsync(u => u.IsActive),
                AdminUsers = await _db.Users.CountAsync(u => u.Role == "Admin" && u.IsActive),
                ManagerUsers = await _db.Users.CountAsync(u => u.Role == "Manager" && u.IsActive),
                StaffUsers = await _db.Users.CountAsync(u => u.Role == "Staff" && u.IsActive),
                DriverUsers = await _db.Users.CountAsync(u => u.Role == "Driver" && u.IsActive),
                FinanceUsers = await _db.Users.CountAsync(u => u.Role == "Finance" && u.IsActive),
                TotalShipments = await _db.Shipments.CountAsync(),
                InTransitShipments = await _db.Shipments.CountAsync(s => s.Status == "In Transit"),
                PendingShipments = await _db.Shipments.CountAsync(s => s.Status == "Pending"),
                TotalCarriers = await _db.Carriers.CountAsync(),
                TotalWarehouses = await _db.Warehouses.CountAsync(),
                TotalRoutes = await _db.Routes.CountAsync(),
                TotalInvoices = await _db.Invoices.CountAsync(),
                TotalTrackingEvents = await _db.TrackingEvents.CountAsync(),
                RecentActivities = await _db.ActivityLogs
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(8)
                    .ToListAsync(),
                Users = await _db.Users
                    .OrderByDescending(u => u.IsActive)
                    .ThenBy(u => u.FullName)
                    .ToListAsync(),
                UserAssignedModules = userAssignedModules,
                Warehouses = warehouses,
                UserWarehouseMap = staffLinks
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string FullName, string Email, string Role, int? WarehouseId)
        {
            if (string.IsNullOrWhiteSpace(FullName) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Role))
            {
                TempData["AdminError"] = "All fields are required to create a user.";
                TempData["ActiveModule"] = "manage-users";
                return RedirectToAction(nameof(Index));
            }

            if (!AssignableRoles.Contains(Role))
            {
                TempData["AdminError"] = "Invalid role selected. Allowed roles: Manager, Staff, Driver.";
                TempData["ActiveModule"] = "manage-users";
                return RedirectToAction(nameof(Index));
            }

            if (string.Equals(Role, "Staff", StringComparison.OrdinalIgnoreCase) && (WarehouseId == null || WarehouseId <= 0))
            {
                TempData["AdminError"] = "A warehouse must be assigned to staff members.";
                TempData["ActiveModule"] = "manage-users";
                return RedirectToAction(nameof(Index));
            }



            var email = Email.Trim();
            var exists = await _db.Users.AnyAsync(u => u.Email == email);
            if (exists)
            {
                TempData["AdminError"] = "Email is already in use.";
                TempData["ActiveModule"] = "manage-users";
                return RedirectToAction(nameof(Index));
            }

            var newUser = new User
            {
                FullName = FullName.Trim(),
                Email = email,
                Role = Role.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("LogiTrack@2026!"), // Default password for new users
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            // Auto-create Staff record linked to this user
            if (string.Equals(Role, "Staff", StringComparison.OrdinalIgnoreCase) && WarehouseId.HasValue)
            {
                _db.Staff.Add(new Staff
                {
                    FullName = FullName.Trim(),
                    UserId = newUser.UserId,
                    WarehouseId = WarehouseId.Value,
                    IsOnShift = false
                });
                await _db.SaveChangesAsync();

                // Auto-assign core modules for Staff
                await _auditLogService.LogAsync(
                    newUser.UserId,
                    "Assigned modules: Dashboard, Shipments, Warehouses, Tracking",
                    "UserModules",
                    HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            // Auto-create Driver record linked to this user
            if (string.Equals(Role, "Driver", StringComparison.OrdinalIgnoreCase))
            {
                _db.Drivers.Add(new Driver
                {
                    FullName = FullName.Trim(),
                    UserId = newUser.UserId,
                    LicenseNumber = "N01-23-" + (100000 + newUser.UserId),
                    VehiclePlate = "NCR-" + (1000 + newUser.UserId),
                    VehicleType = "Truck",
                    Status = "Available",
                    OnTimeDeliveryRate = 100,
                    SafetyScore = 100
                });
                await _db.SaveChangesAsync();
            }

            TempData["AdminSuccess"] = "User created successfully.";
            TempData["ActiveModule"] = "manage-users";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(int UserId, string FullName, string Email, string Role, string? NewPassword, int? WarehouseId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == UserId);
            if (user == null)
            {
                TempData["AdminError"] = "User not found.";
                TempData["ActiveModule"] = "manage-users";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Role))
            {
                TempData["AdminError"] = "Full name, email, and role are required.";
                TempData["ActiveModule"] = "manage-users";
                return RedirectToAction(nameof(Index));
            }

            if (!AssignableRoles.Contains(Role))
            {
                TempData["AdminError"] = "Invalid role selected. Allowed roles: Manager, Staff, Driver.";
                TempData["ActiveModule"] = "manage-users";
                return RedirectToAction(nameof(Index));
            }

            if (string.Equals(Role, "Staff", StringComparison.OrdinalIgnoreCase) && (WarehouseId == null || WarehouseId <= 0))
            {
                TempData["AdminError"] = "A warehouse must be assigned to staff members.";
                TempData["ActiveModule"] = "manage-users";
                return RedirectToAction(nameof(Index));
            }

            var normalizedEmail = Email.Trim();
            var duplicateEmail = await _db.Users.AnyAsync(u => u.Email == normalizedEmail && u.UserId != UserId);
            if (duplicateEmail)
            {
                TempData["AdminError"] = "Another user already uses this email.";
                TempData["ActiveModule"] = "manage-users";
                return RedirectToAction(nameof(Index));
            }

            user.FullName = FullName.Trim();
            user.Email = normalizedEmail;
            user.Role = Role.Trim();

            if (!string.IsNullOrWhiteSpace(NewPassword))
            {
                if (NewPassword.Length < 8)
                {
                    TempData["AdminError"] = "New password must be at least 8 characters.";
                    TempData["ActiveModule"] = "manage-users";
                    return RedirectToAction(nameof(Index));
                }
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            }

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            // Sync Staff record
            var existingStaff = await _db.Staff.FirstOrDefaultAsync(s => s.UserId == UserId);

            if (string.Equals(Role, "Staff", StringComparison.OrdinalIgnoreCase) && WarehouseId.HasValue)
            {
                if (existingStaff != null)
                {
                    // Update existing staff record
                    existingStaff.FullName = FullName.Trim();
                    existingStaff.WarehouseId = WarehouseId.Value;
                }
                else
                {
                    // Create new staff record
                    _db.Staff.Add(new Staff
                    {
                        FullName = FullName.Trim(),
                        UserId = UserId,
                        WarehouseId = WarehouseId.Value,
                        IsOnShift = false
                    });
                }
                await _db.SaveChangesAsync();
            }
            else if (existingStaff != null)
            {
                // Role changed away from Staff — unlink
                existingStaff.UserId = null;
                await _db.SaveChangesAsync();
            }

            // Sync Driver record
            var existingDriver = await _db.Drivers.FirstOrDefaultAsync(d => d.UserId == UserId);
            if (string.Equals(Role, "Driver", StringComparison.OrdinalIgnoreCase))
            {
                if (existingDriver != null)
                {
                    existingDriver.FullName = FullName.Trim();
                }
                else
                {
                    _db.Drivers.Add(new Driver
                    {
                        FullName = FullName.Trim(),
                        UserId = UserId,
                        LicenseNumber = "N01-23-" + (100000 + UserId),
                        VehiclePlate = "NCR-" + (1000 + UserId),
                        VehicleType = "Truck",
                        Status = "Available",
                        OnTimeDeliveryRate = 100,
                        SafetyScore = 100
                    });
                }
                await _db.SaveChangesAsync();
            }
            else if (existingDriver != null)
            {
                existingDriver.UserId = null;
                await _db.SaveChangesAsync();
            }

            TempData["AdminSuccess"] = "User updated successfully.";
            TempData["ActiveModule"] = "manage-users";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int UserId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == UserId);
            if (user == null)
            {
                TempData["AdminError"] = "User not found.";
                TempData["ActiveModule"] = "manage-users";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            TempData["AdminSuccess"] = user.IsActive ? "User successfully reactivated." : "User successfully deactivated.";
            TempData["ActiveModule"] = "manage-users";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignModules(int UserId, List<string> SelectedModules)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == UserId);
            if (user == null)
            {
                TempData["AdminError"] = "User not found for module assignment.";
                TempData["ActiveModule"] = "settings";
                return RedirectToAction(nameof(Index));
            }

            if (string.Equals(user.Role, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                SelectedModules = new List<string> { "Dashboard", "Shipments", "Warehouses", "Tracking" };
            }

            var modules = SelectedModules?.Count > 0 ? string.Join(", ", SelectedModules) : "No modules selected";
            await _auditLogService.LogAsync(
                user.UserId,
                $"Assigned modules: {modules}",
                "UserModules",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["AdminSuccess"] = $"Modules assigned to {user.FullName}.";
            TempData["ActiveModule"] = "settings";
            return RedirectToAction(nameof(Index));
        }
    }
}
