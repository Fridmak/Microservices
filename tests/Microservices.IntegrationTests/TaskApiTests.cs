using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Shared.Contracts;
using Shared.Models;
using Shared.Serialization;
using TaskService.Models;
using TaskService.Models.Db;
using Xunit;

namespace Microservices.IntegrationTests;

[Collection("postgres")]
public sealed class TaskApiTests : IDisposable
{
    private readonly TaskApiFactory _factory;

    public TaskApiTests(PostgresFixture postgres)
    {
        _factory = new TaskApiFactory(postgres.TaskConnectionString);
    }

    [Fact]
    public async Task Task_access_history_status_and_delete_flow_are_enforced()
    {
        var ownerId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        _factory.Directory.ExistingUsers.Add(assigneeId);

        using var owner = CreateClient(ownerId);
        using var assignee = CreateClient(assigneeId);

        using var create = await owner.PostAsJsonAsync(
            "/api/tasks",
            new
            {
                title = "Integration task",
                description = "Created by integration test",
                deadLine = DateTime.UtcNow.AddDays(2),
                priority = "High"
            },
            JsonDefaults.Api);

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await create.Content.ReadFromJsonAsync<UserTaskResponse>(JsonDefaults.Api);
        task.Should().NotBeNull();
        task!.OwnerId.Should().Be(ownerId);
        task.AssignedToUserId.Should().Be(ownerId);
        task.Status.Should().Be(UserTaskStatus.Todo);

        using var readAsStranger = await assignee.GetAsync($"/api/tasks/{task.Id}");
        readAsStranger.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var assign = await owner.PutAsJsonAsync(
            $"/api/tasks/{task.Id}/assign",
            new { userId = assigneeId },
            JsonDefaults.Api);

        assign.StatusCode.Should().Be(HttpStatusCode.OK);

        var assignedTask = await assign.Content.ReadFromJsonAsync<UserTaskResponse>(JsonDefaults.Api);
        assignedTask!.AssignedToUserId.Should().Be(assigneeId);

        using var readAsAssignee = await assignee.GetAsync($"/api/tasks/{task.Id}");
        readAsAssignee.StatusCode.Should().Be(HttpStatusCode.OK);

        using var editByAssignee = await assignee.PutAsJsonAsync(
            $"/api/tasks/{task.Id}",
            new { title = "Hacked" },
            JsonDefaults.Api);

        editByAssignee.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var statusUpdate = await assignee.PatchAsJsonAsync(
            $"/api/tasks/{task.Id}/status",
            new { status = "Done" },
            JsonDefaults.Api);

        statusUpdate.StatusCode.Should().Be(HttpStatusCode.OK);
        var completedTask = await statusUpdate.Content.ReadFromJsonAsync<UserTaskResponse>(JsonDefaults.Api);
        completedTask!.Status.Should().Be(UserTaskStatus.Done);
        completedTask.CompletedAt.Should().NotBeNull();

        var history = await owner.GetFromJsonAsync<PagedResult<TaskHistoryResponse>>(
            $"/api/tasks/{task.Id}/history?page=1&pageSize=50",
            JsonDefaults.Api);

        history.Should().NotBeNull();
        history!.TotalCount.Should().BeGreaterThanOrEqualTo(3);
        history.Items.Select(item => item.Type).Should().Contain(new[]
        {
            ChangeType.Created,
            ChangeType.Assigned,
            ChangeType.StatusChanged
        });

        await WaitUntilAsync(() => _factory.Notifications.Sent.Count >= 3, TimeSpan.FromSeconds(10));
        _factory.Notifications.Sent.Select(item => item.Type).Should().Contain(new[]
        {
            NotificationType.TaskCreated,
            NotificationType.TaskAssigned,
            NotificationType.TaskStatusChanged
        });

        using var softDelete = await owner.DeleteAsync($"/api/tasks/{task.Id}");
        softDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var readDeleted = await assignee.GetAsync($"/api/tasks/{task.Id}");
        readDeleted.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var restore = await owner.PostAsync($"/api/tasks/{task.Id}/restore", content: null);
        restore.StatusCode.Should().Be(HttpStatusCode.OK);

        using var readRestored = await owner.GetAsync($"/api/tasks/{task.Id}");
        readRestored.StatusCode.Should().Be(HttpStatusCode.OK);

        using var hardDelete = await owner.DeleteAsync($"/api/tasks/{task.Id}?hard=true");
        hardDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var readHardDeleted = await owner.GetAsync($"/api/tasks/{task.Id}");
        readHardDeleted.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Priority_sorting_uses_business_rank_not_numeric_enum_value()
    {
        var userId = Guid.NewGuid();
        using var client = CreateClient(userId);

        var priorities = new[] { "Low", "Normal", "High", "Necessary" };
        foreach (var priority in priorities)
        {
            using var response = await client.PostAsJsonAsync(
                "/api/tasks",
                new
                {
                    title = $"Sort {priority}",
                    description = "Sorting check",
                    deadLine = DateTime.UtcNow.AddDays(1),
                    priority
                },
                JsonDefaults.Api);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var result = await client.GetFromJsonAsync<PagedResult<UserTaskResponse>>(
            "/api/tasks?sortBy=ByPriority&searchTerm=Sort&page=1&pageSize=10",
            JsonDefaults.Api);

        result.Should().NotBeNull();
        result!.Items.Select(item => item.Priority).Should().ContainInOrder(
            Priority.Necessary,
            Priority.High,
            Priority.Normal,
            Priority.Low);
    }

    private HttpClient CreateClient(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestConfig.CreateToken(userId));
        return client;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var startedAt = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - startedAt > timeout)
            {
                throw new TimeoutException("Condition was not met in time.");
            }

            await Task.Delay(100);
        }
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
