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
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserTask>(entity =>
            {
                entity.ToTable("Tasks");
                entity.HasKey(task => task.Id);
                entity.Property(task => task.Title).HasMaxLength(200).IsRequired();
                entity.Property(task => task.Comment).HasColumnType("text[]");
                entity.HasIndex(task => new { task.OwnerId, task.IsSoftDeleted });
                entity.HasIndex(task => new { task.AssignedToUserId, task.IsSoftDeleted });
                entity.HasIndex(task => new { task.IsSoftDeleted, task.Status, task.DeadLine });
                entity.HasIndex(task => task.Priority);
            });

            modelBuilder.Entity<TaskHistory>(entity =>
            {
                entity.ToTable("TaskHistories");
                entity.HasKey(history => history.Id);
                entity.Property(history => history.Details).HasMaxLength(500);
                entity.HasIndex(history => new { history.TaskId, history.ChangedAt });
            });

            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("OutboxMessages");
                entity.HasKey(message => message.Id);
                entity.Property(message => message.Type).HasMaxLength(100).IsRequired();
                entity.Property(message => message.Payload).HasColumnType("jsonb").IsRequired();
                entity.Property(message => message.LastError).HasMaxLength(2000);
                entity.HasIndex(message => new { message.ProcessedAt, message.NextAttemptAt });
            });
        }
    }
}
