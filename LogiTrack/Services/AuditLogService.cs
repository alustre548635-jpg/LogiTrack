using LogiTrack.Data;
using LogiTrack.Hubs;
using LogiTrack.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly LogiTrackDbContext _db;
        private readonly IHubContext<AuditLogHub> _hubContext;

        public AuditLogService(LogiTrackDbContext db, IHubContext<AuditLogHub> hubContext)
        {
            _db = db;
            _hubContext = hubContext;
        }

        public async Task LogAsync(int? userId, string action, string? tableAffected, string? ipAddress)
        {
            if (userId == null || userId <= 0 || string.IsNullOrWhiteSpace(action))
                return;

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null)
                return;

            var entry = new ActivityLog
            {
                UserId = user.UserId,
                Action = action.Trim(),
                TableAffected = tableAffected,
                IPAddress = ipAddress,
                Timestamp = DateTime.Now
            };

            _db.ActivityLogs.Add(entry);
            await _db.SaveChangesAsync();

            var payload = new AuditLogEventDto
            {
                LogId = entry.LogId,
                UserId = entry.UserId,
                Action = entry.Action,
                TableAffected = entry.TableAffected,
                IPAddress = entry.IPAddress,
                Timestamp = entry.Timestamp,
                UserFullName = user.FullName
            };

            await _hubContext.Clients.All.SendAsync("auditLogCreated", payload);
        }
    }
}
