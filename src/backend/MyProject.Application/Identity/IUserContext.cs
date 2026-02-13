namespace MyProject.Application.Identity;

/// <summary>
/// Provides access to the current authenticated user's identity claims.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the current user's unique identifier, or <c>null</c> if not authenticated.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current user's email address, or <c>null</c> if not authenticated.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the current user's username, or <c>null</c> if not authenticated.
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Gets the current user's unique identifier.
    /// Throws <see cref="InvalidOperationException"/> if the request is not authenticated.
    /// Use this on endpoints that are guaranteed to be authenticated (e.g. behind <c>[Authorize]</c>
    /// or <c>[RequirePermission]</c>).
    /// </summary>
    Guid AuthenticatedUserId { get; }

    /// <summary>
    /// Gets a value indicating whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Determines whether the current user belongs to the specified role.
    /// </summary>
    /// <param name="role">The role name to check.</param>
    /// <returns><c>true</c> if the user is in the role; otherwise <c>false</c>.</returns>
    bool IsInRole(string role);
}
