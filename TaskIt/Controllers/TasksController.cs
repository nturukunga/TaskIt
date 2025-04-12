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
            try
            {
                if (ModelState.IsValid)
                {
                    // Get current user ID with fallbacks
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    _logger.LogInformation("User claim ID: {Id}", userId ?? "null");
                    
                    if (string.IsNullOrEmpty(userId))
                    {
                        userId = User.Identity?.Name;
                        _logger.LogInformation("Fallback to Identity Name: {Name}", userId ?? "null");
                        
                        if (string.IsNullOrEmpty(userId))
                        {
                            // Emergency fallback - get any user ID from database
                            var anyUser = await _context.Users.FirstOrDefaultAsync();
                            userId = anyUser?.Id;
                            _logger.LogWarning("Emergency fallback to first user ID: {Id}", userId ?? "null");
                            
                            if (string.IsNullOrEmpty(userId))
                            {
                                userId = "unknown-user"; // Last resort
                            }
                        }
                    }
                    
                    // Validate that AssignedToId exists in the Users table if it's not null
                    if (!string.IsNullOrEmpty(task.AssignedToId))
                    {
                        var userExists = await _context.Users.AnyAsync(u => u.Id == task.AssignedToId);
                        if (!userExists)
                        {
                            _logger.LogWarning("Invalid AssignedToId: {AssignedToId} - User doesn't exist", task.AssignedToId);
                            ModelState.AddModelError("AssignedToId", "The selected user doesn't exist in the database.");
                            
                            var users = _context.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
                            ViewData["AssignedToId"] = new SelectList(users, "Id", "FullName", task.AssignedToId);
                            ViewData["Categories"] = GetCategoriesSelectList();
                            return View(task);
                        }
                    }
                    
                    // Set task properties explicitly
                    task.CreatedById = userId;
                    task.CreatedAt = DateTime.UtcNow;
                    task.IsDeleted = false;
                    
                    // Log dropdown selection issue
                    _logger.LogInformation("Selected AssignedToId: {AssignedToId}", 
                        task.AssignedToId ?? "null (no user selected)");
                        
                    _logger.LogInformation("Creating task with properties: {Task}", new {
                        task.Title,
                        task.Description,
                        Status = task.Status.ToString(),
                        Priority = task.Priority.ToString(),
                        task.DueDate,
                        task.CreatedById,
                        task.AssignedToId,
                        task.Category,
                        task.EstimatedHours
                    });
                        
                    // Set self as assignee if none selected as a fallback
                    if (string.IsNullOrEmpty(task.AssignedToId))
                    {
                        _logger.LogInformation("No assignee selected, defaulting to creator");
                        task.AssignedToId = userId;
                    }
                    
                    // Add the task to the context
                    _context.Tasks.Add(task);
                    
                    try
                    {
                        // Try to save changes
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Task created successfully with ID: {Id}", task.Id);
                        
                        // Verify task was actually saved
                        var savedTask = await _context.Tasks.FindAsync(task.Id);
                        if (savedTask != null)
                        {
                            _logger.LogInformation("Verified task exists in database");
                            
                            // Create notification for assigned user
                            if (!string.IsNullOrEmpty(task.AssignedToId))
                            {
                                await _notificationService.CreateTaskAssignedNotification(task);
                            }
                            
                            return RedirectToAction(nameof(Index));
                        }
                        else
                        {
                            _logger.LogWarning("Task appears to be missing after save!");
                            ModelState.AddModelError("", "Task was created but could not be retrieved. Please check the task list.");
                        }
                    }
                    catch (DbUpdateException ex)
                    {
                        _logger.LogError(ex, "Database error creating task. Details: {Message}", ex.InnerException?.Message ?? ex.Message);
                        ModelState.AddModelError("", $"Database error: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }
                else
                {
                    // Log model validation errors
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { x.Key, x.Value.Errors })
                        .ToList();
                        
                    _logger.LogWarning("Model validation failed: {@Errors}", errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating task");
                ModelState.AddModelError("", $"Error creating task: {ex.Message}");
            }

            // If we get here, something failed, redisplay form
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
            if (id != task.Id)
            {
                _logger.LogWarning("ID mismatch in Edit: route ID {RouteId} != form ID {FormId}", id, task.Id);
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        // Get the original task
                        var original = await _context.Tasks.IgnoreQueryFilters().AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Id == id);
                        
                        if (original == null)
                        {
                            _logger.LogWarning("Task not found for editing: {Id}", id);
                            return NotFound();
                        }

                        // Validate that AssignedToId exists in the Users table if it's not null
                        if (!string.IsNullOrEmpty(task.AssignedToId))
                        {
                            var userExists = await _context.Users.AnyAsync(u => u.Id == task.AssignedToId);
                            if (!userExists)
                            {
                                _logger.LogWarning("Invalid AssignedToId: {AssignedToId} - User doesn't exist", task.AssignedToId);
                                ModelState.AddModelError("AssignedToId", "The selected user doesn't exist in the database.");
                                
                                var users = _context.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
                                ViewData["AssignedToId"] = new SelectList(users, "Id", "FullName", task.AssignedToId);
                                ViewData["Categories"] = GetCategoriesSelectList();
                                return View(task);
                            }
                        }

                        _logger.LogInformation("Updating task: {@Task}", new { 
                            task.Id, 
                            task.Title, 
                            task.Description, 
                            Status = task.Status.ToString(), 
                            Priority = task.Priority.ToString(), 
                            task.DueDate, 
                            task.AssignedToId,
                            task.Category,
                            task.EstimatedHours,
                            task.ActualHours
                        });

                        // Keep original metadata
                        task.CreatedById = original.CreatedById;
                        task.CreatedAt = original.CreatedAt;
                        task.UpdatedAt = DateTime.UtcNow;
                        task.IsDeleted = original.IsDeleted;

                        // Detach any existing entity with the same ID
                        var existingEntry = _context.Entry(original);
                        if (existingEntry.State != EntityState.Detached)
                        {
                            existingEntry.State = EntityState.Detached;
                        }

                        // Mark entity as modified
                        _context.Entry(task).State = EntityState.Modified;
                        
                        try
                        {
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Task updated successfully: {Id}", task.Id);

                            // Send notifications if needed
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
                        catch (DbUpdateConcurrencyException ex)
                        {
                            if (!await TaskItemExists(task.Id))
                            {
                                _logger.LogWarning("Task not found during concurrency exception: {Id}", id);
                                return NotFound();
                            }
                            else
                            {
                                _logger.LogError(ex, "Concurrency error updating task {Id}", id);
                                ModelState.AddModelError("", "The task was modified by another user. Please reload and try again.");
                            }
                        }
                        catch (DbUpdateException dbEx)
                        {
                            _logger.LogError(dbEx, "Database error updating task {Id}. Error: {Error}", 
                                id, dbEx.InnerException?.Message ?? dbEx.Message);
                            
                            if (dbEx.InnerException != null && dbEx.InnerException.Message.Contains("FK_Tasks_AspNetUsers_AssignedToId"))
                            {
                                // Foreign key constraint error
                                ModelState.AddModelError("AssignedToId", "The selected user doesn't exist in the database.");
                                task.AssignedToId = null; // Reset to prevent further errors
                            }
                            else
                            {
                                ModelState.AddModelError("", $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating task {Id}", id);
                        ModelState.AddModelError("", $"Error updating task: {ex.Message}");
                    }
                }
                else
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { x.Key, x.Value.Errors })
                        .ToList();
                        
                    _logger.LogWarning("Model validation failed for task edit: {@Errors}", errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Edit action for task {Id}", id);
                ModelState.AddModelError("", $"An unexpected error occurred: {ex.Message}");
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

        // Add this new action to help diagnose database issues
        [HttpGet]
        public async Task<IActionResult> DiagnoseDatabase()
        {
            var result = new Dictionary<string, object>();
            
            try
            {
                // Check for users in the database
                var users = await _context.Users.ToListAsync();
                result["TotalUsers"] = users.Count;
                result["Users"] = users.Select(u => new { 
                    u.Id, 
                    u.UserName,
                    u.Email,
                    u.FirstName,
                    u.LastName
                }).ToList();
                
                // Check for tasks
                var tasks = await _context.Tasks.IgnoreQueryFilters().ToListAsync();
                result["TotalTasks"] = tasks.Count;
                
                // Check for assigned tasks with invalid user IDs
                var invalidAssignments = tasks
                    .Where(t => !string.IsNullOrEmpty(t.AssignedToId) && !users.Any(u => u.Id == t.AssignedToId))
                    .Select(t => new { 
                        t.Id, 
                        t.Title, 
                        t.AssignedToId
                    })
                    .ToList();
                    
                result["InvalidAssignments"] = invalidAssignments;
                result["InvalidAssignmentCount"] = invalidAssignments.Count;
                
                // Fix invalid assignments by setting them to null
                if (invalidAssignments.Any())
                {
                    foreach (var task in tasks.Where(t => !string.IsNullOrEmpty(t.AssignedToId) && 
                                                     !users.Any(u => u.Id == t.AssignedToId)))
                    {
                        task.AssignedToId = null;
                        _context.Update(task);
                    }
                    
                    await _context.SaveChangesAsync();
                    result["FixedAssignments"] = true;
                }
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DiagnoseDatabase");
                result["Error"] = ex.Message;
                result["StackTrace"] = ex.StackTrace;
                return Json(result);
            }
        }

        // Add this new action to help fix database schema issues
        [HttpGet]
        public async Task<IActionResult> FixDatabaseSchema()
        {
            var result = new Dictionary<string, object>();
            
            try
            {
                // Get SQL connection to run raw SQL
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                try
                {
                    // Check if any tasks have null CreatedById
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM \"Tasks\" WHERE \"CreatedById\" IS NULL";
                        var nullCreatedByCount = Convert.ToInt32(await command.ExecuteScalarAsync());
                        result["NullCreatedByCount"] = nullCreatedByCount;
                        
                        if (nullCreatedByCount > 0)
                        {
                            // Fix null CreatedById values
                            var adminId = await _context.Users.Select(u => u.Id).FirstOrDefaultAsync();
                            if (!string.IsNullOrEmpty(adminId))
                            {
                                using (var updateCommand = connection.CreateCommand())
                                {
                                    updateCommand.CommandText = $"UPDATE \"Tasks\" SET \"CreatedById\" = @adminId WHERE \"CreatedById\" IS NULL";
                                    var param = updateCommand.CreateParameter();
                                    param.ParameterName = "@adminId";
                                    param.Value = adminId;
                                    updateCommand.Parameters.Add(param);
                                    
                                    var rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                                    result["FixedNullCreatedBy"] = rowsAffected;
                                }
                            }
                        }
                    }
                    
                    // Check if any tasks have invalid AssignedToId
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT COUNT(*) 
                            FROM ""Tasks"" t 
                            LEFT JOIN ""AspNetUsers"" u ON t.""AssignedToId"" = u.""Id""
                            WHERE t.""AssignedToId"" IS NOT NULL AND u.""Id"" IS NULL";
                            
                        var invalidAssignmentsCount = Convert.ToInt32(await command.ExecuteScalarAsync());
                        result["InvalidAssignmentsCount"] = invalidAssignmentsCount;
                        
                        if (invalidAssignmentsCount > 0)
                        {
                            // Fix invalid AssignedToId values by setting them to NULL
                            using (var updateCommand = connection.CreateCommand())
                            {
                                updateCommand.CommandText = @"
                                    UPDATE ""Tasks"" 
                                    SET ""AssignedToId"" = NULL
                                    WHERE ""AssignedToId"" IS NOT NULL AND ""AssignedToId"" NOT IN (SELECT ""Id"" FROM ""AspNetUsers"")";
                                    
                                var rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                                result["FixedInvalidAssignments"] = rowsAffected;
                            }
                        }
                    }
                    
                    result["Success"] = true;
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        await connection.CloseAsync();
                    }
                }
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing database schema");
                result["Error"] = ex.Message;
                result["StackTrace"] = ex.StackTrace;
                return Json(result);
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

        private async Task<bool> TaskItemExists(int id)
        {
            return await _context.Tasks.IgnoreQueryFilters().AnyAsync(e => e.Id == id);
        }
    }
}
