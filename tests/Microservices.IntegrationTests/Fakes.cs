using System.Collections.Concurrent;
using Shared.Models;
using TaskService.Services;

namespace Microservices.IntegrationTests;

public sealed class FakeUserDirectoryClient : IUserDirectoryClient
{
    public HashSet<Guid> ExistingUsers { get; } = new();

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ExistingUsers.Contains(userId));
    }
}

public sealed class FakeNotificationConnection : INotificationConnection
{
    public ConcurrentBag<Notification> Sent { get; } = new();

    public Task<bool> SendNotificationAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        Sent.Add(notification);
        return Task.FromResult(true);
    }
}
