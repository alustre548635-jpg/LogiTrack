namespace LogiTrack.Models
{
    public class AuditLogEventDto
    {
        public int LogId { get; set; }
        public int UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? TableAffected { get; set; }
        public string? IPAddress { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserFullName { get; set; } = "Unknown user";
    }
}
