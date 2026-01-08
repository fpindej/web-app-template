using System.ComponentModel.DataAnnotations;

namespace MyProject.Infrastructure.Caching.Options;

public sealed class RedisOptions : IValidatableObject
{
    public const string SectionName = "Redis";

    /// <summary>
    /// Gets or sets a value indicating whether Redis caching is enabled.
    /// When false, falls back to in-memory distributed cache.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets or sets the Redis connection string.
    /// Required when Enabled is true.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the instance name prefix for cache keys.
    /// </summary>
    public string InstanceName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the size limit for in-memory cache (when Redis is disabled).
    /// Set to null for no limit (not recommended for production).
    /// </summary>
    public int? InMemorySizeLimit { get; init; } = 1024;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Enabled && string.IsNullOrWhiteSpace(ConnectionString))
        {
            yield return new ValidationResult(
                "ConnectionString is required when Redis is enabled.",
                [nameof(ConnectionString)]);
        }
    }
}
