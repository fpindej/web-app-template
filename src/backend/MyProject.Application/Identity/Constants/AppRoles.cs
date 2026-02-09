namespace MyProject.Application.Identity.Constants;

/// <summary>
/// Defines the application role names used for authorization.
/// <para>
/// All role assignment and lookup should reference these constants instead of inline string literals.
/// ASP.NET Identity normalizes role names to uppercase for comparison, but the canonical
/// <see cref="Name"/> values use PascalCase for display purposes.
/// </para>
/// </summary>
public static class AppRoles
{
    /// <summary>
    /// The default role assigned to all registered users.
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// The administrative role with elevated privileges.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Returns all defined roles. Used by the identity seeding logic to ensure all roles exist.
    /// </summary>
    public static IReadOnlyList<string> All => [User, Admin];
}
