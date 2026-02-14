namespace MyProject.Infrastructure.Features.Jobs.Models;

/// <summary>
/// Represents a paused recurring job with its original cron expression.
/// Stored in the database so pause state survives application restarts.
/// </summary>
public class PausedJob
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the Hangfire recurring job identifier.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original cron expression before the job was paused.
    /// </summary>
    public string OriginalCron { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the job was paused.
    /// </summary>
    public DateTime PausedAt { get; set; }
}
