using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Contracts;
using WarehouseApi.RoutingConstraints;
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
            var products = await service.SearchAsync(parameters, cancellationToken);
            return Results.Ok(products);
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
            // The route constraint guards only URLs, and JSON binding leaves a missing name null;
            // Chapter 8 replaces these checks with validation attributes
            if (!SkuRouteConstraint.IsValidSku(request.Sku) || string.IsNullOrWhiteSpace(request.Name) || request.UnitPrice <= 0)
            {
                return Results.BadRequest(new { Error = "A valid SKU (for example, SKU-001), a name, and a unit price above zero are required" });
            }

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
            // A missing isActive binds null rather than false, so the check can reject it
            if (string.IsNullOrWhiteSpace(request.Name) || request.UnitPrice <= 0 || request.IsActive is null)
            {
                return Results.BadRequest(new { Error = "Name, a unit price above zero, and isActive are required" });
            }

            var updated = await service.UpdateAsync(sku, request, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        return app;
    }
}
