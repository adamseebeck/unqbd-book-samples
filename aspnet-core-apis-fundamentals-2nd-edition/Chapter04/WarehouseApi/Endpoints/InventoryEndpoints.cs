using Microsoft.AspNetCore.Http.HttpResults;
using WarehouseApi.Contracts;
using WarehouseApi.Services;

namespace WarehouseApi.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        // Get product by SKU using a named handler method
        app.MapGet("/api/products/{sku}", GetProductHandler);

        // Get stock level for a specific warehouse and SKU
        app.MapGet("/api/warehouses/{warehouseCode}/stock/{sku}", async (
            string warehouseCode,
            string sku,
            IInventoryService inventoryService,
            CancellationToken cancellationToken) =>
        {
            var stock = await inventoryService.GetStockLevelAsync(warehouseCode, sku, cancellationToken);
            return stock is null ? Results.NotFound() : Results.Ok(stock);
        });

        // Search products with query string filtering
        app.MapGet("/api/products", async (
            string? search,
            bool? isActive,
            IInventoryService inventoryService,
            CancellationToken cancellationToken) =>
        {
            var products = await inventoryService.SearchProductsAsync(search, isActive, cancellationToken);
            return Results.Ok(products);
        });

        // Create product
        app.MapPost("/api/products", async (
            ProductSummary product,
            IInventoryService inventoryService,
            CancellationToken cancellationToken) =>
        {
            if (!IsComplete(product))
            {
                return Results.BadRequest(new { Error = ProductRules });
            }

            var created = await inventoryService.CreateProductAsync(product, cancellationToken);
            return created is null
                ? Results.Conflict(new { Error = $"Product {product.Sku} already exists" })
                : Results.Created($"/api/products/{created.Sku}", created);
        });

        // Replace product
        app.MapPut("/api/products/{sku}", async (
            string sku,
            ProductSummary product,
            IInventoryService inventoryService,
            CancellationToken cancellationToken) =>
        {
            // The URL and the body must name the same product, in any case
            if (!string.Equals(sku, product.Sku, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { Error = "SKU mismatch" });
            }

            if (!IsComplete(product))
            {
                return Results.BadRequest(new { Error = ProductRules });
            }

            var updated = await inventoryService.UpdateProductAsync(sku, product, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        // Delete product
        app.MapDelete("/api/products/{sku}", async (
            string sku,
            IInventoryService inventoryService,
            CancellationToken cancellationToken) =>
        {
            var deleted = await inventoryService.DeleteProductAsync(sku, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }

    // The return type lists both outcomes, so a test or the OpenAPI generator can read them
    private static async Task<Results<Ok<ProductSummary>, NotFound>> GetProductHandler(
        string sku,
        IInventoryService inventoryService,
        CancellationToken cancellationToken)
    {
        var product = await inventoryService.GetProductAsync(sku, cancellationToken);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
    }

    private const string ProductRules =
        "A SKU of 1 to 32 letters, digits, and hyphens, a name, and a unit price above zero are required";

    // JSON binding leaves a missing name null rather than rejecting the request, and the SKU becomes
    // part of the product's URL; Chapter 8 replaces these checks with validation attributes
    private static bool IsComplete(ProductSummary product) =>
        product.Sku is { Length: >= 1 and <= 32 } sku
        && sku.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')
        && !string.IsNullOrWhiteSpace(product.Name)
        && product.UnitPrice > 0;
}
