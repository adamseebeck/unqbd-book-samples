using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WarehouseApi.OpenApi;

// One title, version and description per document, taken from the versions the API declares
public sealed class ApiVersionInfoTransformer : IOpenApiDocumentTransformer
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ApiVersionInfoTransformer(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var description = _provider.ApiVersionDescriptions.FirstOrDefault(d =>
            string.Equals(d.GroupName, context.DocumentName, StringComparison.OrdinalIgnoreCase));

        if (description is null)
        {
            return Task.CompletedTask;
        }

        document.Info.Title = "Warehouse API";
        document.Info.Version = description.ApiVersion.ToString();
        document.Info.Description = "Warehouse inventory and order fulfillment API.";

        if (description.IsDeprecated)
        {
            document.Info.Description += " This API version is deprecated.";

            if (description.SunsetPolicy?.Date is { } sunset)
            {
                document.Info.Description += $" It will be removed on {sunset.UtcDateTime:yyyy-MM-dd}.";
            }
        }

        return Task.CompletedTask;
    }
}
