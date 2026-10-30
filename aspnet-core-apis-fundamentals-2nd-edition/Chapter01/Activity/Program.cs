var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IProductService, ProductService>();

var app = builder.Build();

app.MapGet("/api/products/{sku}", async (string sku, IProductService service) =>
{
    var product = await service.GetBySkuAsync(sku);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

app.Run();

public record ProductSummary(
    string Sku,
    string Name,
    decimal UnitPrice,
    bool IsActive
);

public interface IProductService
{
    Task<ProductSummary?> GetBySkuAsync(string sku);
}

public class ProductService : IProductService
{
    // Case-insensitive keys: SKU-001 and sku-001 identify the same product
    private static readonly Dictionary<string, ProductSummary> Products =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["SKU-001"] = new ProductSummary("SKU-001", "Industrial Shelf Bracket", 12.99m, true),
            ["SKU-002"] = new ProductSummary("SKU-002", "Heavy Duty Casters (Set of 4)", 45.50m, true),
            ["SKU-003"] = new ProductSummary("SKU-003", "Pallet Label Printer", 289.00m, false)
        };

    public Task<ProductSummary?> GetBySkuAsync(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return Task.FromResult<ProductSummary?>(null);
        }

        Products.TryGetValue(sku, out var product);
        return Task.FromResult(product);
    }
}
