using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Net.Http.Headers;
using Shared.Exceptions;
using Shared.Extensions;
using Shared.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());
});

var corsOptions = builder.Configuration
    .GetSection(CorsOptions.SectionName)
    .Get<CorsOptions>() ?? new CorsOptions();
var allowedOrigins = corsOptions.AllowedOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("frontend", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
});

builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();

if (allowedOrigins.Length > 0)
{
    app.UseCors("frontend");
}

app.UseRateLimiter();

app.Use(async (context, next) =>
{
    if (context.Request.Cookies.TryGetValue("auth_token", out var token)
        && !string.IsNullOrEmpty(token))
    {
        if (!context.Request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            context.Request.Headers.Authorization = $"Bearer {token}";
        }

        // Do not forward the JWT cookie to internal services.
        var remainingCookies = context.Request.Cookies
            .Where(cookie => !string.Equals(cookie.Key, "auth_token", StringComparison.Ordinal))
            .Select(cookie => $"{cookie.Key}={cookie.Value}")
            .ToArray();

        if (remainingCookies.Length == 0)
        {
            context.Request.Headers.Remove(HeaderNames.Cookie);
        }
        else
        {
            context.Request.Headers[HeaderNames.Cookie] = string.Join("; ", remainingCookies);
        }
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapReverseProxy();

app.Run();

public partial class Program { }
