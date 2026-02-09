namespace MyProject.Infrastructure.Features.Authentication.Constants;

/// <summary>
/// Default user credentials for development seeding.
/// These are only used by <see cref="Persistence.Extensions.ApplicationBuilderExtensions.InitializeDatabaseAsync"/>
/// and should never appear in production.
/// </summary>
internal static class SeedUsers
{
    /// <summary>
    /// Email address for the default test user.
    /// </summary>
    public const string TestUserEmail = "testuser@test.com";

    /// <summary>
    /// Password for the default test user.
    /// </summary>
    public const string TestUserPassword = "TestUser123!";

    /// <summary>
    /// Email address for the default admin user.
    /// </summary>
    public const string AdminEmail = "admin@test.com";

    /// <summary>
    /// Password for the default admin user.
    /// </summary>
    public const string AdminPassword = "AdminUser123!";
}
