namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Output containing authentication tokens.
/// </summary>
/// <param name="AccessToken">The JWT access token for API authentication.</param>
/// <param name="RefreshToken">The refresh token for obtaining new access tokens.</param>
public record AuthenticationOutput(
    string AccessToken,
    string RefreshToken
);
