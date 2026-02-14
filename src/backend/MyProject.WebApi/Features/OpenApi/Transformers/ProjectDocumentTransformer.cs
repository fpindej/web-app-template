using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MyProject.WebApi.Features.OpenApi.Transformers;

/// <summary>
/// Sets the API title, version, description, and security scheme on the generated OpenAPI document.
/// </summary>
/// <remarks>
/// The description documents both Bearer token and cookie-based authentication flows
/// so consumers of the spec understand both available auth methods.
/// </remarks>
internal sealed class ProjectDocumentTransformer : IOpenApiDocumentTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info.Title = "MyProject API";
        document.Info.Version = "v1";
        document.Info.Description = """
                                    API supports dual authentication methods:

                                    ## Bearer Token Authentication (Mobile/API Clients)
                                    1. Call `POST /api/auth/login` with your credentials
                                    2. Extract `accessToken` and `refreshToken` from the response body
                                    3. Include the access token in subsequent requests: `Authorization: Bearer {accessToken}`
                                    4. When the access token expires, call `POST /api/auth/refresh` with the refresh token in the request body

                                    ## Cookie Authentication (Web Clients)
                                    1. Call `POST /api/auth/login?useCookies=true` with your credentials
                                    2. The response sets `__Secure-ACCESS-TOKEN` and `__Secure-REFRESH-TOKEN` as HttpOnly cookies
                                    3. Subsequent requests automatically include these cookies (ensure `withCredentials` is enabled)
                                    4. Token refresh happens automatically via cookies

                                    ### Remember Me
                                    Set `rememberMe: true` in the login request body to persist cookies across browser restarts.
                                    When `false` (default), session cookies are used and expire when the browser closes.

                                    Both methods work simultaneously — tokens are always returned in the response body.
                                    Cookies are only set when `?useCookies=true` is passed.
                                    """;

        // Add security scheme for Bearer token authentication (mobile/API clients)
        document.Components ??= new OpenApiComponents();
        if (document.Components.SecuritySchemes is null)
        {
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>();
        }

        document.Components.SecuritySchemes["bearerAuth"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Bearer token authentication. Obtain tokens from POST /api/auth/login and include as: Authorization: Bearer {token}"
        };

        return Task.CompletedTask;
    }
}
