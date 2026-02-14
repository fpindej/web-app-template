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
            AddRegistrationPolicy(opt, rateLimitOptions.Registration);
            AddAdminMutationsPolicy(opt, rateLimitOptions.AdminMutations);
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
            var userIdentifier = context.User.Identity?.Name ??
                                 context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

            return RateLimitPartition.GetFixedWindowLimiter(userIdentifier,
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

            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString(CultureInfo.InvariantCulture);
                var timeProvider = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
                context.HttpContext.Response.Headers["X-RateLimit-Reset"] = timeProvider.GetUtcNow().Add(retryAfter).ToUnixTimeSeconds().ToString();

                var response = new ErrorResponse
                {
                    Message = "Rate limit exceeded",
                    Details = $"Too many requests. Please try again in {retryAfter.TotalSeconds:F0} seconds."
                };

                await context.HttpContext.Response.WriteAsJsonAsync(response, token);
            }
        };
    }

    /// <summary>
    /// Adds a fixed-window rate limit policy for the registration endpoint, partitioned by IP address.
    /// </summary>
    private static void AddRegistrationPolicy(RateLimiterOptions options,
        RateLimitingOptions.RegistrationLimitOptions registrationOptions)
    {
        options.AddPolicy(RateLimitingOptions.RegistrationLimitOptions.PolicyName,
            context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(ipAddress,
                    _ => CreateFixedWindowOptions(registrationOptions));
            });
    }

    /// <summary>
    /// Adds a fixed-window rate limit policy for state-changing admin and job management endpoints,
    /// partitioned by authenticated user identity.
    /// </summary>
    private static void AddAdminMutationsPolicy(RateLimiterOptions options,
        RateLimitingOptions.AdminMutationsLimitOptions adminMutationsOptions)
    {
        options.AddPolicy(RateLimitingOptions.AdminMutationsLimitOptions.PolicyName,
            context =>
            {
                var userIdentifier = context.User.Identity?.Name ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(userIdentifier,
                    _ => CreateFixedWindowOptions(adminMutationsOptions));
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
}
