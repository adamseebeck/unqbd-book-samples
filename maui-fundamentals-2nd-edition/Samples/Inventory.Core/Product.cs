using SQLite;

namespace MauiInventory.Core;
public sealed class Product
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    // Store money as integer cents, so SQLite REAL conversion cannot round it.
    public long UnitPriceCents { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Sku) || Sku.Length > 40)
            throw new ArgumentException("SKU must contain 1 to 40 characters.");
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 120)
            throw new ArgumentException("Name must contain 1 to 120 characters.");
        if (Quantity < 0 || UnitPriceCents < 0)
            throw new ArgumentOutOfRangeException(nameof(Quantity), "Quantity and price must be nonnegative.");
    }

    public Product Copy() => new()
    {
        Id = Id,
        Sku = Sku,
        Name = Name,
        Quantity = Quantity,
        UnitPriceCents = UnitPriceCents,
        UpdatedUtc = UpdatedUtc
    };
}

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task SaveAsync(Product product, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class MemoryProductRepository : IProductRepository
{
    private readonly object gate = new();
    private readonly List<Product> items = [new()
    {
        Id = 1,
        Sku = "LAPTOP",
        Name = "Laptop",
        Quantity = 15,
        UnitPriceCents = 99999
    }, new()
    {
        Id = 2,
        Sku = "MOUSE",
        Name = "Mouse",
        Quantity = 150,
        UnitPriceCents = 2999
    }, new()
    {
        Id = 3,
        Sku = "KEYBOARD",
        Name = "Keyboard",
        Quantity = 75,
        UnitPriceCents = 7999
    }

    ];
    public Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
            return Task.FromResult<IReadOnlyList<Product>>(items.Select(p => p.Copy()).ToArray());
    }

    public Task<Product?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
            return Task.FromResult(items.Find(p => p.Id == id)?.Copy());
    }

    public Task SaveAsync(Product product, CancellationToken cancellationToken = default)
    {
        product.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            if (items.Any(p => p.Sku == product.Sku && p.Id != product.Id))
                throw new ArgumentException("SKU already exists.");
            if (product.Id == 0)
                product.Id = items.Count == 0 ? 1 : items.Max(p => p.Id) + 1;
            items.RemoveAll(p => p.Id == product.Id);
            items.Add(product.Copy());
        }

        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
            return Task.FromResult(items.RemoveAll(p => p.Id == id) > 0);
    }
}
