using Microsoft.Extensions.Caching.Memory;

namespace ApiGateway.Services
{
    public class AppCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly int _defaultTtlSeconds;
        private readonly bool _slidingEnabled;
        private readonly int? _absoluteMinutes;

        public AppCacheService(IMemoryCache cache, IConfiguration configuration)
        {
            _cache = cache;
            var cacheSection = configuration.GetSection("Cache");

            _defaultTtlSeconds = cacheSection.GetValue<int>("DefaultTTLSeconds", 60);
            _slidingEnabled = cacheSection.GetValue<bool>("SlidingExpirationEnabled", true);
            _absoluteMinutes = cacheSection.GetValue<int?>("AbsoluteExpirationMinutes");
        }

        public void Set(string key, object value)
        {
            var options = new MemoryCacheEntryOptions();

            if (_slidingEnabled)
                options.SlidingExpiration = TimeSpan.FromSeconds(_defaultTtlSeconds);
            else
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_defaultTtlSeconds);

            if (_absoluteMinutes.HasValue)
                options.AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_absoluteMinutes.Value);

            _cache.Set(key, value, options);
        }

        public T Get<T>(string key)
        {
            return _cache.Get<T>(key);
        }

        public bool TryGet<T>(string key, out T value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }
    }
}