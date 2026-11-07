using System.Collections.Concurrent;
using System.Net.Mail;
using AdventureWorks.Core;

namespace AdventureWorks.Infrastructure;

public sealed class OrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<int, Order> _orders
        = new();
    private int _lastId;

    public Order? GetById(int id) => id == 500
        ? throw new InvalidOperationException(
            "Simulated storage failure.")
        : _orders.GetValueOrDefault(id);

    public Order Add(string ownerId, decimal total)
    {
        var id = Interlocked.Increment(ref _lastId);
        var order = new Order(
            id, $"AW-{id:0000}", ownerId, total);
        _orders[id] = order;
        return order;
    }

    public bool Delete(int id) => _orders.TryRemove(id, out _);

    public bool Archive(int id)
    {
        while (_orders.TryGetValue(id, out var current))
        {
            var archived = current with { Archived = true };
            if (_orders.TryUpdate(id, archived, current))
                return true;
        }
        return false;
    }
}

public sealed class MemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, string> _values
        = new();

    public string? Get(string key) =>
        _values.TryGetValue(key, out var value) ? value : null;

    public void Set(string key, string value) =>
        _values[key] = value;
}

public sealed class EmailValidator : IEmailValidator
{
    public bool IsValid(string address) =>
        !string.IsNullOrWhiteSpace(address) &&
        MailAddress.TryCreate(address, out var parsed) &&
        string.Equals(parsed.Address, address,
            StringComparison.OrdinalIgnoreCase);
}
