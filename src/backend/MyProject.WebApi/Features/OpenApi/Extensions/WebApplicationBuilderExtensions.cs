using MyProject.WebApi.Features.OpenApi.Transformers;
using Scalar.AspNetCore;

namespace MyProject.WebApi.Features.OpenApi.Extensions;

internal static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddOpenApiSpecification(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi("v1", opt =>
        {
            opt.AddDocumentTransformer<ProjectDocumentTransformer>();
            opt.AddDocumentTransformer<CookieAuthDocumentTransformer>();
            opt.AddSchemaTransformer<EnumSchemaTransformer>();
            opt.AddSchemaTransformer<NumericSchemaTransformer>(); });

        return builder;
    }

    public static void UseOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(opt =>
        {
            opt.WithTitle("MyProject API");
            opt.WithTheme(ScalarTheme.Mars);
            opt.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });
    }
}
