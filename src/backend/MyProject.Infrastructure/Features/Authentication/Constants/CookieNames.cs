namespace MyProject.Infrastructure.Features.Authentication.Constants;

/// <summary>
/// Cookie names used for JWT authentication tokens.
/// Uses the <c>__Secure-</c> prefix which requires the <c>Secure</c> attribute.
/// </summary>
public static class CookieNames
{
    /// <summary>
    /// Cookie name for the JWT access token.
    /// </summary>
    public const string AccessToken = "__Secure-ACCESS-TOKEN";

    /// <summary>
    /// Cookie name for the refresh token.
    /// </summary>
    public const string RefreshToken = "__Secure-REFRESH-TOKEN";
}
