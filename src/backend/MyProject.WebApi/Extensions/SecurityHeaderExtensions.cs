namespace MyProject.WebApi.Extensions;

/// <summary>
/// Extension methods for registering security response headers middleware.
/// </summary>
/// <remarks>
/// These headers are browser-instructional — they tell browsers how to behave when
/// handling responses. They have zero impact on non-browser clients (mobile apps, curl, etc.).
/// The frontend applies the same headers to page responses via the SvelteKit handle hook.
/// API proxy routes are skipped on the frontend side since they receive these headers from the backend.
/// </remarks>
internal static class SecurityHeaderExtensions
{
    /// <summary>
    /// Adds middleware that sets security response headers on every API response.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Header</term>
    ///     <description>Purpose</description>
    ///   </listheader>
    ///   <item>
    ///     <term>X-Content-Type-Options: nosniff</term>
    ///     <description>Prevents MIME-type sniffing (XSS via content type confusion).</description>
    ///   </item>
    ///   <item>
    ///     <term>X-Frame-Options: DENY</term>
    ///     <description>Prevents embedding in iframes (clickjacking).</description>
    ///   </item>
    ///   <item>
    ///     <term>Referrer-Policy: strict-origin-when-cross-origin</term>
    ///     <description>Prevents leaking URL paths to third-party sites.</description>
    ///   </item>
    ///   <item>
    ///     <term>Permissions-Policy: camera=(), microphone=(), geolocation=()</term>
    ///     <description>
    ///       Disables browser APIs the app doesn't use. The empty allowlist <c>()</c> denies
    ///       access entirely. To allow a specific API for the app's own origin, change the
    ///       directive to <c>(self)</c> — never remove the header or use <c>*</c>.
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    ///   HSTS is not included here — it is environment-gated and applied separately
    ///   via <c>app.UseHsts()</c> in non-development environments.
    /// </para>
    /// </remarks>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            await next();
        });

        return app;
    }
}
