using AdventureWorks.Core;
using AdventureWorks.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AdventureWorks;

public sealed record SearchParameters(
    string? Q, int Page = 1, int PageSize = 20);

public static class ProductEndpointsV2
{
    public static IEndpointRouteBuilder MapProductEndpointsV2(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2/products")
            .WithTags("Products V2")
            .AddEndpointFilter<RequestLoggingEndpointFilter>();
        group.MapGet("/", SearchProducts).AllowAnonymous();
        group.MapGet("/{id:int}", GetProduct)
            .WithName("GetProductV2").AllowAnonymous();
        group.MapPost("/", CreateProduct)
            .ProducesValidationProblem()
            .RequireAuthorization(StorePolicies.AdminOnly);
        group.MapPut("/{id:int}", UpdateProduct)
            .ProducesValidationProblem()
            .RequireAuthorization(StorePolicies.AdminOnly);
        group.MapDelete("/{id:int}", DeleteProduct)
            .RequireAuthorization(StorePolicies.AdminOnly);
        return app;
    }

    static async Task<Results<Ok<Product[]>, BadRequest<string>>>
        SearchProducts([AsParameters] SearchParameters request,
            IProductService products)
    {
        if (request.Page is < 1 or > 10000 ||
            request.PageSize is < 1 or > 100)
            return TypedResults.BadRequest(
                "Page must be 1-10000 and pageSize 1-100.");
        return TypedResults.Ok(await products.SearchAsync(
            request.Q, request.Page, request.PageSize));
    }

    static async Task<Results<Ok<Product>, NotFound>> GetProduct(
        int id, IProductService products)
    {
        var product = await products.GetByIdAsync(id);
        return product is null
            ? TypedResults.NotFound() : TypedResults.Ok(product);
    }

    static async Task<CreatedAtRoute<Product>> CreateProduct(
        CreateProductRequest request, IProductService products)
    {
        var product = await products.CreateAsync(request);
        return TypedResults.CreatedAtRoute(
            product, "GetProductV2", new { id = product.Id });
    }

    static async Task<Results<NoContent, NotFound>> UpdateProduct(
        int id, UpdateProductRequest request,
        IProductService products) =>
        await products.UpdateAsync(id, request)
            ? TypedResults.NoContent() : TypedResults.NotFound();

    static async Task<Results<NoContent, NotFound>> DeleteProduct(
        int id, IProductService products) =>
        await products.DeleteAsync(id)
            ? TypedResults.NoContent() : TypedResults.NotFound();
}
