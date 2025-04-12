using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskIt.Data;
using TaskIt.Models;
using TaskIt.Services;

namespace TaskIt.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TasksController> _logger;
        private readonly INotificationService _notificationService;

        public TasksController(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<TasksController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: Tasks
        public async Task<IActionResult> Index(string sortOrder = null, string searchString = null, int? pageNumber = 1)
        {
            ViewData["CurrentSort"] = sortOrder ?? "";
            ViewData["TitleSortParm"] = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewData["DueDateSortParm"] = sortOrder == "duedate" ? "duedate_desc" : "duedate";
            ViewData["StatusSortParm"] = sortOrder == "status" ? "status_desc" : "status";
            ViewData["PrioritySortParm"] = sortOrder == "priority" ? "priority_desc" : "priority";
            ViewData["CurrentFilter"] = searchString ?? "";

            // First check if we have ANY tasks in the database at all
            var allTasksCount = await _context.Tasks.IgnoreQueryFilters().CountAsync();
            _logger.LogInformation("Total tasks in database: {Count}", allTasksCount);

            // Simple query to get ALL tasks without ANY filtering
            var tasks = _context.Tasks
                .IgnoreQueryFilters() // Ignore global query filters
                .Where(t => true);    // Ensure we get everything

            // Include related entities
            tasks = tasks.Include(t => t.AssignedTo);
            tasks = tasks.Include(t => t.CreatedBy);

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                tasks = tasks.Where(t => 
                    t.Title.Contains(searchString) || 
                    (t.Description != null && t.Description.Contains(searchString)) ||
                    (t.Category != null && t.Category.Contains(searchString)));
            }

            // Apply sorting
            tasks = ApplySorting(tasks, sortOrder);

            int pageSize = 10;
            var pagedTasks = await PaginatedList<TaskItem>.CreateAsync(tasks, pageNumber ?? 1, pageSize);

            return View(pagedTasks);
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.Tasks.IgnoreQueryFilters()
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            return View(task);
        }

        // GET: Tasks/Create
        public IActionResult Create()
        {
            var users = _context.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
            ViewData["AssignedToId"] = new SelectList(users, "Id", "FullName");
            ViewData["Categories"] = GetCategoriesSelectList();
            return View();
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Status,Priority,DueDate,AssignedToId,Category,EstimatedHours")] TaskItem task)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get current user ID
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId))
                    {
                        _logger.LogWarning("User ID is null or empty! Using fallback value.");
                        userId = User.Identity?.Name ?? "unknown-user";
                    }
                    
                    _logger.LogInformation("Creating task '{Title}' with user ID: {UserId}", task.Title, userId);

                    // Set task properties explicitly
                    task.CreatedById = userId;
                    task.CreatedAt = DateTime.UtcNow;
                    task.IsDeleted = false;
                    
                    // Log task details before saving
                    _logger.LogInformation("Task details - CreatedById: {CreatedById}, IsDeleted: {IsDeleted}", 
                        task.CreatedById, task.IsDeleted);

                    _context.Add(task);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Task created successfully with ID: {Id}", task.Id);

                    // Also update all existing tasks to not be deleted (EMERGENCY FIX)
                    var existingTasks = await _context.Tasks.IgnoreQueryFilters().ToListAsync();
                    foreach (var existingTask in existingTasks)
                    {
                        if (existingTask.IsDeleted)
                        {
                            existingTask.IsDeleted = false;
                            _context.Update(existingTask);
                        }
                    }
                    await _context.SaveChangesAsync();

                    // Create notification for assigned user
                    if (!string.IsNullOrEmpty(task.AssignedToId))
                    {
                        await _notificationService.CreateTaskAssignedNotification(task);
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating task");
                    ModelState.AddModelError("", "Error creating task. Please try again.");
                }
            }

            var users = _context.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
            ViewData["AssignedToId"] = new SelectList(users, "Id", "FullName", task.AssignedToId);
            ViewData["Categories"] = GetCategoriesSelectList();
            return View(task);
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.Tasks.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();

            var users = _context.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
            ViewData["AssignedToId"] = new SelectList(users, "Id", "FullName", task.AssignedToId);
            ViewData["Categories"] = GetCategoriesSelectList();
            return View(task);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Status,Priority,DueDate,AssignedToId,Category,EstimatedHours,ActualHours")] TaskItem task)
        {
            if (id != task.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var original = await _context.Tasks.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
                    if (original == null) return NotFound();

                    task.CreatedById = original.CreatedById;
                    task.CreatedAt = original.CreatedAt;
                    task.UpdatedAt = DateTime.UtcNow;
                    task.IsDeleted = original.IsDeleted;

                    _context.Update(task);
                    await _context.SaveChangesAsync();

                    if (original.Status != task.Status)
                    {
                        await _notificationService.CreateTaskStatusChangedNotification(task, original.Status);
                    }

                    if (original.AssignedToId != task.AssignedToId && !string.IsNullOrEmpty(task.AssignedToId))
                    {
                        await _notificationService.CreateTaskAssignedNotification(task);
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating task {Id}", id);
                    ModelState.AddModelError("", "Error updating task. Please try again.");
                }
            }

            var users = _context.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
            ViewData["AssignedToId"] = new SelectList(users, "Id", "FullName", task.AssignedToId);
            ViewData["Categories"] = GetCategoriesSelectList();
            return View(task);
        }

        // GET: Tasks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.Tasks.IgnoreQueryFilters()
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            return View(task);
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
            if (task != null)
            {
                task.IsDeleted = true;
                _context.Update(task);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // EMERGENCY FIX: Special action to repair database
        public async Task<IActionResult> FixData()
        {
            try
            {
                // 1. Get all tasks including deleted ones
                var allTasks = await _context.Tasks.IgnoreQueryFilters().ToListAsync();
                var result = new Dictionary<string, object>();
                
                // Log counts
                result["TotalTasksBeforeFix"] = allTasks.Count;
                result["DeletedTasksBeforeFix"] = allTasks.Count(t => t.IsDeleted);
                
                // 2. Undelete all tasks
                foreach (var task in allTasks)
                {
                    task.IsDeleted = false;
                    _context.Update(task);
                }
                await _context.SaveChangesAsync();
                
                // 3. Verify fix
                var tasksAfterFix = await _context.Tasks.ToListAsync();
                result["TotalTasksAfterFix"] = tasksAfterFix.Count;
                
                // 4. Return data summary as JSON
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing data");
                return Json(new { Error = ex.Message });
            }
        }

        // Helper methods
        private IQueryable<TaskItem> ApplySorting(IQueryable<TaskItem> query, string sortOrder)
        {
            return sortOrder switch
            {
                "title_desc" => query.OrderByDescending(t => t.Title),
                "duedate" => query.OrderBy(t => t.DueDate),
                "duedate_desc" => query.OrderByDescending(t => t.DueDate),
                "status" => query.OrderBy(t => t.Status),
                "status_desc" => query.OrderByDescending(t => t.Status),
                "priority" => query.OrderBy(t => t.Priority),
                "priority_desc" => query.OrderByDescending(t => t.Priority),
                _ => query.OrderBy(t => t.Title),
            };
        }

        private SelectList GetCategoriesSelectList()
        {
            var categories = new[] { "Development", "Design", "Research", "Testing", "Documentation", "Maintenance", "Meeting", "Other" };
            return new SelectList(categories);
        }
    }
}
