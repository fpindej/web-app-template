using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MyProject.WebApi.Features.OpenApi.Transformers;

/// <summary>
/// Post-processes the generated OpenAPI document to fix structural issues:
/// <list type="bullet">
///   <item>Removes redundant content types (text/plain, text/json, application/*+json) — only application/json is kept.</item>
///   <item>Strips response bodies from HEAD operations (RFC 9110 §9.3.2).</item>
/// </list>
/// </summary>
/// <remarks>
/// Registered as a document transformer so it runs once after the full spec is assembled.
/// </remarks>
internal sealed class CleanupDocumentTransformer : IOpenApiDocumentTransformer
{
    /// <summary>
    /// Content types to remove from both request bodies and responses.
    /// Only <c>application/json</c> (and <c>multipart/form-data</c> for file uploads) is kept.
    /// </summary>
    private static readonly string[] RedundantContentTypes =
        ["text/plain", "text/json", "application/*+json"];

    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var (_, pathItem) in document.Paths ?? [])
        {
            foreach (var (method, operation) in pathItem.Operations ?? [])
            {
                CleanRequestBody(operation);
                CleanResponses(operation, method);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes redundant content types from the request body.
    /// </summary>
    private static void CleanRequestBody(OpenApiOperation operation)
    {
        if (operation.RequestBody?.Content is null)
        {
            return;
        }

        foreach (var contentType in RedundantContentTypes)
        {
            operation.RequestBody.Content.Remove(contentType);
        }
    }

    /// <summary>
    /// Removes redundant content types from all responses.
    /// For HEAD operations, also removes ALL response content (RFC 9110 §9.3.2).
    /// </summary>
    private static void CleanResponses(OpenApiOperation operation, HttpMethod method)
    {
        if (operation.Responses is null)
        {
            return;
        }

        foreach (var (_, response) in operation.Responses)
        {
            if (response.Content is null)
            {
                continue;
            }

            if (method == HttpMethod.Head)
            {
                response.Content.Clear();
                continue;
            }

            foreach (var contentType in RedundantContentTypes)
            {
                response.Content.Remove(contentType);
            }
        }
    }
}
