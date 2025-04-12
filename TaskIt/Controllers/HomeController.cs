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
            _logger.LogInformation("Dashboard for user ID: {UserId}", userId);
            
            try
            {
                // Make sure we have a valid user ID
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID is null or empty in Dashboard!");
                    userId = User.Identity?.Name ?? "unknown";
                }
                
                // Get all tasks directly from database with includes
                var allTasks = await _context.Tasks
                    .IgnoreQueryFilters()
                    .Include(t => t.AssignedTo)
                    .Include(t => t.CreatedBy)
                    .ToListAsync();
                
                _logger.LogInformation("Total tasks in database: {Count}", allTasks.Count);
                
                // Get recent tasks - make sure to include related data
                var recentTasks = await _context.Tasks
                    .IgnoreQueryFilters()
                    .Include(t => t.AssignedTo)
                    .Include(t => t.CreatedBy)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Ensure we have at least non-null items
                if (recentTasks == null)
                {
                    recentTasks = new List<TaskItem>();
                }

                // Prepare task statistics with null checks
                var taskStatistics = new
                {
                    TotalTasks = allTasks.Count,
                    CompletedTasks = allTasks.Count(t => t.Status == TaskItemStatus.Completed),
                    PendingTasks = allTasks.Count(t => t.Status == TaskItemStatus.ToDo),
                    InProgressTasks = allTasks.Count(t => t.Status == TaskItemStatus.InProgress),
                    OverdueTasks = allTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.Today && t.Status != TaskItemStatus.Completed),
                    HighPriorityTasks = allTasks.Count(t => t.Priority == TaskPriority.High || t.Priority == TaskPriority.Critical)
                };

                // Get recent notifications
                var recentNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                ViewBag.TaskStatistics = taskStatistics;
                ViewBag.RecentTasks = recentTasks;
                ViewBag.RecentNotifications = recentNotifications ?? new List<Notification>();

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
