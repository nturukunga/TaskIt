using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskIt.Models
{
    // Renamed to TaskItem to avoid conflict with System.Threading.Tasks.Task
    [Table("Tasks")]
    public class TaskItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public TaskItemStatus Status { get; set; } = TaskItemStatus.ToDo;

        [Required]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? DueDate { get; set; }

        [Required]
        public string CreatedById { get; set; } = string.Empty;

        public string? AssignedToId { get; set; }

        [ForeignKey("CreatedById")]
        public virtual ApplicationUser? CreatedBy { get; set; }

        [ForeignKey("AssignedToId")]
        public virtual ApplicationUser? AssignedTo { get; set; }

        public virtual ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>();

        [StringLength(100)]
        public string? Category { get; set; }

        public int? EstimatedHours { get; set; }

        public int? ActualHours { get; set; }

        public bool IsDeleted { get; set; } = false;
    }

    public enum TaskItemStatus
    {
        ToDo,
        InProgress,
        OnHold,
        Completed,
        Cancelled
    }

    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
