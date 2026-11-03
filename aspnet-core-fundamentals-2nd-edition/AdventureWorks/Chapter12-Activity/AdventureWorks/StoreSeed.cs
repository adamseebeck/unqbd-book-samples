using AdventureWorks.Infrastructure;
using AdventureWorks.Infrastructure.Data;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks;

// Development data for the store tables. Each part fills only an
// empty table, so a restart keeps every change.
public static class StoreSeed
{
    // id, name, SKU, category, price in cents
    static readonly (int, string, string, string, long)[]
        Catalog =
    [
        (1, "Trail Helmet", "HLM-1001", "accessories", 2999),
        (2, "Trail Jersey", "JRS-2001", "clothing", 4999),
        (3, "Mountain-200 Bike", "BIK-3001", "bikes", 129900),
        (4, "Two-Person Tent", "TNT-4001", "camping", 18900)
    ];

    public static async Task AddCatalogAsync(
        AdventureWorksDbContext db)
    {
        if (await db.Products.AnyAsync()) return;
        foreach (var (id, name, sku, category, cents) in Catalog)
            db.Products.Add(new ProductEntity
            {
                Id = id, Name = name, Sku = sku,
                Category = category, PriceCents = cents
            });
        // Two locations for the helmet, for stock transfers.
        db.InventoryItems.AddRange(
            new() { ProductId = 1, Location = "Main",
                Quantity = 10 },
            new() { ProductId = 1, Location = "Overflow" });
        await db.SaveChangesAsync();
    }

    // AW-0002 is jordan's. casey owns the other eleven, so her
    // order list has two pages.
    public static async Task AddOrdersAsync(
        AdventureWorksDbContext db,
        UserManager<ApplicationUser> users)
    {
        if (await db.Orders.AnyAsync()) return;
        var casey = await IdAsync(users, "casey");
        var jordan = await IdAsync(users, "jordan");
        db.Orders.Add(Order(1, casey, productId: 2, 4999));
        db.Orders.Add(Order(2, jordan, productId: 3, 129900));
        db.Orders.Add(Order(3, casey, productId: 4, 18900));
        for (var id = 4; id <= 12; id++)
            db.Orders.Add(Order(id, casey, productId: 1, 2999,
                quantity: id % 3 + 1));
        await db.SaveChangesAsync();
    }

    static OrderEntity Order(int id, string ownerId,
        int productId, long unitPriceCents, int quantity = 1) =>
        new()
        {
            Id = id,
            OrderNumber = $"AW-{id:0000}",
            OwnerId = ownerId,
            Items =
            [
                new()
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPriceCents = unitPriceCents
                }
            ]
        };

    static async Task<string> IdAsync(
        UserManager<ApplicationUser> users, string name) =>
        (await users.FindByNameAsync(
            $"{name}@{DevelopmentSeed.Domain}"))!.Id;
}
