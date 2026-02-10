namespace MyProject.Domain;

/// <summary>
/// User-facing error messages organized by domain area.
/// Constants are used in <c>Result.Failure()</c> calls so that messages remain consistent,
/// greppable, and easy to extract into translation keys later.
/// <para>
/// For dynamic messages (containing runtime values like role names), use inline string
/// interpolation in the service — only truly static messages belong here.
/// </para>
/// </summary>
public static class ErrorMessages
{
    /// <summary>
    /// Authentication error messages.
    /// </summary>
    public static class Auth
    {
        public const string LoginInvalidCredentials = "Invalid username or password.";
        public const string LoginAccountLocked = "Account is temporarily locked due to multiple failed login attempts. Please try again later.";
        public const string RegisterRoleAssignFailed = "Account was created but role assignment failed. Please contact an administrator.";
        public const string TokenMissing = "Refresh token is missing.";
        public const string TokenNotFound = "Refresh token not found.";
        public const string TokenInvalidated = "Refresh token has been invalidated.";
        public const string TokenReused = "Invalid refresh token.";
        public const string TokenExpired = "Refresh token has expired.";
        public const string TokenUserNotFound = "User not found.";
        public const string NotAuthenticated = "User is not authenticated.";
        public const string UserNotFound = "User not found.";
        public const string PasswordIncorrect = "Current password is incorrect.";
    }

    /// <summary>
    /// User self-service error messages.
    /// </summary>
    public static class User
    {
        public const string NotAuthenticated = "User is not authenticated.";
        public const string NotFound = "User not found.";
        public const string DeleteInvalidPassword = "Invalid password.";
    }

    /// <summary>
    /// Administrative operation error messages.
    /// </summary>
    public static class Admin
    {
        public const string HierarchyInsufficient = "You do not have sufficient privileges to manage this user.";
        public const string RoleAssignAboveRank = "Cannot assign a role at or above your own rank.";
        public const string RoleRemoveAboveRank = "Cannot remove a role at or above your own rank.";
        public const string RoleSelfRemove = "Cannot remove a role from your own account.";
        public const string LockSelfAction = "Cannot lock your own account.";
        public const string DeleteSelfAction = "Cannot delete your own account.";
    }

    /// <summary>
    /// Pagination error messages.
    /// </summary>
    public static class Pagination
    {
        public const string InvalidPage = "Page number must be positive.";
        public const string InvalidPageSize = "Page size must be positive.";
    }

    /// <summary>
    /// Server-level error messages.
    /// </summary>
    public static class Server
    {
        public const string InternalError = "An internal error occurred.";
    }

    /// <summary>
    /// Generic entity operation error messages (repository layer).
    /// </summary>
    public static class Entity
    {
        public const string AddFailed = "Failed to add entity.";
        public const string NotFound = "Entity not found.";
        public const string NotDeleted = "Entity could not be deleted.";
    }
}
