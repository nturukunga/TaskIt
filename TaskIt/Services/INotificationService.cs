using TaskIt.Models;

namespace TaskIt.Services
{
    public interface INotificationService
    {
        Task CreateTaskAssignedNotification(TaskItem task);
        Task CreateTaskStatusChangedNotification(TaskItem task, TaskItemStatus oldStatus);
        Task CreateTaskDueSoonNotification(TaskItem task);
        Task CreateTaskOverdueNotification(TaskItem task);
        Task CreateSystemAlertNotification(string userId, string message, string? actionUrl = null);
    }
}
