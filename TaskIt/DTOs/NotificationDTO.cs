using System;
using System.ComponentModel.DataAnnotations;
using TaskIt.Models;

namespace TaskIt.DTOs
{
    /// <summary>
    /// Data Transfer Object for Notifications
    /// </summary>
    public class NotificationDTO
    {
        public int Id { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public bool IsRead { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int? TaskId { get; set; }

        public string? TaskTitle { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        public string? ActionUrl { get; set; }

        // Calculated formatted timestamp for display
        public string FormattedTimestamp => GetFormattedTimestamp();

        private string GetFormattedTimestamp()
        {
            var now = DateTime.UtcNow;
            var diff = now - CreatedAt;

            if (diff.TotalMinutes < 1)
                return "Just now";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} minute{(diff.TotalMinutes == 1 ? "" : "s")} ago";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} hour{(diff.TotalHours == 1 ? "" : "s")} ago";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} day{(diff.TotalDays == 1 ? "" : "s")} ago";
            
            return CreatedAt.ToString("MM/dd/yyyy HH:mm");
        }
    }

    /// <summary>
    /// DTO for creating a new notification
    /// </summary>
    public class CreateNotificationDTO
    {
        [Required]
        [StringLength(200, ErrorMessage = "Message cannot exceed 200 characters.")]
        public string Message { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int? TaskId { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        [StringLength(500)]
        public string? ActionUrl { get; set; }
    }

    /// <summary>
    /// DTO for notification badge info
    /// </summary>
    public class NotificationBadgeDTO
    {
        public int UnreadCount { get; set; }
        public List<NotificationDTO> RecentNotifications { get; set; } = new List<NotificationDTO>();
    }
}
