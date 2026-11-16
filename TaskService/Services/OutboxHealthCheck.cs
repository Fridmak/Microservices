using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TaskService.Models.Db;
using TaskService.Options;

namespace TaskService.Services
{
    public sealed class OutboxHealthCheck : IHealthCheck
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly OutboxOptions _options;

        public OutboxHealthCheck(IServiceScopeFactory scopeFactory, IOptions<OutboxOptions> options)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TaskDbContext>();

            var deadLetterCount = await dbContext.OutboxMessages
                .CountAsync(
                    message => message.ProcessedAt == null
                        && message.Attempts >= _options.MaxAttempts,
                    cancellationToken);

            return deadLetterCount == 0
                ? HealthCheckResult.Healthy("Outbox has no dead-letter messages.")
                : HealthCheckResult.Degraded($"Outbox has {deadLetterCount} dead-letter message(s).");
        }
    }
}
