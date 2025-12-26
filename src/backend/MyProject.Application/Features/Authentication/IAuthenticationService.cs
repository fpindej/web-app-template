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
}
