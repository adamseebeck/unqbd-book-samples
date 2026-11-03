namespace AdventureWorks.Infrastructure.Data;

// The rows EF Core maps to tables. Core's records stay the app's
// model; the EF Core classes convert at this boundary.
public sealed class ProductEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Sku { get; set; } = "";
    public long PriceCents { get; set; }
    public string Category { get; set; } = "";
    public int Version { get; set; } = 1;
}

public sealed class OrderEntity
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public bool Archived { get; set; }
    public string? Notes { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public List<OrderItemEntity> Items { get; set; } = [];
}

public sealed class OrderItemEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public long UnitPriceCents { get; set; }
}

public sealed class InventoryItemEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Location { get; set; } = "";
    public int Quantity { get; set; }
}

// Core and the API use decimal; the database stores whole cents,
// because SQLite cannot sum decimal columns.
public static class Money
{
    public static long ToCents(decimal amount) =>
        (long)decimal.Round(amount * 100,
            MidpointRounding.AwayFromZero);

    public static decimal FromCents(long cents) => cents * 0.01m;
}
