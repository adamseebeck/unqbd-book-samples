using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace AdventureWorks;

public sealed record WarehouseStock(string Sku, int Available);

// The warehouse's stock service (InventoryService, beside the
// app), called through a typed HttpClient with the standard
// resilience handler: timeouts, retries, a circuit breaker.
public sealed class InventoryClient(HttpClient http)
{
    public async Task<WarehouseStock?> GetAsync(string sku,
        CancellationToken ct)
    {
        using var response = await http.GetAsync(
            $"stock/{Uri.EscapeDataString(sku)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content
            .ReadFromJsonAsync<WarehouseStock>(ct);
    }
}

// Cache-aside: read the shared cache, call the service only on a
// miss, and keep the answer for 30 seconds.
public sealed class StockAvailability(IDistributedCache cache,
    InventoryClient client)
{
    static readonly DistributedCacheEntryOptions Entry = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
    };

    public async Task<WarehouseStock?> GetAsync(string sku,
        CancellationToken ct)
    {
        var key = "stock:v1:" + sku;
        var cached = await cache.GetStringAsync(key, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<WarehouseStock>(
                cached, JsonSerializerOptions.Web);
        var stock = await client.GetAsync(sku, ct);
        if (stock is not null)
            await cache.SetStringAsync(key,
                JsonSerializer.Serialize(
                    stock, JsonSerializerOptions.Web),
                Entry, ct);
        return stock;
    }
}

public static class InventoryClientSetup
{
    public static IServiceCollection AddInventoryClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<StockAvailability>();
        services.AddHttpClient<InventoryClient>(http =>
                http.BaseAddress = new Uri(
                    configuration["Inventory:BaseUrl"]
                        ?? "http://127.0.0.1:42132/"))
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout =
                    TimeSpan.FromSeconds(1);
                options.TotalRequestTimeout.Timeout =
                    TimeSpan.FromSeconds(5);
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay =
                    TimeSpan.FromMilliseconds(100);
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.SamplingDuration =
                    TimeSpan.FromSeconds(30);
                options.CircuitBreaker.BreakDuration =
                    TimeSpan.FromSeconds(15);
            });
        return services;
    }
}
