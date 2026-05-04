using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models
{
    [Table("ActivityLogs")]
    public class ActivityLog
    {
        [Key]
        public int LogId { get; set; }

        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Action { get; set; }

        [StringLength(100)]
        public string? TableAffected { get; set; }

        [StringLength(50)]
        public string? IPAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}