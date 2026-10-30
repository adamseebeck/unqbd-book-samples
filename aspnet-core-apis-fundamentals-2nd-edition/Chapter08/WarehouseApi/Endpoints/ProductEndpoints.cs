using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Contracts;
using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/products", async (
            [AsParameters] ProductSearchParameters parameters,
            IProductService service,
            CancellationToken cancellationToken) =>
        {
            var pagination = new PaginationParameters(parameters.PageNumber, parameters.PageSize);

            if (!pagination.IsValid)
            {
                return Results.BadRequest(new { Error = "PageNumber must be 1 to 100000 and PageSize 1 to 100" });
            }

            var result = await service.SearchAsync(parameters.SearchTerm, parameters.IsActive, pagination, cancellationToken);
            return Results.Ok(result);
        });

        app.MapGet("/api/products/{sku:sku}", async (
            [FromRoute] string sku,
            IProductService service,
            CancellationToken cancellationToken) =>
        {
            var product = await service.GetBySkuAsync(sku, cancellationToken);
            return product is null ? Results.NotFound() : Results.Ok(product);
        });

        app.MapPost("/api/products", async (
            CreateProductRequest request,
            IProductService service,
            CancellationToken cancellationToken) =>
        {
            // AddValidation has already checked the request against its attributes
            var created = await service.CreateAsync(request, cancellationToken);

            if (created is null)
            {
                return Results.Conflict(new { Error = $"Product {request.Sku} already exists" });
            }

            return Results.Created($"/api/products/{created.Sku}", created);
        });

        app.MapPut("/api/products/{sku:sku}", async (
            string sku,
            UpdateProductRequest request,
            IProductService service,
            CancellationToken cancellationToken) =>
        {
            var updated = await service.UpdateAsync(sku, request, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        return app;
    }
}
