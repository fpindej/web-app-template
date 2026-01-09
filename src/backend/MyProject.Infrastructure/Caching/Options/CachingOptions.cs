using System.ComponentModel.DataAnnotations;

namespace MyProject.Infrastructure.Caching.Options;

/// <summary>
/// Root caching configuration options.
/// Contains both Redis and InMemory cache configurations.
/// </summary>
public sealed class CachingOptions : IValidatableObject
{
    public const string SectionName = "Caching";

    /// <summary>
    /// Gets or sets the default cache entry expiration.
    /// Used when no explicit expiration is provided.
    /// Applies to both Redis and InMemory caching.
    /// </summary>
    public TimeSpan DefaultExpiration { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the Redis cache configuration.
    /// When Redis.Enabled is true, Redis will be used as the distributed cache.
    /// </summary>
    public RedisOptions Redis { get; init; } = new();

    /// <summary>
    /// Gets or sets the in-memory cache configuration.
    /// Used as fallback when Redis is disabled.
    /// </summary>
    public InMemoryOptions InMemory { get; init; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DefaultExpiration <= TimeSpan.Zero)
        {
            yield return new ValidationResult(
                "DefaultExpiration must be greater than zero.",
                [nameof(DefaultExpiration)]);
        }

        if (Redis.Enabled)
        {
            // Validate Redis options when enabled
            foreach (var result in Redis.Validate(validationContext))
            {
                yield return result;
            }
        }
        else
        {
            // Validate InMemory options when Redis is disabled
            foreach (var result in InMemory.Validate(validationContext))
            {
                yield return result;
            }
        }
    }
}
