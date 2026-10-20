public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/v1/products").WithTags("Products");
        products.MapGet("/", (LinkGenerator links, HttpContext context) => Results.Ok(new
        {
            Items = new[] { "Helmet", "Jersey" },
            Featured = links.GetPathByName(context, "GetProductById", new { id = 42 })
        }));
        products.MapGet("/{id:int}", (int id, LinkGenerator links, HttpContext context) =>
            id <= 0 ? Results.BadRequest(new { Error = "ID must be positive." }) : Results.Ok(new
            {
                Id = id,
                Name = "AdventureWorks Helmet",
                ReviewsLink = links.GetPathByName(context, "GetProductReviews", new { productId = id })
            }))
            .WithName("GetProductById");
        products.MapGet("/search", () => Results.Ok(new { Kind = "literal search route" }));
        products.MapGet("/category/{category:productCategory}", (string category) =>
            Results.Ok(new { Category = category })).WithName("GetProductsByCategory");
        var reviews = products.MapGroup("/{productId:int}/reviews").WithTags("Reviews");
        reviews.MapGet("/", (int productId) =>
            Results.Ok(new { ProductId = productId, Reviews = Array.Empty<string>() }))
            .WithName("GetProductReviews");
        return app;
    }
}
