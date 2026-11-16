using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.Extensions
{
    public static class DatabaseExtensions
    {
        public static async Task MigrateDatabaseAsync<TContext>(
            this IServiceProvider services,
            ILogger logger,
            CancellationToken cancellationToken = default)
            where TContext : DbContext
        {
            const int maxAttempts = 10;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await using var scope = services.CreateAsyncScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

                    logger.LogInformation(
                        "Applying migrations for {DbContext} (attempt {Attempt}/{MaxAttempts})",
                        typeof(TContext).Name,
                        attempt,
                        maxAttempts);

                    await dbContext.Database.MigrateAsync(cancellationToken);

                    logger.LogInformation("Migrations applied for {DbContext}", typeof(TContext).Name);
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    var delay = TimeSpan.FromSeconds(Math.Min(attempt * 2, 10));
                    logger.LogWarning(
                        ex,
                        "Migration failed for {DbContext}; retrying in {Delay}",
                        typeof(TContext).Name,
                        delay);
                    await Task.Delay(delay, cancellationToken);
                }
            }

            throw new InvalidOperationException(
                $"Could not apply migrations for {typeof(TContext).Name} after {maxAttempts} attempts.");
        }
    }
}
