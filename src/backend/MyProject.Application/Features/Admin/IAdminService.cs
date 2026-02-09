using MyProject.Application.Features.Admin.Dtos;
using MyProject.Domain;

namespace MyProject.Application.Features.Admin;

/// <summary>
/// Provides administrative operations for managing users and roles.
/// All operations require the caller to have the Admin role.
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
    /// <returns>The user details, or failure if not found.</returns>
    Task<Result<AdminUserOutput>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="input">The role assignment input.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> AssignRoleAsync(Guid userId, AssignRoleInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="role">The role name to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> RemoveRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks a user account, preventing login.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> LockUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks a user account, allowing login.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a user account.
    /// Identity does not natively support soft delete, so this permanently deletes the user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles with user counts.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of roles with the number of users in each.</returns>
    Task<IReadOnlyList<AdminRoleOutput>> GetRolesAsync(CancellationToken cancellationToken = default);
}
