using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Contracts;
using WarehouseApi.Security;
using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class ProductEndpoints
{
    // Called on the versioned /api/v{version} group; the product contract is the same in both versions
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/products")
            .WithTags("Products");

        products.MapGet("/", async (
            [AsParameters] ProductSearchParameters parameters,
            IProductService service,
            CancellationToken cancellationToken) =>
        {
            var pagination = new PaginationParameters(parameters.PageNumber, parameters.PageSize);

            if (!pagination.IsValid)
            {
                return Results.Problem("PageNumber must be 1 to 100000 and PageSize 1 to 100", statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await service.SearchAsync(parameters.SearchTerm, parameters.IsActive, pagination, cancellationToken);
            return Results.Ok(result);
        })
        .Produces<PagedResult<ProductResponse>>()
        .RequireAuthorization();

        products.MapGet("/{sku:sku}", async (
            [FromRoute] string sku,
            IProductService service,
            CancellationToken cancellationToken) =>
        {
            var product = await service.GetBySkuAsync(sku, cancellationToken);
            return product is null
                ? Results.Problem($"Product {sku} not found.", statusCode: StatusCodes.Status404NotFound)
                : Results.Ok(product);
        })
        .WithName("GetProduct")
        .Produces<ProductResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();

        // Supervisors manage the product catalog
        products.MapPost("/", async (
            CreateProductRequest request,
            IProductService service,
            CancellationToken cancellationToken) =>
        {
            // AddValidation has already checked the request against its attributes
            var created = await service.CreateAsync(request, cancellationToken);

            if (created is null)
            {
                return Results.Problem($"Product {request.Sku} already exists.", statusCode: StatusCodes.Status409Conflict);
            }

            // The link generator reuses the {version} value of this request, so Location keeps the client's version
            return Results.CreatedAtRoute("GetProduct", new { sku = created.Sku }, created);
        })
        .Produces<ProductResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireSupervisor);

        products.MapPut("/{sku:sku}", async (
            string sku,
            UpdateProductRequest request,
            IProductService service,
            CancellationToken cancellationToken) =>
        {
            var updated = await service.UpdateAsync(sku, request, cancellationToken);
            return updated is null
                ? Results.Problem($"Product {sku} not found.", statusCode: StatusCodes.Status404NotFound)
                : Results.Ok(updated);
        })
        .Produces<ProductResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireSupervisor);

        return app;
    }
}
