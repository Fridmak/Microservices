using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace NotificationService.Models
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
        {
        }

        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(notification => notification.Id);
                entity.Property(notification => notification.Title).HasMaxLength(200).IsRequired();
                entity.Property(notification => notification.Message).HasMaxLength(2000).IsRequired();
                entity.HasIndex(notification => new
                {
                    notification.UserId,
                    notification.IsRead,
                    notification.CreatedAt
                });
            });
        }
    }
}
