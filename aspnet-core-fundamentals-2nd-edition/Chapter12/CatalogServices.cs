using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
namespace AdventureWorks.CacheSample;

public sealed record Product(int Id, string Name, long UnitPriceCents, int Generation);
public sealed record Stock(int Quantity);
public sealed class ProductSource
{
    private int generations;
    public int Generations => generations;
    public Product? Find(int id) => id == 1
        ? new Product(1, "Notebook", 1200, Interlocked.Increment(ref generations)) : null;
}
public sealed class CatalogCache(IDistributedCache cache, ProductSource source) : IDisposable
{
    private readonly SemaphoreSlim fillLock = new(1, 1); // One bounded teaching catalog entry.
    public const string Key = "catalog:v1:product:1";
    public async Task<Product?> GetAsync(int id, CancellationToken ct)
    {
        if (id != 1) return null;
        var value = await cache.GetStringAsync(Key, ct);
        if (value is not null) return JsonSerializer.Deserialize<Product>(value);
        await fillLock.WaitAsync(ct);
        try
        {
            value = await cache.GetStringAsync(Key, ct);
            if (value is not null) return JsonSerializer.Deserialize<Product>(value);
            var product = source.Find(id);
            if (product is not null) await cache.SetStringAsync(Key, JsonSerializer.Serialize(product),
                new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(2),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) }, ct);
            return product;
        }
        finally { fillLock.Release(); }
    }
    public Task InvalidateAsync(CancellationToken ct) => cache.RemoveAsync(Key, ct);
    public void Dispose() => fillLock.Dispose();
}
public sealed class InventoryClient(HttpClient client)
{
    public async Task<Stock> GetAsync(CancellationToken ct)
        => await client.GetFromJsonAsync<Stock>("stock/1", ct)
            ?? throw new HttpRequestException("The inventory response was empty.");
}
// Activity helper: a bounded cache-fill lease, not a correctness lock for business writes.
public sealed class RedisLease(IDatabase database, RedisKey key, RedisValue owner) : IAsyncDisposable
{
    public static async Task<RedisLease?> TryAcquireAsync(IDatabase database, RedisKey key, TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(lifetime));
        var owner = Guid.NewGuid().ToString("N");
        return await database.StringSetAsync(key, owner, lifetime, When.NotExists)
            ? new RedisLease(database, key, owner) : null;
    }
    public async ValueTask DisposeAsync()
    {
        await database.ScriptEvaluateAsync(
            "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end",
            new[] { key }, new[] { owner });
    }
}
