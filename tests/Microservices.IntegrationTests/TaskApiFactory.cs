using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskService;
using TaskService.Services;

namespace Microservices.IntegrationTests;

public sealed class TaskApiFactory : WebApplicationFactory<TaskServiceEntryPoint>
{
    private readonly string _connectionString;

    public TaskApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public FakeUserDirectoryClient Directory { get; } = new();

    public FakeNotificationConnection Notifications { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(TestConfig.TaskSettings(_connectionString));
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserDirectoryClient>();
            services.AddSingleton<IUserDirectoryClient>(Directory);

            services.RemoveAll<INotificationConnection>();
            services.AddSingleton<INotificationConnection>(Notifications);
        });
    }
}
