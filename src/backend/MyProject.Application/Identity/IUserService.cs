using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Domain;

namespace MyProject.Application.Identity;

public interface IUserService
{
    /// <summary>
    /// Gets the current authenticated user information.
    /// </summary>
    /// <returns>A Result containing the user if authenticated, or failure if not.</returns>
    Task<Result<UserOutput>> GetCurrentUserAsync();

    /// <summary>
    /// Gets the roles for a specific user.
    /// </summary>
    /// <param name="userId">The user ID to get roles for.</param>
    /// <returns>A list of role names.</returns>
    Task<IList<string>> GetUserRolesAsync(Guid userId);
}
