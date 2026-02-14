using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MyProject.WebApi.Features.OpenApi.Transformers;

/// <summary>
/// Applies the <c>bearerAuth</c> security requirement to operations whose endpoint
/// is protected by <see cref="AuthorizeAttribute"/>, unless overridden by
/// <see cref="AllowAnonymousAttribute"/>.
/// </summary>
internal sealed class BearerSecurityOperationTransformer : IOpenApiOperationTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        var hasAuthorize = metadata.OfType<AuthorizeAttribute>().Any();
        var hasAllowAnonymous = metadata.OfType<AllowAnonymousAttribute>().Any();

        if (hasAuthorize && !hasAllowAnonymous)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearerAuth", context.Document)] = []
            });
        }

        return Task.CompletedTask;
    }
}
