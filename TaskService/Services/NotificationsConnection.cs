using Shared.Models;
using System.Text.Json;

namespace TaskService.Services
{
    public interface INotificationConnection
    {
        Task<bool> SendNotificationAsync(Notification notification);
        Task<bool> SendTaskNotificationAsync(
            Guid userId,
            NotificationType type,
            string title,
            string message,
            Guid? taskId = null,
            Guid? relatedUserId = null,
            string? author = null);
    }

    public class NotificationConnection : INotificationConnection
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NotificationConnection> _logger;
        private readonly string _notificationServiceBaseUrl;

        public NotificationConnection(
            HttpClient httpClient,
            ILogger<NotificationConnection> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;

            _notificationServiceBaseUrl = configuration["Services:NotificationService:BaseUrl"];
        }

        public async Task<bool> SendNotificationAsync(Notification notification)
        {
            try
            {
                var url = $"{_notificationServiceBaseUrl}/api/notifications";
                _logger.LogInformation($"Отправка уведомления на {url}");

                var response = await _httpClient.PostAsJsonAsync(
                    url,
                    notification,
                    new JsonSerializerOptions { WriteIndented = true });

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Уведомление типа {notification.Type} отправлено пользователю {notification.UserId}");
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Не удалось отправить уведомление. Status: {response.StatusCode}, Response: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отправке уведомления типа {notification.Type}");
                return false;
            }
        }

        public async Task<bool> SendTaskNotificationAsync(
            Guid userId,
            NotificationType type,
            string title,
            string message,
            Guid? taskId = null,
            Guid? relatedUserId = null,
            string? author = null)
        {
            var data = new
            {
                TaskId = taskId,
                RelatedUserId = relatedUserId
            };

            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                Author = author,
                Data = JsonSerializer.Serialize(data)
            };

            return await SendNotificationAsync(notification);
        }
    }
}