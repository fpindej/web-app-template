using MyProject.WebApi.Features.OpenApi.Transformers;
using Scalar.AspNetCore;

namespace MyProject.WebApi.Features.OpenApi.Extensions;

/// <summary>
/// Extension methods for configuring OpenAPI specification generation and interactive documentation.
/// </summary>
internal static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Registers the OpenAPI v1 specification with all custom document, operation, and schema transformers.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for chaining.</returns>
    public static WebApplicationBuilder AddOpenApiSpecification(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi("v1", opt =>
        {
            opt.AddDocumentTransformer<ProjectDocumentTransformer>();
            opt.AddDocumentTransformer<CleanupDocumentTransformer>();
            opt.AddOperationTransformer<BearerSecurityOperationTransformer>();
            opt.AddOperationTransformer<CamelCaseQueryParameterTransformer>();
            opt.AddSchemaTransformer<EnumSchemaTransformer>();
            opt.AddSchemaTransformer<NumericSchemaTransformer>();
        });

        return builder;
    }

    /// <summary>
    /// Maps the OpenAPI JSON endpoint and the Scalar interactive API reference UI.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static void UseOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(opt =>
        {
            opt.WithTitle("MyProject API");
            opt.WithTheme(ScalarTheme.Mars);
            opt.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            opt.WithOperationTitleSource(OperationTitleSource.Path);
            opt.SortTagsAlphabetically();
            opt.WithSearchHotKey("k");
        });
    }
}
