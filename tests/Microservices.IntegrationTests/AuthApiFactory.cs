using AuthService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Microservices.IntegrationTests;

public sealed class AuthApiFactory : WebApplicationFactory<AuthServiceEntryPoint>
{
    private readonly string _connectionString;

    public AuthApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(TestConfig.AuthSettings(_connectionString));
        });
    }
}
