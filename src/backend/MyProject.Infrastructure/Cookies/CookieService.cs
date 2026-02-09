using Microsoft.AspNetCore.Http;
using MyProject.Application.Cookies;

namespace MyProject.Infrastructure.Cookies;

/// <summary>
/// Cookie management service.
/// <para>
/// All cookies use <c>SameSite=None</c> + <c>Secure=true</c> because this template is designed
/// for cross-subdomain deployments where the API and frontend are hosted on different origins
/// (e.g. <c>api.example.com</c> and <c>app.example.com</c>). Without <c>SameSite=None</c>,
/// browsers would block cookie transmission on cross-origin requests, breaking authentication.
/// <c>Secure=true</c> is mandatory when <c>SameSite=None</c> is set.
/// </para>
/// </summary>
internal class CookieService(IHttpContextAccessor httpContextAccessor) : ICookieService
{
    /// <inheritdoc />
    public void SetCookie(string key, string value, DateTimeOffset? expires = null)
    {
        var options = new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expires
        };

        httpContextAccessor.HttpContext?.Response.Cookies.Append(key, value, options);
    }

    /// <inheritdoc />
    public void SetSecureCookie(string key, string value, DateTimeOffset? expires = null)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expires
        };

        httpContextAccessor.HttpContext?.Response.Cookies.Append(key, value, options);
    }

    /// <inheritdoc />
    public void DeleteCookie(string key)
    {
        httpContextAccessor.HttpContext?.Response.Cookies.Delete(key, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });
    }

    /// <inheritdoc />
    public string? GetCookie(string key)
    {
        return httpContextAccessor.HttpContext?.Request.Cookies[key];
    }
}
