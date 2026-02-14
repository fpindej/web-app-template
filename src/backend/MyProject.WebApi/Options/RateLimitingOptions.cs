using System.ComponentModel.DataAnnotations;
using System.Threading.RateLimiting;
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
    /// Applies a fixed-window limit across all endpoints, partitioned by authenticated user or IP address.
    /// </summary>
    [Required]
    [ValidateObjectMembers]
    public GlobalLimitOptions Global { get; init; } = new();

    /// <summary>
    /// Gets or sets the registration endpoint rate limiter configuration.
    /// Applies a stricter fixed-window limit to prevent automated account creation, partitioned by IP address.
    /// </summary>
    [Required]
    [ValidateObjectMembers]
    public RegistrationLimitOptions Registration { get; init; } = new();

    /// <summary>
    /// Gets or sets the admin mutations rate limiter configuration.
    /// Applies a stricter fixed-window limit to state-changing admin and job management operations,
    /// partitioned by authenticated user.
    /// </summary>
    [Required]
    [ValidateObjectMembers]
    public AdminMutationsLimitOptions AdminMutations { get; init; } = new();

    /// <summary>
    /// Base configuration for a fixed-window rate limit policy.
    /// Provides shared properties for permit limit, time window, queue behavior, and processing order.
    /// </summary>
    public abstract class FixedWindowPolicyOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of requests permitted within the time window.
        /// Requests beyond this limit are either queued (up to <see cref="QueueLimit"/>) or immediately rejected with 429.
        /// </summary>
        [Range(1, 10000)]
        public int PermitLimit { get; [UsedImplicitly] init; }

        /// <summary>
        /// Gets or sets the time window duration for the rate limiter.
        /// Requests exceeding <see cref="PermitLimit"/> within this window are rejected or queued.
        /// </summary>
        public TimeSpan Window { get; [UsedImplicitly] init; }

        /// <summary>
        /// Gets or sets the maximum number of requests to queue when the permit limit is exhausted.
        /// Defaults to 0 (no queuing — immediate rejection with 429).
        /// </summary>
        [Range(0, 1000)]
        public int QueueLimit { get; [UsedImplicitly] init; }

        /// <summary>
        /// Gets or sets the order in which queued requests are processed when permits become available.
        /// Defaults to <see cref="QueueProcessingOrder.OldestFirst"/>.
        /// </summary>
        public QueueProcessingOrder QueueProcessingOrder { get; [UsedImplicitly] init; } = QueueProcessingOrder.OldestFirst;
    }

    /// <summary>
    /// Configuration options for the global fixed-window rate limiter.
    /// Applied to all endpoints before any per-endpoint policies.
    /// </summary>
    public sealed class GlobalLimitOptions : FixedWindowPolicyOptions
    {
        /// <summary>
        /// Initializes default values for the global rate limiter.
        /// Defaults to 100 requests per 1 minute with no queuing.
        /// </summary>
        public GlobalLimitOptions()
        {
            PermitLimit = 100;
            Window = TimeSpan.FromMinutes(1);
            QueueLimit = 0;
        }
    }

    /// <summary>
    /// Configuration options for the registration endpoint fixed-window rate limiter.
    /// </summary>
    public sealed class RegistrationLimitOptions : FixedWindowPolicyOptions
    {
        /// <summary>
        /// The policy name used to reference this limiter in <c>[EnableRateLimiting]</c> attributes.
        /// </summary>
        public const string PolicyName = "registration";

        /// <summary>
        /// Initializes default values for the registration rate limiter.
        /// Defaults to 5 requests per 1 minute with no queuing.
        /// </summary>
        public RegistrationLimitOptions()
        {
            PermitLimit = 5;
            Window = TimeSpan.FromMinutes(1);
            QueueLimit = 0;
        }
    }

    /// <summary>
    /// Configuration options for the admin mutations fixed-window rate limiter.
    /// Applied to state-changing admin and job management endpoints, partitioned by authenticated user.
    /// </summary>
    public sealed class AdminMutationsLimitOptions : FixedWindowPolicyOptions
    {
        /// <summary>
        /// The policy name used to reference this limiter in <c>[EnableRateLimiting]</c> attributes.
        /// </summary>
        public const string PolicyName = "admin-mutations";

        /// <summary>
        /// Initializes default values for the admin mutations rate limiter.
        /// Defaults to 30 requests per 1 minute with no queuing.
        /// </summary>
        public AdminMutationsLimitOptions()
        {
            PermitLimit = 30;
            Window = TimeSpan.FromMinutes(1);
            QueueLimit = 0;
        }
    }
}
