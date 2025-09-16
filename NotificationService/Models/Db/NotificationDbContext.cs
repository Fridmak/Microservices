using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace NotificationService.Models.Db
{
    public class NotificationDbContext : DbContext
    {
        public DbSet<Notification> Notifications {get; set;}

        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
}
