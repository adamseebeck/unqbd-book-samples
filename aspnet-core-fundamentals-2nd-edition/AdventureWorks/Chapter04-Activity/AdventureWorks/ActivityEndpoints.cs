using AdventureWorks.Core;

namespace AdventureWorks;

public static class ActivityEndpoints
{
    public static IEndpointRouteBuilder MapActivityEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/inventory/{sku:skuFormat}",
            (string sku) => Results.Ok(new { Sku = sku }));
        var products = app.MapGroup("/api/products")
            .WithTags("Product Management");
        products.MapGet("/", ListIds)
            .WithName("ListManagedProducts");
        products.MapGet("/{id:int}", GetById)
            .WithName("GetManagedProduct");
        products.MapPost("/", () => Results.CreatedAtRoute(
            "GetManagedProduct", new { id = 3 }, new { Id = 3 }));
        products.MapGet("/{id:int}/related", Related);
        return app;
    }

    static async Task<IResult> ListIds(IProductService catalog) =>
        Results.Ok((await catalog.SearchAsync(null, 1, 20))
            .Select(product => product.Id));

    static async Task<IResult> GetById(
        int id, IProductService catalog) =>
        await catalog.GetByIdAsync(id) is { } product
            ? Results.Ok(product) : Results.NotFound();

    static IResult Related(
        int id, LinkGenerator links, HttpContext context)
    {
        var paths = new[] { id + 1, id + 2 }
            .Select(related => links.GetPathByName(context,
                "GetManagedProduct", new { id = related }))
            .ToArray();
        return paths.Any(path => path is null)
            ? Results.Problem("Link generation failed.")
            : Results.Ok(paths);
    }
}
