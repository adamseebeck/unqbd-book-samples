using System.ComponentModel.DataAnnotations;

public sealed record ProductItem(int Id, string Name, decimal Price, int Version);
public sealed class ProductInput
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "100000")]
    public decimal Price { get; set; }
    [Range(1, int.MaxValue)]
    public int Version { get; set; }
}

public sealed class ProductStore
{
    private readonly object _gate = new();
    private readonly Dictionary<int, ProductItem> _items = new()
        { [1] = new(1, "AdventureWorks Helmet", 29.99m, 1) };
    public Task<ProductItem?> GetAsync(int id)
    {
        lock (_gate) return Task.FromResult(_items.GetValueOrDefault(id));
    }
    public Task<ProductItem[]> ListAsync()
    {
        lock (_gate) return Task.FromResult(_items.Values.OrderBy(p => p.Id).Take(20).ToArray());
    }
    public Task<bool> UpdateAsync(int id, ProductInput input)
    {
        lock (_gate)
        {
            if (!_items.TryGetValue(id, out var item) || item.Version != input.Version)
                return Task.FromResult(false);
            _items[id] = item with { Name = input.Name, Price = input.Price, Version = item.Version + 1 };
            return Task.FromResult(true);
        }
    }
    public Task<bool> DeleteAsync(int id)
    {
        lock (_gate) return Task.FromResult(_items.Remove(id));
    }
}
