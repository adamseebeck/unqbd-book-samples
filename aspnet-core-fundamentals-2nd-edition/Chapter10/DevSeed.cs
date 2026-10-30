using Microsoft.EntityFrameworkCore;
namespace AdventureWorks.DataSample;

// Development-only sample data for the requests in requests.http. It never migrates: run
// dotnet ef database update first. It seeds only an empty database and changes nothing otherwise.
public static class DevSeed
{
    public static async Task SeedDevelopmentDataAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!db.Database.GetMigrations().Any() || (await db.Database.GetPendingMigrationsAsync()).Any())
        {
            app.Logger.LogWarning("DevSeed skipped: add and apply the migration first (dotnet ef migrations add InitialCreate, then dotnet ef database update).");
            return;
        }
        if (await db.Products.AnyAsync()) return;
        // Generated keys, so the same seed works on SQLite and on the SQL Server option (identity columns).
        // One transaction, so a failed seed leaves the database empty and the next start tries again.
        await using var transaction = await db.Database.BeginTransactionAsync();
        var notebook = new Product { Name = "Notebook", UnitPriceCents = 1200 };
        var pen = new Product { Name = "Pen", UnitPriceCents = 150 };
        db.Products.AddRange(notebook, pen);
        await db.SaveChangesAsync();
        // Two positions for one product, for TransferAsync.
        db.InventoryItems.AddRange(new InventoryItem { ProductId = notebook.Id, Location = "Main", Quantity = 10 },
            new InventoryItem { ProductId = notebook.Id, Location = "Overflow", Quantity = 0 });
        // 25 orders for owner-1 (report pages 1 and 2 hold 20 and 5) and one for owner-2. AW-0001 is the newest.
        var newest = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
        for (var n = 1; n <= 25; n++)
            db.Orders.Add(new Order
            {
                Id = Guid.Parse($"10000000-0000-0000-0000-{n:D12}"), Version = Guid.Parse($"a0000000-0000-0000-0000-{n:D12}"),
                OwnerId = "owner-1", OrderNumber = $"AW-{n:D4}", OrderDate = newest.AddDays(1 - n),
                Items = [new OrderItem { ProductId = notebook.Id, Quantity = n % 3 + 1, UnitPriceCents = notebook.UnitPriceCents }]
            });
        db.Orders.Add(new Order
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), Version = Guid.Parse("b0000000-0000-0000-0000-000000000001"),
            OwnerId = "owner-2", OrderNumber = "AW-0026", OrderDate = newest,
            Items = [new OrderItem { ProductId = pen.Id, Quantity = 4, UnitPriceCents = pen.UnitPriceCents }]
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        app.Logger.LogInformation("DevSeed added 2 products, 2 inventory positions and 26 orders.");
    }
}
