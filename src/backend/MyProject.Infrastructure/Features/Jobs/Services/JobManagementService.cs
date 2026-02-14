using System.Collections.Concurrent;
using Hangfire;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyProject.Application.Features.Jobs;
using MyProject.Application.Features.Jobs.Dtos;
using MyProject.Domain;
using MyProject.Infrastructure.Features.Jobs.Extensions;
using MyProject.Infrastructure.Features.Jobs.Models;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Infrastructure.Features.Jobs.Services;

/// <summary>
/// Provides Hangfire recurring job management operations for the admin API.
/// Uses the Hangfire monitoring API and <see cref="RecurringJob"/> static API
/// to query, trigger, pause, and remove recurring jobs.
/// <para>
/// Pause state is persisted to the database and cached in a static dictionary
/// so it survives application restarts.
/// </para>
/// </summary>
internal sealed class JobManagementService(
    ILogger<JobManagementService> logger,
    MyProjectDbContext dbContext,
    IEnumerable<IRecurringJobDefinition> jobDefinitions,
    TimeProvider timeProvider) : IJobManagementService
{
    /// <summary>
    /// In-memory cache of paused job cron expressions, backed by the <c>jobs.PausedJobs</c> table.
    /// Thread-safe since Hangfire may run on multiple workers.
    /// </summary>
    internal static readonly ConcurrentDictionary<string, string> PausedJobCrons = new();

    /// <summary>
    /// Cron expression that effectively pauses a job by scheduling it far in the future.
    /// Hangfire does not have a native pause — setting cron to "0 0 31 2 *" (Feb 31) ensures it never fires.
    /// </summary>
    internal const string NeverCron = "0 0 31 2 *";

    /// <inheritdoc />
    public Task<IReadOnlyList<RecurringJobOutput>> GetRecurringJobsAsync()
    {
        using var connection = JobStorage.Current.GetConnection();
        var recurringJobs = connection.GetRecurringJobs();

        var result = recurringJobs.Select(job => new RecurringJobOutput(
            Id: job.Id,
            Cron: PausedJobCrons.ContainsKey(job.Id) ? PausedJobCrons[job.Id] : job.Cron,
            NextExecution: job.NextExecution.HasValue
                ? new DateTimeOffset(job.NextExecution.Value, TimeSpan.Zero)
                : null,
            LastExecution: job.LastExecution.HasValue
                ? new DateTimeOffset(job.LastExecution.Value, TimeSpan.Zero)
                : null,
            LastStatus: job.LastJobState,
            IsPaused: PausedJobCrons.ContainsKey(job.Id),
            CreatedAt: job.CreatedAt.HasValue
                ? new DateTimeOffset(job.CreatedAt.Value, TimeSpan.Zero)
                : null
        )).ToList();

        return Task.FromResult<IReadOnlyList<RecurringJobOutput>>(result);
    }

    /// <inheritdoc />
    public Task<Result<RecurringJobDetailOutput>> GetRecurringJobDetailAsync(string jobId)
    {
        using var connection = JobStorage.Current.GetConnection();
        var recurringJobs = connection.GetRecurringJobs();
        var job = recurringJobs.FirstOrDefault(j => j.Id == jobId);

        if (job is null)
        {
            return Task.FromResult(Result<RecurringJobDetailOutput>.Failure(ErrorMessages.Jobs.NotFound));
        }

        var isPaused = PausedJobCrons.ContainsKey(job.Id);
        var displayCron = isPaused ? PausedJobCrons[job.Id] : job.Cron;

        var executionHistory = GetRecentExecutions(job);

        var detail = new RecurringJobDetailOutput(
            Id: job.Id,
            Cron: displayCron,
            NextExecution: job.NextExecution.HasValue
                ? new DateTimeOffset(job.NextExecution.Value, TimeSpan.Zero)
                : null,
            LastExecution: job.LastExecution.HasValue
                ? new DateTimeOffset(job.LastExecution.Value, TimeSpan.Zero)
                : null,
            LastStatus: job.LastJobState,
            IsPaused: isPaused,
            CreatedAt: job.CreatedAt.HasValue
                ? new DateTimeOffset(job.CreatedAt.Value, TimeSpan.Zero)
                : null,
            ExecutionHistory: executionHistory
        );

        return Task.FromResult(Result<RecurringJobDetailOutput>.Success(detail));
    }

    /// <inheritdoc />
    public Task<Result> TriggerJobAsync(string jobId)
    {
        if (!JobExists(jobId))
        {
            logger.LogWarning("Attempted to trigger non-existent job '{JobId}'", jobId);
            return Task.FromResult(Result.Failure(ErrorMessages.Jobs.NotFound));
        }

        try
        {
            RecurringJob.TriggerJob(jobId);
            logger.LogInformation("Manually triggered job '{JobId}'", jobId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to trigger job '{JobId}'", jobId);
            return Task.FromResult(Result.Failure(ErrorMessages.Jobs.TriggerFailed));
        }
    }

    /// <inheritdoc />
    public async Task<Result> RemoveJobAsync(string jobId)
    {
        if (!JobExists(jobId))
        {
            logger.LogWarning("Attempted to remove non-existent job '{JobId}'", jobId);
            return Result.Failure(ErrorMessages.Jobs.NotFound);
        }

        RecurringJob.RemoveIfExists(jobId);

        var pausedJob = await dbContext.PausedJobs.FirstOrDefaultAsync(p => p.JobId == jobId);
        if (pausedJob is not null)
        {
            dbContext.PausedJobs.Remove(pausedJob);
            await dbContext.SaveChangesAsync();
        }

        PausedJobCrons.TryRemove(jobId, out _);
        logger.LogWarning("Removed recurring job '{JobId}'", jobId);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> PauseJobAsync(string jobId)
    {
        using var connection = JobStorage.Current.GetConnection();
        var recurringJobs = connection.GetRecurringJobs();
        var job = recurringJobs.FirstOrDefault(j => j.Id == jobId);

        if (job is null)
        {
            logger.LogWarning("Attempted to pause non-existent job '{JobId}'", jobId);
            return Result.Failure(ErrorMessages.Jobs.NotFound);
        }

        if (PausedJobCrons.ContainsKey(jobId))
        {
            logger.LogDebug("Job '{JobId}' is already paused", jobId);
            return Result.Success();
        }

        dbContext.PausedJobs.Add(new PausedJob
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            OriginalCron = job.Cron,
            PausedAt = timeProvider.GetUtcNow().UtcDateTime
        });
        await dbContext.SaveChangesAsync();

        PausedJobCrons[jobId] = job.Cron;
        RecurringJob.AddOrUpdate(jobId, () => NoOp(), NeverCron);
        logger.LogInformation("Paused job '{JobId}' (original cron: '{OriginalCron}')", jobId, job.Cron);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ResumeJobAsync(string jobId)
    {
        if (!JobExists(jobId))
        {
            logger.LogWarning("Attempted to resume non-existent job '{JobId}'", jobId);
            return Result.Failure(ErrorMessages.Jobs.NotFound);
        }

        if (!PausedJobCrons.TryRemove(jobId, out var originalCron))
        {
            logger.LogDebug("Job '{JobId}' is not paused — nothing to resume", jobId);
            return Result.Success();
        }

        var pausedJob = await dbContext.PausedJobs.FirstOrDefaultAsync(p => p.JobId == jobId);
        if (pausedJob is not null)
        {
            dbContext.PausedJobs.Remove(pausedJob);
            await dbContext.SaveChangesAsync();
        }

        RecurringJob.AddOrUpdate(jobId, () => ApplicationBuilderExtensions.ExecuteJobAsync(jobId), originalCron);
        logger.LogInformation("Resumed job '{JobId}' (restored cron: '{RestoredCron}')", jobId, originalCron);
        return Result.Success();
    }

    /// <inheritdoc />
    public Task<Result> RestoreJobsAsync()
    {
        try
        {
            var definitions = jobDefinitions.ToList();

            foreach (var definition in definitions)
            {
                var isPaused = PausedJobCrons.ContainsKey(definition.JobId);
                var cron = isPaused ? NeverCron : definition.CronExpression;

                RecurringJob.AddOrUpdate(
                    definition.JobId,
                    () => ApplicationBuilderExtensions.ExecuteJobAsync(definition.JobId),
                    cron);

                logger.LogInformation(
                    "Restored job '{JobId}' with cron '{Cron}'{PauseNote}",
                    definition.JobId,
                    definition.CronExpression,
                    isPaused ? " (paused)" : "");
            }

            logger.LogInformation("Restored {Count} recurring job(s)", definitions.Count);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to restore jobs");
            return Task.FromResult(Result.Failure(ErrorMessages.Jobs.RestoreFailed));
        }
    }

    /// <summary>
    /// Placeholder method used when pausing to preserve the Hangfire job entry.
    /// The actual job method is re-registered on resume via <c>UseJobScheduling()</c> restart or manual resume.
    /// </summary>
    public static void NoOp() { }

    private static bool JobExists(string jobId)
    {
        using var connection = JobStorage.Current.GetConnection();
        var recurringJobs = connection.GetRecurringJobs();
        return recurringJobs.Any(j => j.Id == jobId);
    }

    private static IReadOnlyList<JobExecutionOutput> GetRecentExecutions(RecurringJobDto job)
    {
        if (string.IsNullOrEmpty(job.LastJobId))
        {
            return [];
        }

        var monitoringApi = JobStorage.Current.GetMonitoringApi();
        var history = new List<JobExecutionOutput>();

        var succeededJobs = monitoringApi.SucceededJobs(0, 20);
        foreach (var succeeded in succeededJobs.Where(s => s.Value.Job?.Type == job.Job?.Type))
        {
            history.Add(new JobExecutionOutput(
                JobId: succeeded.Key,
                Status: "Succeeded",
                StartedAt: succeeded.Value.SucceededAt.HasValue
                    ? new DateTimeOffset(succeeded.Value.SucceededAt.Value, TimeSpan.Zero)
                    : null,
                Duration: succeeded.Value.TotalDuration.HasValue
                    ? TimeSpan.FromMilliseconds(succeeded.Value.TotalDuration.Value)
                    : null,
                Error: null
            ));
        }

        var failedJobs = monitoringApi.FailedJobs(0, 20);
        foreach (var failed in failedJobs.Where(f => f.Value.Job?.Type == job.Job?.Type))
        {
            history.Add(new JobExecutionOutput(
                JobId: failed.Key,
                Status: "Failed",
                StartedAt: failed.Value.FailedAt.HasValue
                    ? new DateTimeOffset(failed.Value.FailedAt.Value, TimeSpan.Zero)
                    : null,
                Duration: null,
                Error: failed.Value.ExceptionMessage
            ));
        }

        return history
            .OrderByDescending(e => e.StartedAt)
            .Take(10)
            .ToList();
    }
}
