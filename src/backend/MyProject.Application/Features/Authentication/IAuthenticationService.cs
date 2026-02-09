using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Domain;

namespace MyProject.Application.Features.Authentication;

/// <summary>
/// Provides authentication operations including login, registration, logout, token refresh, and password management.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="useCookies">Whether to set authentication cookies. Defaults to false (stateless). Set to true for web clients.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing authentication tokens on success.</returns>
    Task<Result<AuthenticationOutput>> Login(string username, string password, bool useCookies = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="input">The registration input.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<Guid>> Register(RegisterInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out the current user by clearing cookies and revoking tokens.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Logout(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the access token using a refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="useCookies">Whether to set authentication cookies. Defaults to false (stateless). Set to true for web clients.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing new authentication tokens on success.</returns>
    Task<Result<AuthenticationOutput>> RefreshTokenAsync(string refreshToken, bool useCookies = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the current user's password after verifying the current password.
    /// Revokes all existing refresh tokens to force re-authentication on other devices.
    /// </summary>
    /// <param name="input">The change password input containing current and new passwords.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ChangePasswordAsync(ChangePasswordInput input, CancellationToken cancellationToken = default);
}
