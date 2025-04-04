using System;
using System.ComponentModel.DataAnnotations;
using TaskIt.Models;

namespace TaskIt.DTOs
{
    /// <summary>
    /// Data Transfer Object for Task items
    /// </summary>
    public class TaskDTO
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Required]
        public TaskItemStatus Status { get; set; } = TaskItemStatus.ToDo;

        [Required]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? DueDate { get; set; }

        [Required]
        public string CreatedById { get; set; } = string.Empty;

        public string? AssignedToId { get; set; }

        public string? CreatedByName { get; set; }

        public string? AssignedToName { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public int? EstimatedHours { get; set; }

        public int? ActualHours { get; set; }

        // Calculated properties
        public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.Today && Status != TaskItemStatus.Completed;
        
        public bool IsDueSoon => DueDate.HasValue && 
                                !IsOverdue && 
                                DueDate.Value.Date <= DateTime.Today.AddDays(2) && 
                                Status != TaskItemStatus.Completed;
    }

    /// <summary>
    /// DTO for creating a new task
    /// </summary>
    public class CreateTaskDTO
    {
        [Required]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Required]
        public TaskItemStatus Status { get; set; } = TaskItemStatus.ToDo;

        [Required]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime? DueDate { get; set; }

        public string? AssignedToId { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public int? EstimatedHours { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing task
    /// </summary>
    public class UpdateTaskDTO
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Required]
        public TaskItemStatus Status { get; set; }

        [Required]
        public TaskPriority Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public string? AssignedToId { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public int? EstimatedHours { get; set; }

        public int? ActualHours { get; set; }
    }

    /// <summary>
    /// DTO for task statistics
    /// </summary>
    public class TaskStatisticsDTO
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int HighPriorityTasks { get; set; }
    }
}
