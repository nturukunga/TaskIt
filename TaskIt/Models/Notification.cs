using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskIt.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Message { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public int? TaskId { get; set; }

        [ForeignKey("TaskId")]
        public virtual TaskItem? Task { get; set; }

        [Required]
        public NotificationType Type { get; set; } = NotificationType.TaskAssigned;

        // URL to navigate to when notification is clicked
        [StringLength(500)]
        public string? ActionUrl { get; set; }
    }

    public enum NotificationType
    {
        TaskAssigned,
        TaskUpdated,
        TaskDueSoon,
        TaskOverdue,
        TaskCompleted,
        UserMention,
        SystemAlert
    }
}
