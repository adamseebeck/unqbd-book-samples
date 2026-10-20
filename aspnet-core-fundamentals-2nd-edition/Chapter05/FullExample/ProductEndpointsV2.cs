using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

public static class ProductEndpointsV2
{
    public static IEndpointRouteBuilder MapProductEndpointsV2(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2/products").WithTags("Products V2")
            .AddEndpointFilter<RequestLoggingEndpointFilter>();
        group.MapGet("/", async Task<Results<Ok<ProductDto[]>, BadRequest<string>>> (
            [AsParameters] SearchParameters request, IProductService products) =>
        {
            if (request.Page is < 1 or > 10000 || request.PageSize is < 1 or > 100)
                return TypedResults.BadRequest("Page must be 1-10000 and pageSize 1-100.");
            return TypedResults.Ok(await products.SearchAsync(request.Q, request.Page, request.PageSize));
        });
        group.MapGet("/{id:int}", async Task<Results<Ok<ProductDto>, NotFound>> (int id, IProductService products) =>
        {
            var product = await products.GetByIdAsync(id);
            return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
        }).WithName("GetProductV2");
        group.MapPost("/", async Task<CreatedAtRoute<ProductDto>> (CreateProductRequest request, IProductService products) =>
        {
            var product = await products.CreateAsync(request);
            return TypedResults.CreatedAtRoute(product, "GetProductV2", new { id = product.Id });
        }).AddEndpointFilter<ValidationFilter<CreateProductRequest>>()
          .ProducesValidationProblem();
        group.MapPut("/{id:int}", async Task<Results<NoContent, NotFound>> (
            int id, UpdateProductRequest request, IProductService products) =>
            await products.UpdateAsync(id, request) ? TypedResults.NoContent() : TypedResults.NotFound())
            .AddEndpointFilter<ValidationFilter<UpdateProductRequest>>().ProducesValidationProblem();
        group.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound>> (int id, IProductService products) =>
            await products.DeleteAsync(id) ? TypedResults.NoContent() : TypedResults.NotFound());
        return app;
    }
}
