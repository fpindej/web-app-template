namespace MyProject.Application.Caching.Constants;

/// <summary>
/// Provides factory methods for generating standardized cache keys.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Returns the cache key for a user's profile data.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>A cache key in the format <c>user:{userId}</c>.</returns>
    public static string User(Guid userId) => $"user:{userId}";
}
