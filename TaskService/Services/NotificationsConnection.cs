using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using Shared.Options;
using Shared.Security;
using Shared.Serialization;

namespace TaskService.Services
{
    public interface INotificationConnection
    {
        Task<bool> SendNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
    }

    public sealed class NotificationConnection : INotificationConnection
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NotificationConnection> _logger;
        private readonly NotificationClientOptions _options;

        public NotificationConnection(
            HttpClient httpClient,
            ILogger<NotificationConnection> logger,
            IOptions<NotificationClientOptions> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<bool> SendNotificationAsync(
            Notification notification,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "internal/notifications")
            {
                Content = JsonContent.Create(notification, options: JsonDefaults.Api)
            };
            request.Headers.Add(ApiKeyValidator.HeaderName, _options.ApiKey);

            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict)
                {
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "NotificationService returned {StatusCode}: {Response}",
                    (int)response.StatusCode,
                    errorContent);
                return false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Failed to send notification {NotificationId}", notification.Id);
                return false;
            }
        }
    }
}
