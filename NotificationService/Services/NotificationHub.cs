using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Shared.Models;
using System.Security.Claims;

namespace NotificationService.Services
{
    public class UserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }

    [Authorize]
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
    }
}