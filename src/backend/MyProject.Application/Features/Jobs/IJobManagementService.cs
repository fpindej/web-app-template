using MyProject.Application.Features.Jobs.Dtos;
using MyProject.Domain;

namespace MyProject.Application.Features.Jobs;

/// <summary>
/// Provides query and management operations for Hangfire recurring jobs.
/// <para>
/// Used by the admin controller to expose job management through the API.
/// Job definitions themselves are registered at startup via
/// <c>UseJobScheduling()</c> in the middleware pipeline.
/// </para>
/// </summary>
public interface IJobManagementService
{
    /// <summary>
    /// Gets all registered recurring jobs.
    /// </summary>
    /// <returns>A list of recurring job summaries.</returns>
    Task<IReadOnlyList<RecurringJobOutput>> GetRecurringJobsAsync();

    /// <summary>
    /// Gets detailed information about a single recurring job, including recent execution history.
    /// </summary>
    /// <param name="jobId">The recurring job identifier.</param>
    /// <returns>The job details, or a failure if not found.</returns>
    Task<Result<RecurringJobDetailOutput>> GetRecurringJobDetailAsync(string jobId);

    /// <summary>
    /// Manually triggers an immediate execution of a recurring job.
    /// </summary>
    /// <param name="jobId">The recurring job identifier.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> TriggerJobAsync(string jobId);

    /// <summary>
    /// Removes a recurring job from the scheduler.
    /// </summary>
    /// <param name="jobId">The recurring job identifier.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> RemoveJobAsync(string jobId);

    /// <summary>
    /// Pauses a recurring job by disabling its schedule.
    /// </summary>
    /// <param name="jobId">The recurring job identifier.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> PauseJobAsync(string jobId);

    /// <summary>
    /// Resumes a previously paused recurring job, restoring its original schedule.
    /// </summary>
    /// <param name="jobId">The recurring job identifier.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> ResumeJobAsync(string jobId);

    /// <summary>
    /// Re-registers all recurring job definitions, restoring any jobs deleted from the dashboard.
    /// Paused jobs are re-registered with a disabled schedule to preserve their pause state.
    /// </summary>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> RestoreJobsAsync();
}
