using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MyProject.WebApi.Features.OpenApi.Transformers;

/// <summary>
/// Ensures numeric types (<see cref="int"/>, <see cref="long"/>, <see cref="double"/>,
/// <see cref="float"/>, <see cref="decimal"/>) are not incorrectly represented as strings
/// in the OpenAPI spec.
/// </summary>
/// <remarks>
/// ASP.NET's OAS generation may add a <c>string</c> type flag and a regex pattern to numeric schemas.
/// This transformer strips those artifacts so consumers see clean <c>integer</c>/<c>number</c> types.
/// </remarks>
internal sealed class NumericSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(int) || underlyingType == typeof(long) ||
            underlyingType == typeof(double) || underlyingType == typeof(float) ||
            underlyingType == typeof(decimal))
        {
            if (schema.Type.HasValue && schema.Type.Value.HasFlag(JsonSchemaType.String))
            {
                schema.Type &= ~JsonSchemaType.String;
                schema.Pattern = null;
            }
        }

        return Task.CompletedTask;
    }
}
