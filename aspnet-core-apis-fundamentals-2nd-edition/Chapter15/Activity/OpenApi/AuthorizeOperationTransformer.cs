using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WarehouseApi.OpenApi;

// Adds the Bearer requirement only to operations whose endpoint requires authorization
public sealed class AuthorizeOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        // [Authorize] on a controller or action and RequireAuthorization on an endpoint both add IAuthorizeData
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var requiresAuthorization =
            metadata.OfType<IAuthorizeData>().Any() &&
            !metadata.OfType<IAllowAnonymous>().Any();

        if (!requiresAuthorization)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(BearerSecuritySchemeTransformer.SchemeName, context.Document)] = []
        });

        return Task.CompletedTask;
    }
}
