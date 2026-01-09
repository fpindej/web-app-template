namespace MyProject.Application.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache entry options that can be used without depending on infrastructure packages.
/// </summary>
public sealed class CacheEntryOptions
{
    /// <summary>
    /// Gets or sets an absolute expiration date for the cache entry.
    /// </summary>
    public DateTimeOffset? AbsoluteExpiration { get; init; }

    /// <summary>
    /// Gets or sets an absolute expiration time, relative to now.
    /// </summary>
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; init; }

    /// <summary>
    /// Gets or sets how long a cache entry can be inactive (e.g. not accessed) before it will be removed.
    /// This will not extend the entry lifetime beyond the absolute expiration (if set).
    /// </summary>
    public TimeSpan? SlidingExpiration { get; init; }

    /// <summary>
    /// Creates options with absolute expiration relative to now.
    /// </summary>
    public static CacheEntryOptions AbsoluteExpireIn(TimeSpan duration) => new()
    {
        AbsoluteExpirationRelativeToNow = duration
    };

    /// <summary>
    /// Creates options with sliding expiration.
    /// </summary>
    public static CacheEntryOptions SlidingExpireIn(TimeSpan duration) => new()
    {
        SlidingExpiration = duration
    };
}
