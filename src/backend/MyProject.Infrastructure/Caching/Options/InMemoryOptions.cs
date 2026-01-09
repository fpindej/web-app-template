using System.ComponentModel.DataAnnotations;

namespace MyProject.Infrastructure.Caching.Options;

/// <summary>
/// Configuration options for in-memory distributed cache.
/// Used when Redis is disabled.
/// </summary>
public sealed class InMemoryOptions : IValidatableObject
{
    /// <summary>
    /// Gets or sets the size limit for in-memory cache entries.
    /// Each cached item should specify a size; this limits total size.
    /// Required when Redis is disabled. Set to null for no limit (not recommended for production).
    /// </summary>
    public int? SizeLimit { get; init; } = 1024;

    /// <summary>
    /// Gets or sets the minimum length of time between successive scans for expired items.
    /// </summary>
    public TimeSpan ExpirationScanFrequency { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the maximum percentage of the cache that can be compacted during a compaction operation.
    /// Value should be between 0 and 1 (e.g., 0.05 = 5%).
    /// </summary>
    public double CompactionPercentage { get; init; } = 0.05;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SizeLimit is null)
        {
            yield return new ValidationResult(
                "SizeLimit is required when Redis is disabled. Set a positive value or enable Redis.",
                [nameof(SizeLimit)]);
        }
        else if (SizeLimit <= 0)
        {
            yield return new ValidationResult(
                "SizeLimit must be greater than 0.",
                [nameof(SizeLimit)]);
        }

        if (ExpirationScanFrequency <= TimeSpan.Zero)
        {
            yield return new ValidationResult(
                "ExpirationScanFrequency must be greater than zero.",
                [nameof(ExpirationScanFrequency)]);
        }

        if (CompactionPercentage is <= 0 or > 1)
        {
            yield return new ValidationResult(
                "CompactionPercentage must be between 0 (exclusive) and 1 (inclusive).",
                [nameof(CompactionPercentage)]);
        }
    }
}
