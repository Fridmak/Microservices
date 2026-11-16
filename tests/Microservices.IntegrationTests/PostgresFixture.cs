using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Microservices.IntegrationTests;

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;

    public string AuthConnectionString => BuildConnectionString("AuthDb");

    public string TaskConnectionString => BuildConnectionString("TaskDb");

    public string NotificationsConnectionString => BuildConnectionString("NotificationsDb");

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        await _container.StartAsync();

        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();

        foreach (var database in new[] { "AuthDb", "TaskDb", "NotificationsDb" })
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{database}\"";
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private string BuildConnectionString(string database)
    {
        var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = database
        };

        return builder.ConnectionString;
    }
}
