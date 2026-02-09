using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Caching;
using MyProject.Infrastructure.Caching.Options;
using MyProject.Infrastructure.Caching.Services;
using StackExchange.Redis;

namespace MyProject.Infrastructure.Caching.Extensions;

/// <summary>
/// Extension methods for registering caching services (Redis or in-memory fallback).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers distributed caching with Redis (if enabled) or in-memory fallback, and the <see cref="ICacheService"/> implementation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration for reading caching options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CachingOptions>()
            .BindConfiguration(CachingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var cachingOptions = configuration.GetSection(CachingOptions.SectionName).Get<CachingOptions>();

        if (cachingOptions?.Redis.Enabled is true)
        {
            var configurationOptions = BuildConfigurationOptions(cachingOptions.Redis);

            services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = configurationOptions;
                options.InstanceName = cachingOptions.Redis.InstanceName;
            });
        }
        else
        {
            var inMemoryOptions = cachingOptions?.InMemory ?? new CachingOptions.InMemoryOptions();
            services.AddDistributedMemoryCache(options =>
            {
                options.SizeLimit = inMemoryOptions.SizeLimit;
                options.ExpirationScanFrequency = inMemoryOptions.ExpirationScanFrequency;
                options.CompactionPercentage = inMemoryOptions.CompactionPercentage;
            });
        }

        services.AddScoped<ICacheService, CacheService>();
        return services;
    }

    private static ConfigurationOptions BuildConfigurationOptions(CachingOptions.RedisOptions redisOptions)
    {
        // Parse the connection string using StackExchange.Redis built-in parser
        // Supports formats like: "localhost:6379" or "host1:6379,host2:6379" or full connection strings
        var configurationOptions = ConfigurationOptions.Parse(redisOptions.ConnectionString);

        // Override with explicit options from configuration
        configurationOptions.DefaultDatabase = redisOptions.DefaultDatabase;
        configurationOptions.Ssl = redisOptions.UseSsl;
        configurationOptions.AbortOnConnectFail = redisOptions.AbortOnConnectFail;
        configurationOptions.ConnectTimeout = redisOptions.ConnectTimeoutMs;
        configurationOptions.SyncTimeout = redisOptions.SyncTimeoutMs;
        configurationOptions.AsyncTimeout = redisOptions.AsyncTimeoutMs;
        configurationOptions.ConnectRetry = redisOptions.ConnectRetry;
        configurationOptions.KeepAlive = redisOptions.KeepAliveSeconds;

        if (!string.IsNullOrWhiteSpace(redisOptions.Password))
        {
            configurationOptions.Password = redisOptions.Password;
        }

        return configurationOptions;
    }
}
