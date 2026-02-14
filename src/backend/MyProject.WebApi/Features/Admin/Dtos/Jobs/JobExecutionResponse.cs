using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Admin.Dtos.Jobs;

/// <summary>
/// Represents a single job execution history entry.
/// </summary>
public class JobExecutionResponse
{
    /// <summary>
    /// The background job identifier for this execution.
    /// </summary>
    public string JobId { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The execution status (e.g. "Succeeded", "Failed", "Processing").
    /// </summary>
    public string Status { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// When the execution started.
    /// </summary>
    public DateTimeOffset? StartedAt { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// How long the execution took, or null if still running.
    /// </summary>
    public TimeSpan? Duration { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The error message if the execution failed, or null on success.
    /// </summary>
    public string? Error { [UsedImplicitly] get; [UsedImplicitly] init; }
}
