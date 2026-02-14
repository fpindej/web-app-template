using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using MyProject.WebApi.Options;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Extensions;

/// <summary>
/// Extension methods for registering rate limiting with global and per-endpoint fixed-window policies.
/// </summary>
internal static class RateLimiterExtensions
{
    /// <summary>
    /// Guard flag to emit the anonymous-fallback warning at most once per application lifetime.
    /// A null <c>RemoteIpAddress</c> typically means the <c>ForwardedHeaders</c> middleware is misconfigured
    /// when running behind a reverse proxy.
    /// </summary>
    private static volatile bool _anonymousFallbackWarned;
    /// <summary>
    /// Registers rate limiting services with a global fixed-window limiter and per-endpoint policies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration for reading rate limiting options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingOptions>()
            .BindConfiguration(RateLimitingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(opt =>
        {
            var rateLimitOptions = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
                                      ?? throw new InvalidOperationException("Rate limiting options are not configured properly.");

            ConfigureGlobalLimiter(opt, rateLimitOptions.Global);
            ConfigureOnRejected(opt);

            AddIpPolicy(opt, RateLimitPolicies.Registration, rateLimitOptions.Registration);
            AddIpPolicy(opt, RateLimitPolicies.Auth, rateLimitOptions.Auth);
            AddUserPolicy(opt, RateLimitPolicies.Sensitive, rateLimitOptions.Sensitive);
            AddUserPolicy(opt, RateLimitPolicies.AdminMutations, rateLimitOptions.AdminMutations);
        });

        return services;
    }

    /// <summary>
    /// Configures the global fixed-window rate limiter partitioned by authenticated user or IP address.
    /// </summary>
    private static void ConfigureGlobalLimiter(RateLimiterOptions options,
        RateLimitingOptions.GlobalLimitOptions globalOptions)
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var partitionKey = context.User.Identity?.Name
                               ?? context.Connection.RemoteIpAddress?.ToString();

            if (partitionKey is null)
            {
                WarnAnonymousFallback(context);
                partitionKey = "anonymous";
            }

            return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                _ => CreateFixedWindowOptions(globalOptions));
        });
    }

    /// <summary>
    /// Configures the rejection handler that returns a JSON <see cref="ErrorResponse"/> with retry-after headers.
    /// </summary>
    private static void ConfigureOnRejected(RateLimiterOptions options)
    {
        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            context.HttpContext.Response.ContentType = "application/json";

            var retryAfterSeconds = 60;

            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                var timeProvider = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
                context.HttpContext.Response.Headers["X-RateLimit-Reset"] =
                    timeProvider.GetUtcNow().Add(retryAfter).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
            }

            context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

            var response = new ErrorResponse
            {
                Message = "Rate limit exceeded",
                Details = $"Too many requests. Please try again in {retryAfterSeconds} seconds."
            };

            await context.HttpContext.Response.WriteAsJsonAsync(response, token);
        };
    }

    /// <summary>
    /// Adds a fixed-window rate limit policy partitioned by client IP address.
    /// Suitable for unauthenticated endpoints (login, registration, token refresh).
    /// </summary>
    private static void AddIpPolicy(RateLimiterOptions options, string policyName,
        RateLimitingOptions.FixedWindowPolicyOptions policyOptions)
    {
        options.AddPolicy(policyName, context =>
        {
            var partitionKey = context.Connection.RemoteIpAddress?.ToString();

            if (partitionKey is null)
            {
                WarnAnonymousFallback(context);
                partitionKey = "anonymous";
            }

            return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                _ => CreateFixedWindowOptions(policyOptions));
        });
    }

    /// <summary>
    /// Adds a fixed-window rate limit policy partitioned by authenticated user identity,
    /// falling back to client IP address when the user is not authenticated.
    /// Suitable for authenticated endpoints (admin mutations, sensitive operations).
    /// </summary>
    private static void AddUserPolicy(RateLimiterOptions options, string policyName,
        RateLimitingOptions.FixedWindowPolicyOptions policyOptions)
    {
        options.AddPolicy(policyName, context =>
        {
            var partitionKey = context.User.Identity?.Name
                               ?? context.Connection.RemoteIpAddress?.ToString();

            if (partitionKey is null)
            {
                WarnAnonymousFallback(context);
                partitionKey = "anonymous";
            }

            return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                _ => CreateFixedWindowOptions(policyOptions));
        });
    }

    /// <summary>
    /// Creates <see cref="FixedWindowRateLimiterOptions"/> from a <see cref="RateLimitingOptions.FixedWindowPolicyOptions"/> configuration.
    /// </summary>
    private static FixedWindowRateLimiterOptions CreateFixedWindowOptions(
        RateLimitingOptions.FixedWindowPolicyOptions policyOptions)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = policyOptions.PermitLimit,
            Window = policyOptions.Window,
            QueueProcessingOrder = policyOptions.QueueProcessingOrder,
            QueueLimit = policyOptions.QueueLimit
        };
    }

    /// <summary>
    /// Emits a one-time warning when <c>RemoteIpAddress</c> is null.
    /// All rate-limited requests then share a single "anonymous" bucket, which is
    /// almost always a sign of misconfigured <c>ForwardedHeaders</c> middleware
    /// when running behind a reverse proxy.
    /// </summary>
    private static void WarnAnonymousFallback(HttpContext context)
    {
        if (_anonymousFallbackWarned)
        {
            return;
        }

        _anonymousFallbackWarned = true;

        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(RateLimiterExtensions));

        logger.LogWarning(
            "RemoteIpAddress is null — all unauthenticated requests share a single rate-limit bucket. " +
            "If running behind a reverse proxy, verify the ForwardedHeaders middleware is configured correctly");
    }
}
