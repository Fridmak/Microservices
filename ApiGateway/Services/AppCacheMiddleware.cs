using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;

namespace ApiGateway.Services
{
    public class AppCacheMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AppCacheService _cacheService;
        private readonly IConfiguration _configuration;

        public AppCacheMiddleware(RequestDelegate next, AppCacheService cacheService, IConfiguration configuration)
        {
            _next = next;
            _cacheService = cacheService;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!IsCachingEnabled() || !ShouldCacheRequest(context))
            {
                await _next(context);
                return;
            }

            var cacheKey = GenerateCacheKey(context);
            var cachedResponse = _cacheService.Get<byte[]>(cacheKey);

            if (cachedResponse != null)
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.Body.WriteAsync(cachedResponse);
                return;
            }

            var originalBody = context.Response.Body;
            await using var newBody = new MemoryStream();

            try
            {
                context.Response.Body = newBody;
                await _next(context);

                if (context.Response.StatusCode == 200)
                {
                    var maxBodySize = _configuration.GetValue<long>("Cache:MaxBodySizeBytes", 1_048_576);
                    if (newBody.Length <= maxBodySize)
                    {
                        var buffer = newBody.ToArray();
                        _cacheService.Set(cacheKey, buffer);

                        newBody.Seek(0, SeekOrigin.Begin);
                        await newBody.CopyToAsync(originalBody);
                    }
                    else
                    {
                        newBody.Seek(0, SeekOrigin.Begin);
                        await newBody.CopyToAsync(originalBody);
                    }
                }
                else
                {
                    newBody.Seek(0, SeekOrigin.Begin);
                    await newBody.CopyToAsync(originalBody);
                }
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        }

        private bool IsCachingEnabled()
        {
            return _configuration.GetValue<bool>("Cache:Enabled", true);
        }

        private bool ShouldCacheRequest(HttpContext context)
        {
            if (context.Request.Method != HttpMethods.Get) return false;
            if (context.Request.Path.StartsWithSegments("/api/auth")) return false;
            if (context.Request.Path.StartsWithSegments("/api/notifications")) return false;
            if (!string.IsNullOrEmpty(context.Request.Headers["Authorization"])) return false;
            if (context.Request.Path.StartsWithSegments("/api/tasks")) return false; // Можем менять, лучше не кэшироватс
            return true;
        }

        private string GenerateCacheKey(HttpContext context)
        {
            var path = context.Request.Path;
            var queryParams = context.Request.Query.OrderBy(kvp => kvp.Key)
                                 .Select(kvp => $"{kvp.Key}={kvp.Value}");

            var queryString = queryParams.Any() ? $"?{string.Join("&", queryParams)}" : "";
            return $"CACHE_{path}{queryString}";
        }
    }
}