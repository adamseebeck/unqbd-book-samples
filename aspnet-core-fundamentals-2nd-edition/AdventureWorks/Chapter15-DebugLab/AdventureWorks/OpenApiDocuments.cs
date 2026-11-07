using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AdventureWorks;

// Three OpenAPI documents at /openapi/{name}.json: the store's
// unversioned routes and the two versions of Chapter 5's product
// API. An endpoint's group name picks its document.
public static class OpenApiDocuments
{
    public static IServiceCollection AddStoreOpenApi(
        this IServiceCollection services)
    {
        // The default rule includes endpoints with no group name.
        services.AddOpenApi("store", options =>
            options.AddDocumentTransformer(Info(
                "AdventureWorks Store API", "1.0",
                "Catalog, orders, accounts and Admin routes.")));
        services.AddOpenApi("v1", options =>
        {
            options.ShouldInclude = api => api.GroupName == "v1";
            options.AddDocumentTransformer(Info(
                "AdventureWorks Products API", "1.0",
                "Controllers. Deprecated: use version 2."));
            // Runs once for each operation in this document.
            options.AddOperationTransformer(
                (operation, context, _) =>
                {
                    operation.Deprecated = true;
                    return Task.CompletedTask;
                });
        });
        services.AddOpenApi("v2", options =>
        {
            options.ShouldInclude = api => api.GroupName == "v2";
            options.AddDocumentTransformer(Info(
                "AdventureWorks Products API", "2.0",
                "Minimal APIs, the current version."));
        });
        return services;
    }

    static Func<OpenApiDocument,
        OpenApiDocumentTransformerContext, CancellationToken,
        Task> Info(string title, string version,
        string description) => (document, _, _) =>
    {
        document.Info.Title = title;
        document.Info.Version = version;
        document.Info.Description = description;
        return Task.CompletedTask;
    };
}
