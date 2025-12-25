using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Domain;

namespace MyProject.Application.Features.Authentication;

public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> Login(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="input">The registration input.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<Guid>> Register(RegisterInput input);

    /// <summary>
    /// Logs out the current user by clearing cookies and revoking tokens.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Logout();

    /// <summary>
    /// Refreshes the access token using a refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current authenticated user information
    /// </summary>
    /// <returns>A Result containing the user if authenticated, or failure if not</returns>
    Task<Result<UserOutput>> GetCurrentUserAsync();

    /// <summary>
    /// Gets the roles for a specific user
    /// </summary>
    /// <param name="userId">The user ID to get roles for</param>
    /// <returns>A list of role names</returns>
    Task<IList<string>> GetUserRolesAsync(Guid userId);
}
