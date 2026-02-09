using System.Text.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MyProject.WebApi.Features.OpenApi.Transformers;

/// <summary>
/// Converts PascalCase query parameter names to camelCase and propagates
/// descriptions from the parameter schema when the parameter itself has none.
/// </summary>
/// <remarks>
/// ASP.NET emits query parameter names using C# property names (PascalCase),
/// but response bodies are already camelCase via JsonSerializerOptions.
/// This transformer ensures consistency across the entire OAS spec.
/// </remarks>
internal sealed class CamelCaseQueryParameterTransformer : IOpenApiOperationTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (operation.Parameters is not { Count: > 0 })
        {
            return Task.CompletedTask;
        }

        foreach (var parameter in operation.Parameters.OfType<OpenApiParameter>())
        {
            if (parameter.In != ParameterLocation.Query)
            {
                continue;
            }

            // Convert PascalCase → camelCase
            if (parameter.Name is not null)
            {
                parameter.Name = JsonNamingPolicy.CamelCase.ConvertName(parameter.Name);
            }

            // Propagate schema description to the parameter level when missing.
            // This fixes inherited base-class properties (e.g. PaginatedRequest.PageNumber)
            // whose XML <summary> tags don't flow through to the OAS parameter description.
            if (string.IsNullOrEmpty(parameter.Description)
                && parameter.Schema is not null
                && !string.IsNullOrEmpty(parameter.Schema.Description))
            {
                parameter.Description = parameter.Schema.Description;
            }
        }

        return Task.CompletedTask;
    }
}
