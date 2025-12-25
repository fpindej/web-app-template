using System.Globalization;
using System.Threading.RateLimiting;
using MyProject.WebApi.Options;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Extensions;

internal static class RateLimiterExtensions
{
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
                    context.HttpContext.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.Add(retryAfter).ToUnixTimeSeconds().ToString();

                    var response = new ErrorResponse
                    {
                        Message = "Rate limit exceeded",
                        Details = $"Too many requests. Please try again in {retryAfter.TotalMinutes:F1} minutes."
                    };

                    await context.HttpContext.Response.WriteAsJsonAsync(response, token);
                }
            };
        });

        return services;
    }
}
