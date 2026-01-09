using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MyProject.Application.Caching;
using MyProject.Infrastructure.Caching.Options;

namespace MyProject.Infrastructure.Caching.Services;

internal class CacheService(
    IDistributedCache distributedCache,
    IOptions<CachingOptions> cachingOptions) : ICacheService
{
    private readonly TimeSpan _defaultExpiration = cachingOptions.Value.DefaultExpiration;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var cachedValue = await distributedCache.GetStringAsync(key, cancellationToken);

        return string.IsNullOrEmpty(cachedValue) ? default : JsonSerializer.Deserialize<T>(cachedValue);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var serializedValue = JsonSerializer.Serialize(value);
        await distributedCache.SetStringAsync(key, serializedValue, ToDistributedOptions(options), cancellationToken);
    }

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);
        if (value is not null)
        {
            await SetAsync(key, value, options, cancellationToken);
        }

        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await distributedCache.RemoveAsync(key, cancellationToken);
    }

    private DistributedCacheEntryOptions ToDistributedOptions(CacheEntryOptions? options)
    {
        if (options is null)
        {
            return new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _defaultExpiration
            };
        }

        return new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = options.AbsoluteExpiration,
            AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
            SlidingExpiration = options.SlidingExpiration
        };
    }
}
