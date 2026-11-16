using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Shared.Serialization;
using Xunit;

namespace Microservices.IntegrationTests;

[Collection("postgres")]
public sealed class AuthApiTests : IDisposable
{
    private readonly AuthApiFactory _factory;

    public AuthApiTests(PostgresFixture postgres)
    {
        _factory = new AuthApiFactory(postgres.AuthConnectionString);
    }

    [Fact]
    public async Task Register_login_me_logout_and_validation_work()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var email = $"auth-{Guid.NewGuid():N}@example.com";
        const string password = "password123";

        using var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { username = "Alice", email, password },
            JsonDefaults.Api);

        register.StatusCode.Should().Be(HttpStatusCode.Created);

        using var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = email.ToUpperInvariant(), password },
            JsonDefaults.Api);

        login.StatusCode.Should().Be(HttpStatusCode.OK);

        // AuthService expects a bearer token; the gateway is responsible for the cookie -> header conversion.
        login.Headers.TryGetValues("Set-Cookie", out var setCookies).Should().BeTrue();
        var token = setCookies!.First().Split(';')[0].Split('=', 2)[1];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var me = await client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);

        using var users = await client.GetAsync("/api/auth/users?search=Alice");
        users.StatusCode.Should().Be(HttpStatusCode.OK);

        using var logout = await client.PostAsync("/api/auth/logout", content: null);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        client.DefaultRequestHeaders.Authorization = null;
        using var meAfterLogout = await client.GetAsync("/api/auth/me");
        meAfterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var duplicate = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { username = "Alice2", email = email.ToUpperInvariant(), password },
            JsonDefaults.Api);

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var invalid = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { username = "Bad", email = "not-an-email", password },
            JsonDefaults.Api);

        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
