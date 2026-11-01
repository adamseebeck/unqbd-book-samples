using Microsoft.EntityFrameworkCore;

public sealed record ProductView(int Id, string Name,
    decimal Price, int StockQuantity, string Version);

public sealed record EditProduct(string Name, decimal Price,
    int StockQuantity, string Version);

public static class ProductEndpoints
{
    public static void MapProductEndpoints(
        this WebApplication app)
    {
        app.MapGet("/products", ListAsync);
        app.MapGet("/products/page", PageAsync);
        app.MapPut("/products/{id:int}", EditAsync);
    }

    private static async Task<IResult> ListAsync(
        int? take, InventoryDbContext db, CancellationToken ct)
    {
        var size = take ?? 20;
        if (size is < 1 or > 100) return Results.BadRequest();
        var items = await db.Products
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Take(size)
            .Select(p => ToView(p))
            .ToListAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> PageAsync(
        int? page, int? take, int? threshold,
        InventoryDbContext db, CancellationToken ct)
    {
        var (number, size, limit) = (page ?? 1, take ?? 20,
            threshold ?? 20);
        if (number is < 1 or > 10_000 || size is < 1 or > 100
            || limit < 0)
            return Results.BadRequest();

        var lowStock = new LowStockSpecification(limit);
        var filtered = db.Products
            .AsNoTracking()
            .Where(lowStock.Criteria);
        var total = await filtered.CountAsync(ct);
        var items = await filtered
            .OrderBy(p => p.Id)
            .Skip((number - 1) * size)
            .Take(size)
            .Select(p => ToView(p))
            .ToListAsync(ct);
        return Results.Ok(new
        {
            Page = number,
            Total = total,
            TotalPages = (total + size - 1) / size,
            Items = items
        });
    }

    private static async Task<IResult> EditAsync(
        int id, EditProduct input,
        InventoryDbContext db, CancellationToken ct)
    {
        if (!IsValid(input, out var token))
            return Results.BadRequest();
        var product = await db.Products
            .SingleOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return Results.NotFound();
        if (!token.AsSpan().SequenceEqual(product.RowVersion))
            return Results.Conflict();

        db.Entry(product).Property(p => p.RowVersion)
            .OriginalValue = token;
        product.Name = input.Name.Trim();
        product.Price = input.Price;
        product.StockQuantity = input.StockQuantity;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict();
        }
        return Results.Ok(ToView(product));
    }

    private static ProductView ToView(Product p) =>
        new(p.Id, p.Name, p.Price, p.StockQuantity,
            Convert.ToBase64String(p.RowVersion));

    private static bool IsValid(
        EditProduct input, out byte[] token)
    {
        token = new byte[8];
        return !string.IsNullOrWhiteSpace(input.Name)
            && input.Name.Length <= 200
            && input.Price is >= 0 and <= 1_000_000
            && decimal.Round(input.Price, 2) == input.Price
            && input.StockQuantity >= 0
            && Convert.TryFromBase64String(
                input.Version ?? "", token, out var length)
            && length == 8;
    }
}
