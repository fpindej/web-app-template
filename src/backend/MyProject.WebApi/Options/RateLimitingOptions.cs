using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace MyProject.WebApi.Options;

/// <summary>
/// Root rate limiting configuration options.
/// Maps to the "RateLimiting" section in appsettings.json.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Gets or sets the global rate limiter configuration.
    /// Applies a fixed-window limit across all endpoints.
    /// </summary>
    [Required]
    [ValidateObjectMembers]
    public GlobalLimitOptions Global { get; init; } = new();

    /// <summary>
    /// Configuration options for the global fixed-window rate limiter.
    /// </summary>
    public sealed class GlobalLimitOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of requests permitted within the time window.
        /// Defaults to 100. Must be between 1 and 1000.
        /// </summary>
        [Range(1, 1000)]
        public int PermitLimit { get; [UsedImplicitly] init; } = 100;

        /// <summary>
        /// Gets or sets the time window duration for the rate limiter.
        /// Defaults to 1 minute. Requests exceeding PermitLimit within this window are rejected.
        /// </summary>
        public TimeSpan Window { get; [UsedImplicitly] init; } = TimeSpan.FromMinutes(1);
    }
}
