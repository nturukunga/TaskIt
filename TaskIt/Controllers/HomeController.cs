using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskIt.Data;
using TaskIt.Models;
using System.Security.Claims;

namespace TaskIt.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Dashboard));
            }
            
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            try
            {
                // DEBUG: Log all tasks directly from database to see what's happening
                var allTasks = await _context.Tasks.IgnoreQueryFilters().ToListAsync();
                _logger.LogInformation($"User ID: {userId}");
                _logger.LogInformation($"Total tasks in database: {allTasks.Count}");
                foreach (var task in allTasks)
                {
                    _logger.LogInformation($"Task ID: {task.Id}, Title: {task.Title}, CreatedById: {task.CreatedById}, AssignedToId: {task.AssignedToId}, IsDeleted: {task.IsDeleted}");
                }
                
                // Get task statistics for the authenticated user
                var taskStatistics = new
                {
                    TotalTasks = await _context.Tasks.IgnoreQueryFilters().CountAsync(t => t.AssignedToId == userId || t.CreatedById == userId),
                    CompletedTasks = await _context.Tasks.IgnoreQueryFilters().CountAsync(t => (t.AssignedToId == userId || t.CreatedById == userId) && t.Status == TaskItemStatus.Completed),
                    PendingTasks = await _context.Tasks.IgnoreQueryFilters().CountAsync(t => (t.AssignedToId == userId || t.CreatedById == userId) && t.Status == TaskItemStatus.ToDo),
                    InProgressTasks = await _context.Tasks.IgnoreQueryFilters().CountAsync(t => (t.AssignedToId == userId || t.CreatedById == userId) && t.Status == TaskItemStatus.InProgress),
                    OverdueTasks = await _context.Tasks.IgnoreQueryFilters().CountAsync(t => (t.AssignedToId == userId || t.CreatedById == userId) && t.DueDate < DateTime.Today && t.Status != TaskItemStatus.Completed),
                    HighPriorityTasks = await _context.Tasks.IgnoreQueryFilters().CountAsync(t => (t.AssignedToId == userId || t.CreatedById == userId) && (t.Priority == TaskPriority.High || t.Priority == TaskPriority.Critical))
                };

                // Get the most recent tasks assigned to or created by the user
                var recentTasks = await _context.Tasks.IgnoreQueryFilters()
                    .Where(t => t.AssignedToId == userId || t.CreatedById == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Get the most recent notifications for the user
                var recentNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                ViewBag.TaskStatistics = taskStatistics;
                ViewBag.RecentTasks = recentTasks;
                ViewBag.RecentNotifications = recentNotifications;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving dashboard data");
                return View("Error", new ErrorViewModel { ErrorMessage = "An error occurred while loading dashboard data. Please try again later." });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        
        [Authorize]
        public async Task<IActionResult> DiagnoseAllTasks()
        {
            try
            {
                // Get all tasks from the database with no filters
                var allTasks = await _context.Tasks.IgnoreQueryFilters()
                    .Include(t => t.AssignedTo)
                    .Include(t => t.CreatedBy)
                    .ToListAsync();
                
                ViewBag.AllTasks = allTasks;
                ViewBag.CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks for diagnostics");
                return View("Error", new ErrorViewModel { ErrorMessage = "Error retrieving tasks for diagnostics." });
            }
        }
    }
}
