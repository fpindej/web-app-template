using System.Reflection;

namespace MyProject.Application.Identity.Constants;

/// <summary>
/// Defines the atomic permission claim values used for fine-grained authorization.
/// <para>
/// Permissions are stored as <c>ClaimType = "permission"</c> in <c>AspNetRoleClaims</c>
/// and embedded in the JWT. The <see cref="All"/> collection is discovered via reflection
/// so that adding a new <c>public const string</c> field is sufficient — no manual registration required.
/// </para>
/// <para>
/// SuperAdmin bypasses all permission checks in the authorization handler (implicit all).
/// </para>
/// </summary>
public static class AppPermissions
{
    /// <summary>
    /// The claim type used for permission claims in JWT tokens and role claims.
    /// </summary>
    public const string ClaimType = "permission";

    /// <summary>
    /// User management permissions.
    /// </summary>
    public static class Users
    {
        /// <summary>View user list and details in the admin panel.</summary>
        public const string View = "users.view";

        /// <summary>Lock, unlock, and delete user accounts.</summary>
        public const string Manage = "users.manage";

        /// <summary>Assign and remove roles to/from users.</summary>
        public const string AssignRoles = "users.assign_roles";
    }

    /// <summary>
    /// Role management permissions.
    /// </summary>
    public static class Roles
    {
        /// <summary>View the role list and details.</summary>
        public const string View = "roles.view";

        /// <summary>Create, edit, and delete custom roles and assign permissions to any role.</summary>
        public const string Manage = "roles.manage";
    }

    /// <summary>
    /// All defined permission values, discovered automatically from nested static class constants.
    /// Adding a new permission constant is sufficient — no manual registration required.
    /// </summary>
    public static readonly IReadOnlyList<string> All = DiscoverPermissions()
        .Select(p => p.Value)
        .ToList();

    /// <summary>
    /// All permission definitions grouped by category.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<PermissionDefinition>> ByCategory =
        DiscoverPermissions()
            .GroupBy(p => p.Category)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<PermissionDefinition>)g.ToList());

    private static List<PermissionDefinition> DiscoverPermissions()
    {
        var permissions = new List<PermissionDefinition>();

        foreach (var nestedType in typeof(AppPermissions).GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
        {
            var category = nestedType.Name;
            var fields = nestedType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            foreach (var field in fields)
            {
                var value = (string)field.GetRawConstantValue()!;
                permissions.Add(new PermissionDefinition(value, category));
            }
        }

        return permissions;
    }
}
