using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;

namespace WarehouseApi.OpenApi;

public static class VersionedOpenApiExtensions
{
    // Shared setup for each per-version document registered with AddOpenApi
    public static OpenApiOptions UseApiVersionDocument(this OpenApiOptions options)
    {
        // AddOpenApi("v1", ...) has already set DocumentName to "v1". Include only the API explorer
        // group with the same name; the default rule would also add every endpoint without a group
        options.ShouldInclude = description =>
            string.Equals(description.GroupName, options.DocumentName, StringComparison.OrdinalIgnoreCase);

        options.AddDocumentTransformer<ApiVersionInfoTransformer>();
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        options.AddOperationTransformer<AuthorizeOperationTransformer>();
        options.AddOperationTransformer<IdempotencyKeyOperationTransformer>();

        // Asp.Versioning marks the API descriptions of a deprecated version; the generator
        // does not read that flag, so copy it to each operation
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            if (context.Description.IsDeprecated)
            {
                operation.Deprecated = true;
            }

            return Task.CompletedTask;
        });

        return options;
    }
}
