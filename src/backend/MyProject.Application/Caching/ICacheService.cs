namespace MyProject.Application.Caching;

/// <summary>
/// Provides an abstraction over distributed caching with JSON serialization.
/// </summary>
/// <remarks>Pattern documented in src/backend/AGENTS.md — update both when changing.</remarks>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a cached value by key.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the cached value into.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The cached value if found; otherwise <c>null</c>.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in the cache with optional expiration.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="options">Optional cache entry expiration options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a cached value by key, or creates and caches it using the factory if not found.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the cached value into.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">A factory function to produce the value if the key is not in the cache.</param>
    /// <param name="options">Optional cache entry expiration options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The cached or newly created value.</returns>
    Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
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
