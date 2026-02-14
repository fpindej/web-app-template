using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Admin.Dtos.Jobs;

/// <summary>
/// Detailed recurring job response including execution history.
/// </summary>
public class RecurringJobDetailResponse
{
    /// <summary>
    /// The unique job identifier.
    /// </summary>
    public string Id { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The cron expression for the job schedule.
    /// </summary>
    public string Cron { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The next scheduled execution time, or null if paused.
    /// </summary>
    public DateTimeOffset? NextExecution { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The last execution time, or null if never executed.
    /// </summary>
    public DateTimeOffset? LastExecution { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The status of the most recent execution.
    /// </summary>
    public string? LastStatus { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// Whether the job is currently paused.
    /// </summary>
    public bool IsPaused { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// When the job was registered.
    /// </summary>
    public DateTimeOffset? CreatedAt { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// Recent execution history entries.
    /// </summary>
    public IReadOnlyList<JobExecutionResponse> ExecutionHistory { [UsedImplicitly] get; [UsedImplicitly] init; } = [];
}
