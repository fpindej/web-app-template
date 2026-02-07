using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.Login;

/// <summary>
/// Response containing authentication tokens for API clients.
/// Web clients can ignore this response body as tokens are also set in HttpOnly cookies.
/// </summary>
public class AuthenticationResponse
{
    /// <summary>
    /// The JWT access token for Bearer authentication.
    /// Include this in the Authorization header as "Bearer {accessToken}" for subsequent API requests.
    /// </summary>
    public string AccessToken { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The refresh token for obtaining new access tokens.
    /// Use this with the /api/auth/refresh endpoint when the access token expires.
    /// </summary>
    public string RefreshToken { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;
}
