namespace MyProject.Application.Cookies;

/// <summary>
/// Provides an abstraction for reading and writing HTTP cookies on the current response/request.
/// </summary>
public interface ICookieService
{
    /// <summary>
    /// Sets a cookie with the given key and value.
    /// </summary>
    /// <param name="key">The cookie name.</param>
    /// <param name="value">The cookie value.</param>
    /// <param name="expires">Optional absolute expiration date.</param>
    void SetCookie(string key, string value, DateTimeOffset? expires = null);

    /// <summary>
    /// Sets a secure, HttpOnly cookie with <c>SameSite=None</c> for cross-origin requests.
    /// </summary>
    /// <param name="key">The cookie name.</param>
    /// <param name="value">The cookie value.</param>
    /// <param name="expires">Optional absolute expiration date.</param>
    void SetSecureCookie(string key, string value, DateTimeOffset? expires = null);

    /// <summary>
    /// Deletes a cookie by setting its expiration in the past.
    /// </summary>
    /// <param name="key">The cookie name to delete.</param>
    void DeleteCookie(string key);

    /// <summary>
    /// Reads a cookie value from the current request.
    /// </summary>
    /// <param name="key">The cookie name.</param>
    /// <returns>The cookie value if present; otherwise <c>null</c>.</returns>
    string? GetCookie(string key);
}
