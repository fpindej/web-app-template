using System.Diagnostics;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyProject.Infrastructure.Features.Jobs.Options;
using MyProject.Infrastructure.Features.Jobs.Services;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Infrastructure.Features.Jobs.Extensions;

/// <summary>
/// Extension methods for configuring the Hangfire middleware pipeline and registering recurring jobs.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Root service provider captured at startup, used by <see cref="ExecuteJobAsync"/>
    /// to create a fresh DI scope per job execution. Stored statically because Hangfire
    /// serializes job arguments as JSON — <see cref="IServiceProvider"/> is not serializable.
    /// </summary>
    private static IServiceProvider? _rootServiceProvider;

    /// <summary>
    /// Configures the Hangfire dashboard (development only) and registers all recurring jobs
    /// discovered via <see cref="IRecurringJobDefinition"/> implementations.
    /// <para>
    /// In development, the built-in Hangfire dashboard is available at <c>/hangfire</c>
    /// with no authentication. In production, use the admin API endpoints instead.
    /// </para>
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseJobScheduling(this IApplicationBuilder app)
    {
        var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ApplicationBuilderExtensions));

        var options = app.ApplicationServices.GetRequiredService<IOptions<JobSchedulingOptions>>().Value;

        if (!options.Enabled)
        {
            logger.LogInformation("Job scheduling is disabled via configuration");
            return app;
        }

        _rootServiceProvider = app.ApplicationServices;

        var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();

        if (env.IsDevelopment())
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = []
            });
            logger.LogInformation("Hangfire dashboard enabled at /hangfire (development only)");
        }

        RegisterRecurringJobs(app.ApplicationServices, logger);
        RestorePauseStateAsync(app.ApplicationServices, logger).GetAwaiter().GetResult();

        return app;
    }

    private static void RegisterRecurringJobs(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var jobDefinitions = scope.ServiceProvider.GetServices<IRecurringJobDefinition>().ToList();

        if (jobDefinitions.Count == 0)
        {
            logger.LogWarning("No IRecurringJobDefinition implementations found — no jobs registered");
            return;
        }

        foreach (var job in jobDefinitions)
        {
            RecurringJob.AddOrUpdate(
                job.JobId,
                () => ExecuteJobAsync(job.JobId),
                job.CronExpression);

            logger.LogInformation("Registered recurring job '{JobId}' with cron '{CronExpression}'",
                job.JobId, job.CronExpression);
        }

        logger.LogInformation("Registered {Count} recurring job(s)", jobDefinitions.Count);
    }

    /// <summary>
    /// Loads persisted pause state from the database and overrides paused jobs with a never-firing cron.
    /// Called once at startup after <see cref="RegisterRecurringJobs"/> to restore pause state.
    /// </summary>
    private static async Task RestorePauseStateAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyProjectDbContext>();

        var pausedJobs = await dbContext.PausedJobs.ToListAsync();

        if (pausedJobs.Count == 0)
        {
            return;
        }

        foreach (var pausedJob in pausedJobs)
        {
            JobManagementService.PausedJobCrons[pausedJob.JobId] = pausedJob.OriginalCron;

            RecurringJob.AddOrUpdate(
                pausedJob.JobId,
                () => ExecuteJobAsync(pausedJob.JobId),
                JobManagementService.NeverCron);

            logger.LogInformation(
                "Restored pause state for job '{JobId}' (original cron: '{OriginalCron}')",
                pausedJob.JobId, pausedJob.OriginalCron);
        }

        logger.LogInformation("Restored {Count} paused job state(s) from database", pausedJobs.Count);
    }

    /// <summary>
    /// Resolves a job definition from DI and executes it.
    /// This indirection ensures each execution gets a fresh DI scope with proper lifetime management.
    /// <para>
    /// Only the <paramref name="jobId"/> string is passed through Hangfire's serialization —
    /// the service provider is accessed from the static field captured at startup.
    /// </para>
    /// </summary>
    /// <param name="jobId">The job identifier to resolve and execute.</param>
    public static async Task ExecuteJobAsync(string jobId)
    {
        if (_rootServiceProvider is null)
        {
            return;
        }

        var logger = _rootServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ApplicationBuilderExtensions));

        using var scope = _rootServiceProvider.CreateScope();
        var jobDefinitions = scope.ServiceProvider.GetServices<IRecurringJobDefinition>();
        var job = jobDefinitions.FirstOrDefault(j => j.JobId == jobId);

        if (job is null)
        {
            logger.LogWarning("No IRecurringJobDefinition found for job '{JobId}' — skipping execution", jobId);
            return;
        }

        logger.LogInformation("Executing job '{JobId}'", jobId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await job.ExecuteAsync();
            stopwatch.Stop();
            logger.LogInformation("Job '{JobId}' completed in {ElapsedMs}ms", jobId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Job '{JobId}' failed after {ElapsedMs}ms", jobId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
