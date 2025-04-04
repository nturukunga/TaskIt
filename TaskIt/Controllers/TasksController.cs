using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskIt.Data;
using TaskIt.DTOs;
using TaskIt.Models;
using TaskIt.Services;

namespace TaskIt.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
            ApplicationDbContext context,
            IMapper mapper,
            INotificationService notificationService,
            ILogger<TasksController> logger)
        {
            _context = context;
            _mapper = mapper;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: Tasks
        public async Task<IActionResult> Index(string sortOrder, string searchString, string currentFilter, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["TitleSortParm"] = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewData["DueDateSortParm"] = sortOrder == "duedate" ? "duedate_desc" : "duedate";
            ViewData["StatusSortParm"] = sortOrder == "status" ? "status_desc" : "status";
            ViewData["PrioritySortParm"] = sortOrder == "priority" ? "priority_desc" : "priority";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var userId = User.Identity?.Name;
            var tasks = _context.Tasks.Include(t => t.AssignedTo).Include(t => t.CreatedBy)
                .Where(t => t.AssignedToId == userId || t.CreatedById == userId);

            if (!String.IsNullOrEmpty(searchString))
            {
                tasks = tasks.Where(t => t.Title.Contains(searchString) ||
                                        t.Description.Contains(searchString) ||
                                        t.Category.Contains(searchString));
            }

            tasks = sortOrder switch
            {
                "title_desc" => tasks.OrderByDescending(t => t.Title),
                "duedate" => tasks.OrderBy(t => t.DueDate),
                "duedate_desc" => tasks.OrderByDescending(t => t.DueDate),
                "status" => tasks.OrderBy(t => t.Status),
                "status_desc" => tasks.OrderByDescending(t => t.Status),
                "priority" => tasks.OrderBy(t => t.Priority),
                "priority_desc" => tasks.OrderByDescending(t => t.Priority),
                _ => tasks.OrderBy(t => t.Title),
            };

            int pageSize = 10;
            return View(await PaginatedList<TaskItem>.CreateAsync(tasks.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taskItem = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (taskItem == null)
            {
                return NotFound();
            }

            return View(taskItem);
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
        public async Task<IActionResult> Create([Bind("Title,Description,Status,Priority,DueDate,AssignedToId,Category,EstimatedHours")] TaskItem taskItem)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    taskItem.CreatedById = User.Identity?.Name;
                    taskItem.CreatedAt = DateTime.UtcNow;
                    
                    _context.Add(taskItem);
                    await _context.SaveChangesAsync();

                    // Create notification for assigned user
                    if (!string.IsNullOrEmpty(taskItem.AssignedToId))
                    {
                        await _notificationService.CreateTaskAssignedNotification(taskItem);
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating task");
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the task. Please try again.");
                }
            }
            
            var users = _context.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
            ViewData["AssignedToId"] = new SelectList(users, "Id", "FullName", taskItem.AssignedToId);
            ViewData["Categories"] = GetCategoriesSelectList();
            return View(taskItem);
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taskItem = await _context.Tasks.FindAsync(id);
            if (taskItem == null)
            {
                return NotFound();
            }

            var users = _context.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
            ViewData["AssignedToId"] = new SelectList(users, "Id", "FullName", taskItem.AssignedToId);
            ViewData["Categories"] = GetCategoriesSelectList();
            return View(taskItem);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Status,Priority,DueDate,AssignedToId,Category,EstimatedHours,ActualHours")] TaskItem taskItem)
        {
            if (id != taskItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var originalTask = await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
                    if (originalTask == null)
                    {
                        return NotFound();
                    }

                    // Preserve original creation data
                    taskItem.CreatedById = originalTask.CreatedById;
                    taskItem.CreatedAt = originalTask.CreatedAt;
                    taskItem.UpdatedAt = DateTime.UtcNow;
                    
                    _context.Update(taskItem);
                    await _context.SaveChangesAsync();

                    // Create notifications for status changes
                    if (originalTask.Status != taskItem.Status)
                    {
                        await _notificationService.CreateTaskStatusChangedNotification(taskItem, originalTask.Status);
                    }

                    // Create notification for assignment changes
                    if (originalTask.AssignedToId != taskItem.AssignedToId && !string.IsNullOrEmpty(taskItem.AssignedToId))
                    {
                        await _notificationService.CreateTaskAssignedNotification(taskItem);
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!TaskItemExists(taskItem.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, "Concurrency error updating task");
                        ModelState.AddModelError(string.Empty, "The task was modified by another user. Please refresh and try again.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating task");
                    ModelState.AddModelError(string.Empty, "An error occurred while updating the task. Please try again.");
                }
            }
            
            var users = _context.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
            ViewData["AssignedToId"] = new SelectList(users, "Id", "FullName", taskItem.AssignedToId);
            ViewData["Categories"] = GetCategoriesSelectList();
            return View(taskItem);
        }

        // GET: Tasks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taskItem = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (taskItem == null)
            {
                return NotFound();
            }

            return View(taskItem);
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var taskItem = await _context.Tasks.FindAsync(id);
                if (taskItem != null)
                {
                    // Soft delete
                    taskItem.IsDeleted = true;
                    _context.Tasks.Update(taskItem);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task");
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper methods
        private SelectList GetCategoriesSelectList()
        {
            var categories = new[] { "Development", "Design", "Research", "Testing", "Documentation", "Maintenance", "Meeting", "Other" };
            return new SelectList(categories);
        }

        private bool TaskItemExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }
    }

    // Helper class for pagination
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            this.AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}
