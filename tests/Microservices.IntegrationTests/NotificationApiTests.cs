using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Shared.Contracts;
using Shared.Models;
using Shared.Serialization;
using Xunit;

namespace Microservices.IntegrationTests;

[Collection("postgres")]
public sealed class NotificationApiTests : IDisposable
{
    private readonly NotificationApiFactory _factory;

    public NotificationApiTests(PostgresFixture postgres)
    {
        _factory = new NotificationApiFactory(postgres.NotificationsConnectionString);
    }

    [Fact]
    public async Task Notifications_are_scoped_to_current_user_and_idempotent()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        using var publicClient = _factory.CreateClient();
        using var userAClient = CreateClient(userA);
        using var userBClient = CreateClient(userB);

        var notificationId = Guid.NewGuid();
        var payload = new
        {
            id = notificationId,
            userId = userA,
            title = "Integration notification",
            message = "Hello",
            type = "TaskCreated"
        };

        using var first = await PostInternalAsync(publicClient, payload, withApiKey: true);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await first.Content.ReadFromJsonAsync<Notification>(JsonDefaults.Api);
        created!.Id.Should().Be(notificationId);

        using var second = await PostInternalAsync(publicClient, payload, withApiKey: true);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var duplicate = await second.Content.ReadFromJsonAsync<Notification>(JsonDefaults.Api);
        duplicate!.Id.Should().Be(notificationId);

        var notificationsA = await userAClient.GetFromJsonAsync<PagedResult<Notification>>(
            "/api/notifications?page=1&pageSize=20",
            JsonDefaults.Api);
        notificationsA!.TotalCount.Should().Be(1);

        var notificationsB = await userBClient.GetFromJsonAsync<PagedResult<Notification>>(
            "/api/notifications?page=1&pageSize=20",
            JsonDefaults.Api);
        notificationsB!.TotalCount.Should().Be(0);

        using var unreadBefore = await userAClient.GetAsync("/api/notifications/unread-count");
        unreadBefore.StatusCode.Should().Be(HttpStatusCode.OK);

        using var markRead = await userAClient.PutAsync($"/api/notifications/{notificationId}/read", null);
        markRead.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var notificationsAfterRead = await userAClient.GetFromJsonAsync<PagedResult<Notification>>(
            "/api/notifications?unreadOnly=true&page=1&pageSize=20",
            JsonDefaults.Api);
        notificationsAfterRead!.TotalCount.Should().Be(0);

        using var withoutKey = await PostInternalAsync(publicClient, payload, withApiKey: false);
        withoutKey.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateClient(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestConfig.CreateToken(userId));
        return client;
    }

    private static async Task<HttpResponseMessage> PostInternalAsync(
        HttpClient client,
        object payload,
        bool withApiKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/internal/notifications")
        {
            Content = JsonContent.Create(payload, options: JsonDefaults.Api)
        };

        if (withApiKey)
        {
            request.Headers.Add("Service-Api-Key", TestConfig.ServiceApiKey);
        }

        return await client.SendAsync(request);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
