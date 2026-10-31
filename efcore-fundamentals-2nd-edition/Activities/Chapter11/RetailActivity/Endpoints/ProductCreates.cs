using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

public sealed record CreateProduct(string Name, string Sku,
    decimal Price, int StockQuantity, int CategoryId);

public static class ProductCreates
{
    public static void MapProductCreates(
        this WebApplication app)
    {
        app.MapGet("/products/{id:int}", GetAsync);
        app.MapPost("/products", CreateAsync);
    }

    private static async Task<IResult> GetAsync(int id,
        InventoryDbContext db, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id, ct);
        return product is null
            ? Results.NotFound()
            : Results.Ok(View(product));
    }

    private static async Task<IResult> CreateAsync(
        CreateProduct input, InventoryDbContext db,
        CancellationToken ct)
    {
        if (!IsValid(input)) return Results.BadRequest();
        if (!await db.Categories.AnyAsync(
                c => c.Id == input.CategoryId, ct))
            return Results.BadRequest();

        var product = new Product
        {
            Name = input.Name.Trim(),
            Sku = input.Sku.Trim(),
            Price = input.Price,
            StockQuantity = input.StockQuantity,
            CategoryId = input.CategoryId
        };
        db.Products.Add(product);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException
            is SqlException { Number: 2601 or 2627 })
        {
            return Results.Conflict();
        }
        return Results.Created($"/products/{product.Id}",
            View(product));
    }

    private static ProductView View(Product p) =>
        new(p.Id, p.Name, p.Price, p.StockQuantity,
            Convert.ToBase64String(p.RowVersion));

    private static bool IsValid(CreateProduct input) =>
        !string.IsNullOrWhiteSpace(input.Name)
        && input.Name.Length <= 200
        && !string.IsNullOrWhiteSpace(input.Sku)
        && input.Sku.Length <= 40
        && input.Price is >= 0 and <= 1_000_000
        && decimal.Round(input.Price, 2) == input.Price
        && input.StockQuantity >= 0;
}
