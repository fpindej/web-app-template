using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Domain;

namespace MyProject.Application.Identity;

/// <summary>
/// Provides operations for retrieving and updating user profile information.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets the current authenticated user information.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A Result containing the user if authenticated, or failure if not.</returns>
    Task<Result<UserOutput>> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the current user's profile information.
    /// </summary>
    /// <param name="input">The profile update input.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A Result containing the updated user if successful, or failure if not.</returns>
    Task<Result<UserOutput>> UpdateProfileAsync(UpdateProfileInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the roles for a specific user.
    /// </summary>
    /// <param name="userId">The user ID to get roles for.</param>
    /// <returns>A list of role names.</returns>
    Task<IList<string>> GetUserRolesAsync(Guid userId);
}
