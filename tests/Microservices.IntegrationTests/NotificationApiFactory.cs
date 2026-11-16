using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using NotificationService;

namespace Microservices.IntegrationTests;

public sealed class NotificationApiFactory : WebApplicationFactory<NotificationServiceEntryPoint>
{
    private readonly string _connectionString;

    public NotificationApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(TestConfig.NotificationSettings(_connectionString));
        });
    }
}
