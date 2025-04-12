using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskIt.Models;

namespace TaskIt.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Tasks entity
            builder.Entity<TaskItem>(entity => 
            {
                entity.ToTable("Tasks");
                
                entity.HasKey(t => t.Id);
                
                entity.Property(t => t.Title)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(t => t.Description)
                    .HasMaxLength(500);
                
                entity.Property(t => t.Status)
                    .IsRequired()
                    .HasConversion<int>();
                
                entity.Property(t => t.Priority)
                    .IsRequired()
                    .HasConversion<int>();
                
                entity.Property(t => t.CreatedById)
                    .IsRequired();
                
                entity.HasOne(t => t.CreatedBy)
                    .WithMany(u => u.CreatedTasks)
                    .HasForeignKey(t => t.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.AssignedTo)
                    .WithMany(u => u.AssignedTasks)
                    .HasForeignKey(t => t.AssignedToId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Notifications entity
            builder.Entity<Notification>(entity => 
            {
                entity.ToTable("Notifications");
                
                entity.HasKey(n => n.Id);
                
                entity.Property(n => n.Message)
                    .IsRequired()
                    .HasMaxLength(200);
                
                entity.Property(n => n.Type)
                    .IsRequired()
                    .HasConversion<int>();
                
                entity.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(n => n.Task)
                    .WithMany(t => t.Notifications)
                    .HasForeignKey(n => n.TaskId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Query filter for soft delete
            builder.Entity<TaskItem>().HasQueryFilter(t => !t.IsDeleted);
        }
    }
}
