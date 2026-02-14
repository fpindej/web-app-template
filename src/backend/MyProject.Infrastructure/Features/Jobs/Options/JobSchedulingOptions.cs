namespace MyProject.Infrastructure.Features.Jobs.Options;

/// <summary>
/// Configuration options for the Hangfire job scheduling system.
/// Bind to the <c>JobScheduling</c> section of appsettings.
/// </summary>
public sealed class JobSchedulingOptions
{
    public const string SectionName = "JobScheduling";

    /// <summary>
    /// Gets or sets whether the job scheduling system is enabled.
    /// When false, Hangfire server and recurring jobs will not be registered,
    /// but the rest of the application will function normally.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets the number of Hangfire worker threads.
    /// Defaults to the number of logical processors on the machine.
    /// </summary>
    public int WorkerCount { get; init; } = Environment.ProcessorCount;
}
