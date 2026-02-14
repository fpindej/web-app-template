namespace MyProject.WebApi.Shared;

/// <summary>
/// Rate limit policy names for use with <c>[EnableRateLimiting]</c> attributes.
/// Each constant maps to a policy registered in <see cref="Extensions.RateLimiterExtensions"/>.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// Stricter limit for registration, partitioned by IP address.
    /// </summary>
    public const string Registration = "registration";

    /// <summary>
    /// Stricter limit for login and token refresh, partitioned by IP address.
    /// </summary>
    public const string Auth = "auth";

    /// <summary>
    /// Stricter limit for password changes and account deletion, partitioned by authenticated user.
    /// </summary>
    public const string Sensitive = "sensitive";

    /// <summary>
    /// Stricter limit for state-changing admin and job management operations, partitioned by authenticated user.
    /// </summary>
    public const string AdminMutations = "admin-mutations";
}
