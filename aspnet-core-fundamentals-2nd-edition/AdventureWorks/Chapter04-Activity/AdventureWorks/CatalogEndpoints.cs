using AdventureWorks.Core;
using AdventureWorks.Infrastructure;

namespace AdventureWorks;

public static class CatalogEndpoints
{
    public static IServiceCollection AddCatalog(
        this IServiceCollection services)
    {
        services.AddSingleton<IProductService,
            InMemoryProductService>();
        services.Configure<RouteOptions>(options =>
            options.ConstraintMap.Add("productCategory",
                typeof(ProductCategoryConstraint)));
        return services;
    }

    public static IEndpointRouteBuilder MapCatalogEndpoints(
        this IEndpointRouteBuilder app)
    {
        var catalog = app.MapGroup("/api/catalog")
            .WithTags("Catalog");
        catalog.MapGet("/", ListCatalog);
        catalog.MapGet("/{id:int}", GetCatalogItem)
            .WithName("GetCatalogItem");
        catalog.MapGet("/search", SearchCatalog);
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

    static async Task<IResult> SearchCatalog(
        string? q, IProductService products) =>
        Results.Ok(await products.SearchAsync(q, 1, 20));

    static async Task<IResult> ListCategory(
        string category, IProductService products) =>
        Results.Ok(await products.ListByCategoryAsync(category));
}
