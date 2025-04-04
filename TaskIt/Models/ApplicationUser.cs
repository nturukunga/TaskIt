using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace TaskIt.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActive { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<TaskItem> CreatedTasks { get; set; } = new HashSet<TaskItem>();
        public virtual ICollection<TaskItem> AssignedTasks { get; set; } = new HashSet<TaskItem>();
        public virtual ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>();
        
        public string FullName => $"{FirstName} {LastName}";
    }
}
