using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Microservices.IntegrationTests;

public static class TestConfig
{
    public const string JwtKey = "integration-test-key-0123456789abcdef";
    public const string JwtIssuer = "AuthService";
    public const string JwtAudience = "TaskManagementSystem";
    public const string ServiceApiKey = "integration-test-api-key";

    public static Dictionary<string, string?> AuthSettings(string connectionString) => new()
    {
        ["ConnectionStrings:DefaultConnection"] = connectionString,
        ["Jwt:Key"] = JwtKey,
        ["Jwt:Issuer"] = JwtIssuer,
        ["Jwt:Audience"] = JwtAudience,
        ["Jwt:ExpiryHours"] = "24",
        ["Auth:Cookie:Secure"] = "false",
        ["Services:Security:ApiKey"] = ServiceApiKey,
        ["Database:MigrateOnStartup"] = "true"
    };

    public static Dictionary<string, string?> TaskSettings(string connectionString) => new()
    {
        ["ConnectionStrings:DefaultConnection"] = connectionString,
        ["Jwt:Key"] = JwtKey,
        ["Jwt:Issuer"] = JwtIssuer,
        ["Jwt:Audience"] = JwtAudience,
        ["Services:AuthService:BaseUrl"] = "http://localhost",
        ["Services:AuthService:ApiKey"] = ServiceApiKey,
        ["Services:NotificationService:BaseUrl"] = "http://localhost",
        ["Services:NotificationService:ApiKey"] = ServiceApiKey,
        ["Outbox:BatchSize"] = "10",
        ["Outbox:MaxAttempts"] = "5",
        ["Outbox:PollIntervalSeconds"] = "1",
        ["Database:MigrateOnStartup"] = "true"
    };

    public static Dictionary<string, string?> NotificationSettings(string connectionString) => new()
    {
        ["ConnectionStrings:DefaultConnection"] = connectionString,
        ["Jwt:Key"] = JwtKey,
        ["Jwt:Issuer"] = JwtIssuer,
        ["Jwt:Audience"] = JwtAudience,
        ["Services:Security:ApiKey"] = ServiceApiKey,
        ["Database:MigrateOnStartup"] = "true"
    };

    public static string CreateToken(Guid userId, string email = "user@example.com", string name = "User")
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
