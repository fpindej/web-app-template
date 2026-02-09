using MyProject.Application.Features.Admin.Dtos;
using MyProject.Domain;

namespace MyProject.Application.Features.Admin;

/// <summary>
/// Provides administrative operations for managing users and roles.
/// <para>
/// All mutation operations require a <c>callerUserId</c> parameter to enforce role hierarchy
/// and self-action protection at the service layer. The caller must have a strictly higher
/// role rank than the target user (see <see cref="MyProject.Application.Identity.Constants.AppRoles"/>).
/// </para>
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Gets a paginated list of all users, optionally filtered by a search term.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="search">Optional search term to filter by name or email.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A paginated list of users with admin-level details.</returns>
    Task<AdminUserListOutput> GetUsersAsync(int pageNumber, int pageSize, string? search = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single user by ID with full admin-level details.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The user details.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    Task<AdminUserOutput> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user. The caller must have a strictly higher role rank than the target user,
    /// and can only assign roles below their own rank.
    /// </summary>
    /// <param name="callerUserId">The ID of the admin performing the action.</param>
    /// <param name="userId">The target user ID.</param>
    /// <param name="input">The role assignment input.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> AssignRoleAsync(Guid callerUserId, Guid userId, AssignRoleInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user. The caller must have a strictly higher role rank than the target user,
    /// and cannot remove roles at or above their own rank.
    /// </summary>
    /// <param name="callerUserId">The ID of the admin performing the action.</param>
    /// <param name="userId">The target user ID.</param>
    /// <param name="role">The role name to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> RemoveRoleAsync(Guid callerUserId, Guid userId, string role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks a user account, preventing login. The caller must have a strictly higher role rank
    /// than the target user and cannot lock themselves.
    /// </summary>
    /// <param name="callerUserId">The ID of the admin performing the action.</param>
    /// <param name="userId">The target user ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> LockUserAsync(Guid callerUserId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks a user account, allowing login. The caller must have a strictly higher role rank
    /// than the target user.
    /// </summary>
    /// <param name="callerUserId">The ID of the admin performing the action.</param>
    /// <param name="userId">The target user ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> UnlockUserAsync(Guid callerUserId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a user account. The caller must have a strictly higher role rank
    /// than the target user and cannot delete themselves.
    /// </summary>
    /// <param name="callerUserId">The ID of the admin performing the action.</param>
    /// <param name="userId">The target user ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> DeleteUserAsync(Guid callerUserId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles with user counts.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of roles with the number of users in each.</returns>
    Task<IReadOnlyList<AdminRoleOutput>> GetRolesAsync(CancellationToken cancellationToken = default);
}
