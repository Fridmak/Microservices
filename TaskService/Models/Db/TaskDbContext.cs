using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace TaskService.Models.Db
{
    public class TaskDbContext : DbContext
    {
        public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options)
        {
        }

        public DbSet<UserTask> Tasks { get; set; }
        public DbSet<TaskHistory> TaskHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserTask>()
                .Property(t => t.Comment)
                .HasColumnType("text[]");
        }
    }
}