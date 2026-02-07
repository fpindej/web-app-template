namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Output containing authentication tokens.
/// </summary>
/// <param name="AccessToken">The JWT access token for API authentication.</param>
/// <param name="RefreshToken">The refresh token for obtaining new access tokens.</param>
/// <param name="AccessTokenExpiresInSeconds">The access token expiration time in seconds.</param>
/// <param name="RefreshTokenExpiresInSeconds">The refresh token expiration time in seconds.</param>
public record AuthenticationOutput(
    string AccessToken,
    string RefreshToken,
    int AccessTokenExpiresInSeconds,
    int RefreshTokenExpiresInSeconds
);
