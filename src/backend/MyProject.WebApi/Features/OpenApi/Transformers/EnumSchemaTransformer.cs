using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MyProject.WebApi.Features.OpenApi.Transformers;

/// <summary>
/// Ensures all enum types appear as string enums in the OpenAPI spec with every member listed.
/// Handles both non-nullable (<c>MyEnum</c>) and nullable (<c>MyEnum?</c>) enum properties.
/// </summary>
/// <remarks>See AGENTS.md → Enum Handling for conventions.</remarks>
internal sealed class EnumSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        var enumType = Nullable.GetUnderlyingType(type) ?? type;

        if (!enumType.IsEnum)
        {
            return Task.CompletedTask;
        }

        // Preserve the null flag for nullable enums (MyEnum?) → type: [string, null]
        var isNullable = schema.Type.HasValue && schema.Type.Value.HasFlag(JsonSchemaType.Null);
        schema.Type = isNullable
            ? JsonSchemaType.String | JsonSchemaType.Null
            : JsonSchemaType.String;
        schema.Format = null;

        // List every enum member as a string value in the schema
        schema.Enum = Enum.GetNames(enumType)
            .Select(name => (JsonNode)JsonValue.Create(name))
            .ToList();

        return Task.CompletedTask;
    }
}
