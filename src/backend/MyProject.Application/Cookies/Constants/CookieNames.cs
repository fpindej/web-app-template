namespace MyProject.Application.Cookies.Constants;

/// <summary>
/// Cookie names used for JWT authentication tokens.
/// <para>
/// Uses the <c>__Secure-</c> prefix which requires the <c>Secure</c> attribute.
/// Defined in the Application layer so both WebApi (controllers) and Infrastructure
/// (services, JWT middleware) can reference them without cross-assembly leakage.
/// </para>
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
