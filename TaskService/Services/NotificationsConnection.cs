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
            Guid? author = null);
    }


    public class NotificationConnection : INotificationConnection
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NotificationConnection> _logger;
        private readonly string _notificationServiceBaseUrl;
        private readonly string _apiKey;

        public NotificationConnection(
            HttpClient httpClient,
            ILogger<NotificationConnection> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;

            _notificationServiceBaseUrl = configuration["Services:NotificationService:BaseUrl"]
                ?? throw new InvalidOperationException("BaseUrl not configured.");

            _apiKey = configuration["Services:NotificationService:Security:ApiKey"]
                ?? throw new InvalidOperationException("ApiKey not configured.");
        }

        public async Task<bool> SendNotificationAsync(Notification notification)
        {
            try
            {
                var url = $"{_notificationServiceBaseUrl}/api/notifications";
                _logger.LogInformation($"Отправка уведомления на {url}");

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = JsonContent.Create(notification, options: new JsonSerializerOptions { WriteIndented = true })
                };

                request.Headers.Add("Service-Api-Key", _apiKey);

                var response = await _httpClient.SendAsync(request);

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
            Guid? author = null)
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