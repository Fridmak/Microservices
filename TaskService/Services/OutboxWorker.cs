using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Models;
using Shared.Serialization;
using TaskService.Models.Db;
using TaskService.Options;

namespace TaskService.Services
{
    public sealed class OutboxWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxWorker> _logger;
        private readonly OutboxOptions _options;

        public OutboxWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxWorker> logger,
            IOptions<OutboxOptions> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox processing failed");
                }

                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
            }
        }

        private async Task ProcessBatchAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
            var sender = scope.ServiceProvider.GetRequiredService<INotificationConnection>();

            var now = DateTime.UtcNow;
            var lockUntil = now.AddMinutes(2);

            // Claim a batch atomically. If the process dies after claiming, NextAttemptAt lets another worker retry.
            var messages = await dbContext.OutboxMessages
                .FromSqlInterpolated($"""
                    UPDATE "OutboxMessages"
                    SET "ProcessingAt" = {now}, "NextAttemptAt" = {lockUntil}
                    WHERE "Id" IN (
                        SELECT "Id"
                        FROM "OutboxMessages"
                        WHERE "ProcessedAt" IS NULL
                          AND "NextAttemptAt" <= {now}
                          AND "Attempts" < {_options.MaxAttempts}
                        ORDER BY "CreatedAt"
                        LIMIT {_options.BatchSize}
                        FOR UPDATE SKIP LOCKED
                    )
                    RETURNING *
                    """)
                .ToListAsync(cancellationToken);

            if (messages.Count == 0)
            {
                return;
            }

            foreach (var message in messages)
            {
                try
                {
                    var notification = JsonSerializer.Deserialize<Notification>(message.Payload, JsonDefaults.Api)
                        ?? throw new InvalidOperationException("Outbox payload is not a valid notification.");

                    var sent = await sender.SendNotificationAsync(notification, cancellationToken);

                    if (sent)
                    {
                        message.ProcessedAt = DateTime.UtcNow;
                        message.ProcessingAt = null;
                        message.LastError = null;
                    }
                    else
                    {
                        RegisterFailure(message, "NotificationService returned an unsuccessful status.");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    RegisterFailure(message, ex.Message);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static void RegisterFailure(OutboxMessage message, string error)
        {
            message.Attempts++;
            message.ProcessingAt = null;
            message.LastError = error.Length > 2000 ? error[..2000] : error;

            var delaySeconds = Math.Min(Math.Pow(2, message.Attempts), 300);
            message.NextAttemptAt = DateTime.UtcNow.AddSeconds(delaySeconds);
        }
    }
}
