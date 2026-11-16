using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Exceptions;
using Shared.Options;
using Shared.Security;
using Shared.Serialization;

namespace TaskService.Services
{
    public interface IUserDirectoryClient
    {
        Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    public sealed class UserDirectoryClient : IUserDirectoryClient
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<UserDirectoryClient> _logger;
        private readonly AuthServiceClientOptions _options;

        public UserDirectoryClient(
            HttpClient httpClient,
            IMemoryCache cache,
            ILogger<UserDirectoryClient> logger,
            IOptions<AuthServiceClientOptions> options)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"user-exists:{userId}";
            if (_cache.TryGetValue(cacheKey, out bool cached) && cached)
            {
                return true;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, $"internal/users/{userId}/exists");
            request.Headers.Add(ApiKeyValidator.HeaderName, _options.ApiKey);

            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "AuthService returned {StatusCode} while checking user {UserId}",
                        (int)response.StatusCode,
                        userId);
                    throw new ServiceUnavailableException("AuthService недоступен.");
                }

                var result = await response.Content.ReadFromJsonAsync<UserExistsResponse>(
                    JsonDefaults.Api,
                    cancellationToken);

                if (result?.Exists == true)
                {
                    _cache.Set(cacheKey, true, TimeSpan.FromMinutes(5));
                    return true;
                }

                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "AuthService request failed while checking user {UserId}", userId);
                throw new ServiceUnavailableException("AuthService недоступен.");
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "AuthService request timed out while checking user {UserId}", userId);
                throw new ServiceUnavailableException("AuthService не ответил вовремя.");
            }
        }

        private sealed record UserExistsResponse(bool Exists);
    }
}
