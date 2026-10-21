using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public static class PriorModelProof
{
    public static async Task Main()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlServer(MigrationFactory.ReadLocalConnection()).Options;
        await using var previous = new InventoryDbContext(options);
        var mapped = previous.Model.FindEntityType(typeof(Product))!;
        string[] expected = ["CategoryId", "Id", "IsDeleted", "LastModifiedUtc", "Name", "Price", "RowVersion", "Sku", "StockQuantity"];
        if (!expected.SequenceEqual(mapped.GetProperties().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal)))
            throw new InvalidOperationException("Expected the actual unexpanded Chapter15 product mapping.");
        var p = await previous.Products.AsNoTracking().Include(x => x.Category)
            .Include(x => x.ProductCategories).ThenInclude(x => x.Category).AsSingleQuery().SingleAsync();
        if (p.Name != "C# Notebook" || p.Sku != "BOOK-001" || p.Price != 12.50m || p.StockQuantity != 10 ||
            p.IsDeleted || p.RowVersion.Length != 8 || p.Category.Name != "Books" ||
            p.ProductCategories.Single().Category.Name != "Gifts" || p.ProductCategories.Single().StockThreshold != 3)
            throw new InvalidOperationException("Actual previous product/relationship mapping could not read the expanded schema.");
        if (await previous.Products.IgnoreQueryFilters().CountAsync() != 2)
            throw new InvalidOperationException("Expected the hidden fixture as well as the normally visible product.");
        var utc = await previous.Products.Select(x => EF.Property<DateTime>(x, "LastModifiedUtc")).SingleAsync();
        if (utc == default) throw new InvalidOperationException("Previous shadow audit mapping did not round-trip.");
        var customer = await previous.Customers.AsNoTracking().Include(x => x.Profile)
            .Include(x => x.Orders).ThenInclude(x => x.OrderItems).AsSingleQuery().SingleAsync();
        var line = customer.Orders.Single().OrderItems.Single();
        var payment = await previous.Set<CardPayment>().AsNoTracking().SingleAsync();
        if (customer.ShippingAddress.Street != "1 Test Street" || customer.ShippingAddress.PostalCode != "12345" ||
            customer.Profile?.LoyaltyTier != "Gold" || line.ProductId != p.Id || line.Quantity != 2 ||
            line.UnitPrice != 12.50m || payment.Amount.Value != 25m || payment.LastFour != "1234")
            throw new InvalidOperationException("Previous owned, one-to-one, order or converted TPH mapping failed.");
        Console.WriteLine(JsonSerializer.Serialize(new { model = "actual Chapter15 checkpoint", database = previous.Database.GetDbConnection().Database, p.Id, p.Name,
            p.Price, p.StockQuantity, category = p.Category.Name, versionBytes = p.RowVersion.Length,
            mappedProductProperties = mapped.GetProperties().Select(x => x.Name).OrderBy(x => x).ToArray(),
            secondaryPlacement = p.ProductCategories.Single().Category.Name, customer.ShippingAddress.PostalCode,
            loyaltyTier = customer.Profile!.LoyaltyTier, line.Quantity, line.UnitPrice, amount = payment.Amount.Value,
            payment.LastFour, persistedAudit = utc, normallyVisible = 1, unfilteredProducts = 2 }));
        Console.WriteLine("PASS: exact Chapter15 model reads the same additive schema without the new property");
    }
}
