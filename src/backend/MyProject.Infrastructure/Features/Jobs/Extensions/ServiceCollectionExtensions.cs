using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Features.Jobs;
using MyProject.Infrastructure.Features.Jobs.Options;
using MyProject.Infrastructure.Features.Jobs.Examples;
using MyProject.Infrastructure.Features.Jobs.RecurringJobs;
using MyProject.Infrastructure.Features.Jobs.Services;

namespace MyProject.Infrastructure.Features.Jobs.Extensions;

/// <summary>
/// Extension methods for registering Hangfire job scheduling services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers Hangfire with PostgreSQL storage and all job-related services.
        /// <para>
        /// Uses the same <c>ConnectionStrings:Database</c> connection string as the main database.
        /// Hangfire will automatically create its schema in a <c>hangfire</c> schema on first startup.
        /// </para>
        /// <para>
        /// When <c>JobScheduling:Enabled</c> is <c>false</c>, skips Hangfire registration entirely.
        /// The admin API endpoints will still be reachable but will return empty results.
        /// </para>
        /// </summary>
        /// <param name="configuration">The application configuration for reading connection strings.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddJobScheduling(IConfiguration configuration)
        {
            services.AddOptions<JobSchedulingOptions>()
                .BindConfiguration(JobSchedulingOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            var options = configuration
                .GetSection(JobSchedulingOptions.SectionName)
                .Get<JobSchedulingOptions>() ?? new JobSchedulingOptions();

            if (!options.Enabled)
            {
                return services;
            }

            var connectionString = configuration.GetConnectionString("Database");

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(o =>
                    o.UseNpgsqlConnection(connectionString)));

            services.AddHangfireServer(serverOptions =>
            {
                serverOptions.WorkerCount = options.WorkerCount;
                serverOptions.ServerTimeout = TimeSpan.FromMinutes(5);
                serverOptions.ShutdownTimeout = TimeSpan.FromSeconds(30);
            });

            // Register recurring job definitions — add new jobs here.
            services.AddScoped<ExpiredRefreshTokenCleanupJob>();
            services.AddScoped<IRecurringJobDefinition>(sp =>
                sp.GetRequiredService<ExpiredRefreshTokenCleanupJob>());

            // Register fire-and-forget job classes — Hangfire resolves them from DI when executed.
            // Example: backgroundJobClient.Enqueue<ExampleFireAndForgetJob>(job => job.ExecuteAsync("hello"));
            services.AddScoped<ExampleFireAndForgetJob>();

            services.AddScoped<IJobManagementService, JobManagementService>();

            return services;
        }
    }
}
