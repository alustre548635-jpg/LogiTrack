namespace LogiTrack.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(int? userId, string action, string? tableAffected, string? ipAddress);
    }
}
