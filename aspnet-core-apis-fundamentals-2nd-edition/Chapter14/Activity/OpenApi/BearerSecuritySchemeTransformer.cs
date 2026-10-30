using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WarehouseApi.OpenApi;

// Declares the Bearer scheme once, in components/securitySchemes
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public const string SchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "A JWT in the Authorization header, for example from dotnet user-jwts in Development."
        };

        return Task.CompletedTask;
    }
}
