using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Infrastructure.Features.Jobs.RecurringJobs;

/// <summary>
/// Removes expired and consumed refresh tokens from the database.
/// Runs hourly to keep the RefreshTokens table lean and prevent unbounded growth.
/// </summary>
internal sealed class ExpiredRefreshTokenCleanupJob(
    MyProjectDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<ExpiredRefreshTokenCleanupJob> logger) : IRecurringJobDefinition
{
    /// <inheritdoc />
    public string JobId => "expired-refresh-token-cleanup";

    /// <inheritdoc />
    public string CronExpression => Cron.Hourly();

    /// <summary>
    /// Grace period before deleting expired tokens. Avoids a race condition where a token
    /// expires at the exact moment it is being refreshed — the auth service has loaded the
    /// token (still valid) but hasn't called SaveChangesAsync yet, while the cleanup job
    /// deletes the now-expired row, causing a DbUpdateConcurrencyException.
    /// </summary>
    private static readonly TimeSpan ExpirationGracePeriod = TimeSpan.FromHours(1);

    /// <inheritdoc />
    public async Task ExecuteAsync()
    {
        var cutoff = timeProvider.GetUtcNow().UtcDateTime - ExpirationGracePeriod;

        var deletedCount = await dbContext.RefreshTokens
            .Where(t => t.ExpiredAt < cutoff || t.IsUsed || t.IsInvalidated)
            .ExecuteDeleteAsync();

        logger.LogInformation("Deleted {Count} expired refresh tokens", deletedCount);
    }
}
