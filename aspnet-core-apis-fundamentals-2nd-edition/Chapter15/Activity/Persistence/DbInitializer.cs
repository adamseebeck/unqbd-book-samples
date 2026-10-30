using Microsoft.EntityFrameworkCore;
using WarehouseApi.Models;

namespace WarehouseApi.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(WarehouseDbContext context, CancellationToken cancellationToken = default)
    {
        // Applies pending migrations; EF Core 11 throws if the project contains no migrations
        await context.Database.MigrateAsync(cancellationToken);

        if (await context.Warehouses.AnyAsync(cancellationToken))
        {
            return; // Already seeded
        }

        // The sample data from Chapters 5 and 6, without reserved units: only an allocation reserves stock
        context.Warehouses.AddRange(
            new Warehouse { Id = 1, Code = "WH-001", Name = "North Distribution Center", Location = "Chicago, IL", IsOperational = true },
            new Warehouse { Id = 2, Code = "WH-002", Name = "South Fulfillment Hub", Location = "Atlanta, GA", IsOperational = true });

        context.Products.AddRange(
            new Product { Sku = "SKU-001", Name = "Industrial Shelf Bracket", Description = "Steel bracket for pallet racking", UnitPrice = 12.99m, IsActive = true },
            new Product { Sku = "SKU-002", Name = "Heavy Duty Casters (Set of 4)", Description = "Swivel casters for warehouse carts", UnitPrice = 45.50m, IsActive = true });

        context.StockItems.AddRange(
            NewStock("WH-001", "SKU-001", onHand: 150, reorderThreshold: 40),
            NewStock("WH-001", "SKU-002", onHand: 75, reorderThreshold: 100),
            NewStock("WH-002", "SKU-001", onHand: 60, reorderThreshold: 25));

        // One SaveChangesAsync call; EF Core inserts the rows in dependency order
        await context.SaveChangesAsync(cancellationToken);
    }

    private static StockItem NewStock(string warehouseCode, string productSku, int onHand, int reorderThreshold) =>
        new()
        {
            WarehouseCode = warehouseCode,
            ProductSku = productSku,
            OnHand = onHand,
            ReorderThreshold = reorderThreshold
        };
}
