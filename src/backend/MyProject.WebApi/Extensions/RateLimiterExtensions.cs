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
    /// Registers rate limiting services with a global fixed-window limiter partitioned by user or IP.
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

            opt.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var userIdentifier = context.User.Identity?.Name ??
                                     context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(userIdentifier, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.Global.PermitLimit,
                        Window = rateLimitOptions.Global.Window,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queuing - immediate rejection with 429
                    });
            });

            opt.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                context.HttpContext.Response.ContentType = "application/json";

                // Add standard rate limiting headers
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString(CultureInfo.InvariantCulture);
                    var timeProvider = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
                    context.HttpContext.Response.Headers["X-RateLimit-Reset"] = timeProvider.GetUtcNow().Add(retryAfter).ToUnixTimeSeconds().ToString();

                    var response = new ErrorResponse
                    {
                        Message = "Rate limit exceeded",
                        Details = $"Too many requests. Please try again in {retryAfter.TotalMinutes:F1} minutes."
                    };

                    await context.HttpContext.Response.WriteAsJsonAsync(response, token);
                }
            };

            AddRegistrationPolicy(opt, rateLimitOptions.Registration);
        });

        return services;
    }

    /// <summary>
    /// Adds a fixed-window rate limit policy for the registration endpoint, partitioned by IP address.
    /// </summary>
    private static void AddRegistrationPolicy(RateLimiterOptions options,
        RateLimitingOptions.RegistrationLimitOptions registrationOptions)
    {
        options.AddFixedWindowLimiter(RateLimitingOptions.RegistrationLimitOptions.PolicyName, opt =>
        {
            opt.PermitLimit = registrationOptions.PermitLimit;
            opt.Window = registrationOptions.Window;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });
    }
}
