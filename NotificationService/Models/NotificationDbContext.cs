using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace NotificationService.Models
{
    public class NotificationDbContext : DbContext
    {
        public DbSet<Notification> Notifications {get; set;}

        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
        {
        }
    }
}
