using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NotificationService.Models;
using Shared.Models;

namespace NotificationService.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<Notification>> GetNotificationsForUserAsync(Guid userId, bool all = false);
        Task<Notification> CreateNotificationAsync(Notification notification);
        Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);
    }
    public class NotificationService : INotificationService
    {
        private readonly NotificationDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            NotificationDbContext context,
            ILogger<NotificationService> logger,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<IEnumerable<Notification>> GetNotificationsForUserAsync(Guid userId, bool all = false)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(x => x.UserId == userId && (all || !x.IsRead))
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation($"Получено {notifications.Count} уведомлений для пользователя {userId}");

                return notifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении уведомлений для пользователя {userId}");
                throw;
            }
        }

        public async Task<Notification> CreateNotificationAsync(Notification notification)
        {
            try
            {
                if (notification == null || notification.UserId == Guid.Empty)
                    throw new ArgumentException("Некорректные данные уведомления");

                notification.Id = Guid.NewGuid();
                notification.CreatedAt = DateTime.UtcNow;
                notification.IsRead = false;

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Создано уведомление типа {notification.Type} для пользователя {notification.UserId}");

                await _hubContext.Clients.User(notification.UserId.ToString())
                    .SendAsync("ReceiveNotification", notification);

                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании уведомления");
                throw;
            }
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);

                if (notification == null)
                    return false;

                if (notification.UserId != userId)
                    return false;

                notification.IsRead = true;
                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Уведомление {notificationId} отмечено как прочитанное");

                await _hubContext.Clients.User(notification.UserId.ToString())
                    .SendAsync("NotificationUpdated", notification);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отметке уведомления {notificationId} как прочитанного");
                return false;
            }
        }
    }
}