using Microsoft.AspNetCore.SignalR;
using Shared.Models;

namespace NotificationService.Services
{

    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public async Task SendNotification(Notification notification)
        {
            try
            {
                await Clients.User(notification.UserId.ToString()).SendAsync("ReceiveNotification", notification);

                _logger.LogInformation($"Уведомление отправлено пользователю {notification.UserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отправке уведомления через SignalR пользователю {notification.UserId}");
            }
        }

        public async Task GetNotificationsForUser(Guid userId)
        {
            // Этот метод можно использовать для отправки всех непрочитанных уведомлений при подключении
            // Реализация зависит от вашего хранилища
            return;
        }
    }
}