using System.ComponentModel.DataAnnotations;

namespace MyProject.Infrastructure.Caching.Options;

/// <summary>
/// Configuration options for Redis distributed cache.
/// Validated only when Enabled is true.
/// </summary>
public sealed class RedisOptions : IValidatableObject
{
    /// <summary>
    /// Gets or sets a value indicating whether Redis caching is enabled.
    /// When false, falls back to in-memory distributed cache.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets or sets the Redis connection string (host:port format).
    /// Required when Enabled is true.
    /// Example: "localhost:6379" or "redis-server.example.com:6380"
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the Redis authentication password.
    /// Required for production environments.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Gets or sets whether SSL/TLS should be used for the Redis connection.
    /// Should be true for production, especially cloud-hosted Redis.
    /// </summary>
    public bool UseSsl { get; init; }

    /// <summary>
    /// Gets or sets the default Redis database number (0-15).
    /// </summary>
    public int DefaultDatabase { get; init; }

    /// <summary>
    /// Gets or sets the instance name prefix for cache keys.
    /// Helps namespace keys when multiple apps share Redis.
    /// </summary>
    public string InstanceName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection timeout in milliseconds.
    /// </summary>
    public int ConnectTimeoutMs { get; init; } = 5000;

    /// <summary>
    /// Gets or sets the synchronous operation timeout in milliseconds.
    /// </summary>
    public int SyncTimeoutMs { get; init; } = 5000;

    /// <summary>
    /// Gets or sets the asynchronous operation timeout in milliseconds.
    /// </summary>
    public int AsyncTimeoutMs { get; init; } = 5000;

    /// <summary>
    /// Gets or sets whether to abort connection if initial connect fails.
    /// Set to false for production to allow retry in background.
    /// </summary>
    public bool AbortOnConnectFail { get; init; } = true;

    /// <summary>
    /// Gets or sets the number of times to retry connection.
    /// </summary>
    public int ConnectRetry { get; init; } = 3;

    /// <summary>
    /// Gets or sets the keepalive interval in seconds.
    /// Sends periodic pings to keep connection alive.
    /// </summary>
    public int KeepAliveSeconds { get; init; } = 60;

    /// <summary>
    /// Validates Redis options. Only called when Redis is enabled.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            yield return new ValidationResult(
                "ConnectionString is required when Redis is enabled.",
                [nameof(ConnectionString)]);
        }

        if (DefaultDatabase is < 0 or > 15)
        {
            yield return new ValidationResult(
                "DefaultDatabase must be between 0 and 15.",
                [nameof(DefaultDatabase)]);
        }

        if (ConnectTimeoutMs <= 0)
        {
            yield return new ValidationResult(
                "ConnectTimeoutMs must be greater than 0.",
                [nameof(ConnectTimeoutMs)]);
        }

        if (SyncTimeoutMs <= 0)
        {
            yield return new ValidationResult(
                "SyncTimeoutMs must be greater than 0.",
                [nameof(SyncTimeoutMs)]);
        }

        if (AsyncTimeoutMs <= 0)
        {
            yield return new ValidationResult(
                "AsyncTimeoutMs must be greater than 0.",
                [nameof(AsyncTimeoutMs)]);
        }

        if (ConnectRetry < 0)
        {
            yield return new ValidationResult(
                "ConnectRetry must be non-negative.",
                [nameof(ConnectRetry)]);
        }

        if (KeepAliveSeconds <= 0)
        {
            yield return new ValidationResult(
                "KeepAliveSeconds must be greater than 0.",
                [nameof(KeepAliveSeconds)]);
        }
    }
}
