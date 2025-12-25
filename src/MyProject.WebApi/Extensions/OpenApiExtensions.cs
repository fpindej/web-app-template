using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MyProject.WebApi.Extensions;

internal static class OpenApiExtensions
{
    public static IServiceCollection AddApiDefinition(this IServiceCollection services)
    {
        services.AddOpenApi("v1", opt =>
        {
            opt.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "MyProject API";
                document.Info.Version = "v1";
                return Task.CompletedTask;
            });

            opt.AddDocumentTransformer<CookieAuthDocumentTransformer>();
        });

        return services;
    }
}

internal sealed class CookieAuthDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info.Description = """
                                    API uses cookie-based authentication.

                                    To authenticate:
                                    1. Call `POST /api/auth/login` with your credentials
                                    2. The response will set HttpOnly cookies containing the access and refresh tokens
                                    3. Subsequent requests will automatically include these cookies

                                    **Note:** Make sure "withCredentials" is enabled in your HTTP client to send cookies with requests.
                                    """;

        return Task.CompletedTask;
    }
}
