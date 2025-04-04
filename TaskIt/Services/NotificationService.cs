using Microsoft.EntityFrameworkCore;
using TaskIt.Data;
using TaskIt.Models;

namespace TaskIt.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CreateTaskAssignedNotification(TaskItem task)
        {
            if (string.IsNullOrEmpty(task.AssignedToId))
            {
                return;
            }

            try
            {
                var notification = new Notification
                {
                    UserId = task.AssignedToId,
                    TaskId = task.Id,
                    Type = NotificationType.TaskAssigned,
                    Message = $"You have been assigned to task: {task.Title}",
                    ActionUrl = $"/Tasks/Details/{task.Id}",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task assigned notification");
            }
        }

        public async Task CreateTaskStatusChangedNotification(TaskItem task, TaskItemStatus oldStatus)
        {
            if (string.IsNullOrEmpty(task.AssignedToId) && string.IsNullOrEmpty(task.CreatedById))
            {
                return;
            }

            try
            {
                var notificationList = new List<Notification>();

                // Notify task creator if different from assignee
                if (!string.IsNullOrEmpty(task.CreatedById) && task.CreatedById != task.AssignedToId)
                {
                    notificationList.Add(new Notification
                    {
                        UserId = task.CreatedById,
                        TaskId = task.Id,
                        Type = NotificationType.TaskUpdated,
                        Message = $"Task status changed from {oldStatus} to {task.Status}: {task.Title}",
                        ActionUrl = $"/Tasks/Details/{task.Id}",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    });
                }

                // Notify assignee
                if (!string.IsNullOrEmpty(task.AssignedToId))
                {
                    notificationList.Add(new Notification
                    {
                        UserId = task.AssignedToId,
                        TaskId = task.Id,
                        Type = NotificationType.TaskUpdated,
                        Message = $"Task status changed from {oldStatus} to {task.Status}: {task.Title}",
                        ActionUrl = $"/Tasks/Details/{task.Id}",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    });
                }

                _context.Notifications.AddRange(notificationList);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task status changed notification");
            }
        }

        public async Task CreateTaskDueSoonNotification(TaskItem task)
        {
            if (string.IsNullOrEmpty(task.AssignedToId) || !task.DueDate.HasValue)
            {
                return;
            }

            try
            {
                var notification = new Notification
                {
                    UserId = task.AssignedToId,
                    TaskId = task.Id,
                    Type = NotificationType.TaskDueSoon,
                    Message = $"Task due soon: {task.Title} is due on {task.DueDate:MM/dd/yyyy}",
                    ActionUrl = $"/Tasks/Details/{task.Id}",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task due soon notification");
            }
        }

        public async Task CreateTaskOverdueNotification(TaskItem task)
        {
            if (string.IsNullOrEmpty(task.AssignedToId) || !task.DueDate.HasValue)
            {
                return;
            }

            try
            {
                var notification = new Notification
                {
                    UserId = task.AssignedToId,
                    TaskId = task.Id,
                    Type = NotificationType.TaskOverdue,
                    Message = $"Task overdue: {task.Title} was due on {task.DueDate:MM/dd/yyyy}",
                    ActionUrl = $"/Tasks/Details/{task.Id}",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task overdue notification");
            }
        }

        public async Task CreateSystemAlertNotification(string userId, string message, string? actionUrl = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Type = NotificationType.SystemAlert,
                    Message = message,
                    ActionUrl = actionUrl,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system alert notification");
            }
        }
    }
}
