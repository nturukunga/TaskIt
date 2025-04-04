using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskIt.Data;
using TaskIt.Models;
using TaskIt.Services;

namespace TaskIt.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationsController> _logger;
        private readonly INotificationService _notificationService;

        public NotificationsController(
            ApplicationDbContext context,
            ILogger<NotificationsController> logger,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        // GET: Notifications
        public async Task<IActionResult> Index()
        {
            var userId = User.Identity?.Name;
            
            try
            {
                var notifications = await _context.Notifications
                    .Include(n => n.Task)
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                return View(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                return View("Error", new ErrorViewModel { ErrorMessage = "An error occurred while retrieving notifications." });
            }
        }

        // POST: Notifications/MarkAsRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.Identity?.Name;
            
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                {
                    return NotFound();
                }

                notification.IsRead = true;
                _context.Update(notification);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Notifications/MarkAllAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.Identity?.Name;
            
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                _context.UpdateRange(notifications);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Notifications/GetUnreadCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.Identity?.Name;
            
            try
            {
                var count = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notification count");
                return Json(new { count = 0 });
            }
        }

        // GET: Notifications/GetRecentNotifications
        [HttpGet]
        public async Task<IActionResult> GetRecentNotifications()
        {
            var userId = User.Identity?.Name;
            
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .Select(n => new
                    {
                        id = n.Id,
                        message = n.Message,
                        createdAt = n.CreatedAt,
                        isRead = n.IsRead,
                        actionUrl = n.ActionUrl
                    })
                    .ToListAsync();

                return Json(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent notifications");
                return Json(new object[0]);
            }
        }
    }
}
