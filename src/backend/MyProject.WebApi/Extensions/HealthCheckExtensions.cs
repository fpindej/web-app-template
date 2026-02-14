using HealthChecks.UI.Client;
using MyProject.Infrastructure.Caching.Options;

namespace MyProject.WebApi.Extensions;

/// <summary>
/// Extension methods for registering and mapping health check endpoints with dependency verification.
/// </summary>
internal static class HealthCheckExtensions
{
    private const string ReadyTag = "ready";

    /// <summary>
    /// Registers health checks for application dependencies (PostgreSQL, optionally Redis, optionally Frontend),
    /// and the HealthChecks UI dashboard with in-memory storage in non-production environments.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration for reading connection strings and caching options.</param>
    /// <param name="environment">The hosting environment, used to conditionally register the HealthChecks UI.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        var healthChecks = services.AddHealthChecks();

        var connectionString = configuration.GetConnectionString("Database")
                               ?? throw new InvalidOperationException(
                                   "ConnectionStrings:Database is required for the health check.");

        healthChecks.AddNpgSql(
            connectionString,
            name: "PostgreSQL",
            timeout: TimeSpan.FromSeconds(3),
            tags: [ReadyTag]);

        var cachingOptions = configuration.GetSection(CachingOptions.SectionName).Get<CachingOptions>();

        if (cachingOptions?.Redis is { Enabled: true } redisOptions)
        {
            var redisConnectionString = BuildRedisConnectionString(redisOptions);

            healthChecks.AddRedis(
                redisConnectionString,
                name: "Redis",
                timeout: TimeSpan.FromSeconds(3),
                tags: [ReadyTag]);
        }

        var frontendUrl = configuration["HealthChecks:FrontendUrl"];

        if (!string.IsNullOrWhiteSpace(frontendUrl))
        {
            healthChecks.AddUrlGroup(
                new Uri(frontendUrl),
                name: "Frontend",
                timeout: TimeSpan.FromSeconds(5));
        }

        if (!environment.IsProduction())
        {
            services
                .AddHealthChecksUI(setup =>
                {
                    setup.SetEvaluationTimeInSeconds(30);
                    setup.MaximumHistoryEntriesPerEndpoint(50);
                    // Absolute URI required — relative URIs resolve via ServerAddressesService
                    // which returns the bind address (0.0.0.0), not a routable target.
                    // Port 8080 matches the Dockerfile EXPOSE directive.
                    setup.AddHealthCheckEndpoint("API", "http://localhost:8080/health");
                })
                .AddInMemoryStorage();
        }

        return services;
    }

    /// <summary>
    /// Maps health check endpoints with rate limiting disabled:
    /// <list type="bullet">
    ///   <item><c>/health</c> — all checks, JSON response (HealthChecks UI format)</item>
    ///   <item><c>/health/ready</c> — readiness checks only (DB + Redis), JSON response</item>
    ///   <item><c>/health/live</c> — no checks, always 200 Healthy, plain text</item>
    /// </list>
    /// In non-production environments, also maps the HealthChecks UI dashboard at <c>/health-ui</c>.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new()
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            })
            .DisableRateLimiting();

        app.MapHealthChecks("/health/ready", new()
            {
                Predicate = check => check.Tags.Contains(ReadyTag),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            })
            .DisableRateLimiting();

        app.MapHealthChecks("/health/live", new()
            {
                Predicate = _ => false
            })
            .DisableRateLimiting();

        if (!app.Environment.IsProduction())
        {
            app.MapHealthChecksUI(options =>
                {
                    options.UIPath = "/health-ui";
                    options.ApiPath = "/health-ui/api";
                })
                .DisableRateLimiting();
        }

        return app;
    }

    /// <summary>
    /// Builds a StackExchange.Redis-compatible connection string from <see cref="CachingOptions.RedisOptions"/>.
    /// </summary>
    private static string BuildRedisConnectionString(CachingOptions.RedisOptions redisOptions)
    {
        var parts = new List<string> { redisOptions.ConnectionString };

        if (!string.IsNullOrWhiteSpace(redisOptions.Password))
        {
            parts.Add($"password={redisOptions.Password}");
        }

        if (redisOptions.UseSsl)
        {
            parts.Add("ssl=true");
        }

        return string.Join(",", parts);
    }
}
