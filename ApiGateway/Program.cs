using ApiGateway.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    })
    .AddCookie("Cookies", options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<AppCacheService>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseCookiePolicy();

app.Use(async (context, next) =>
{
    var path = context.Request.Path;

    if (path.StartsWithSegments("/api/auth/login") ||
        path.StartsWithSegments("/api/auth/register"))
    {
        await next();
        return;
    }

    if (context.Request.Cookies.TryGetValue("auth_token", out var token) &&
        !string.IsNullOrEmpty(token))
    {
        context.Request.Headers.Authorization = $"Bearer {token}";
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<AppCacheMiddleware>();

app.MapReverseProxy();

app.Run();