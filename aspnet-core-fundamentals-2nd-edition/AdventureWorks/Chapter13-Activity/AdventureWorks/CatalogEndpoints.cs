using AdventureWorks.Core;
using AdventureWorks.Infrastructure.Data;
using Microsoft.AspNetCore.OutputCaching;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace AdventureWorks;

public static class CatalogEndpoints
{
    public static IServiceCollection AddCatalog(
        this IServiceCollection services)
    {
        // The catalog lives in the database now. A DbContext is
        // scoped, so everything that uses one is scoped too.
        // Chapter 12: a decorator around EfProductService evicts
        // the cached catalog after every product write.
        services.AddScoped<EfProductService>();
        services.AddScoped<IProductService>(provider =>
            new CatalogCacheEviction(
                provider.GetRequiredService<EfProductService>(),
                provider
                    .GetRequiredService<IOutputCacheStore>()));
        services.AddScoped<IInventoryRepository,
            EfInventoryRepository>();
        services.AddScoped<CatalogAvailability>();
        services.Configure<RouteOptions>(options =>
            options.ConstraintMap.Add("productCategory",
                typeof(ProductCategoryConstraint)));
        return services;
    }

    public static IEndpointRouteBuilder MapCatalogEndpoints(
        this IEndpointRouteBuilder app)
    {
        var catalog = app.MapGroup("/api/catalog")
            .WithTags("Catalog").AllowAnonymous();
        catalog.MapGet("/", ListCatalog)
            .CacheOutput(StorePerformance.CatalogTag);
        catalog.MapGet("/{id:int}", GetCatalogItem)
            .WithName("GetCatalogItem")
            .CacheOutput(StorePerformance.CatalogTag);
        catalog.MapGet("/search", SearchCatalog)
            .RequireRateLimiting("search");
        catalog.MapGet("/{id:int}/availability", GetAvailability);
        catalog.MapGet("/category/{category:productCategory}",
            ListCategory).WithName("GetCatalogCategory");
        var reviews = catalog.MapGroup("/{productId:int}/reviews")
            .WithTags("Reviews");
        reviews.MapGet("/", (int productId) => Results.Ok(new
        {
            ProductId = productId,
            Reviews = Array.Empty<string>()
        })).WithName("GetProductReviews");
        return app;
    }

    static async Task<IResult> ListCatalog(
        IProductService products, LinkGenerator links,
        HttpContext context) => Results.Ok(new
        {
            Items = await products.SearchAsync(null, 1, 20),
            Featured = links.GetPathByName(
                context, "GetCatalogItem", new { id = 3 })
        });

    static async Task<IResult> GetCatalogItem(int id,
        IProductService products, LinkGenerator links,
        HttpContext context)
    {
        if (id <= 0)
            return Results.BadRequest(
                new { Error = "ID must be positive." });
        var product = await products.GetByIdAsync(id);
        if (product is null) return Results.NotFound();
        return Results.Ok(new
        {
            product.Id, product.Name, product.Sku,
            product.Price, product.Category,
            ReviewsLink = links.GetPathByName(context,
                "GetProductReviews", new { productId = id })
        });
    }

    // Warehouse stock from InventoryService, through the cache.
    // 503 when it cannot answer: retries used up, the attempt or
    // total timeout passed, or the circuit breaker is open.
    static async Task<IResult> GetAvailability(int id,
        IProductService products, StockAvailability stock,
        CancellationToken ct)
    {
        var product = await products.GetByIdAsync(id);
        if (product is null) return Results.NotFound();
        try
        {
            var found = await stock.GetAsync(product.Sku, ct);
            return found is null ? Results.NotFound()
                : Results.Ok(new { ProductId = id, found.Sku,
                    found.Available });
        }
        catch (Exception ex) when (ex is HttpRequestException
            or TimeoutRejectedException or BrokenCircuitException)
        {
            return Results.Problem(
                "The warehouse did not answer; try again soon.",
                statusCode:
                    StatusCodes.Status503ServiceUnavailable,
                title: "Stock is unavailable");
        }
    }

    static async Task<IResult> SearchCatalog(
        string? q, IProductService products) =>
        Results.Ok(await products.SearchAsync(q, 1, 20));

    static async Task<IResult> ListCategory(
        string category, IProductService products) =>
        Results.Ok(await products.ListByCategoryAsync(category));
}
