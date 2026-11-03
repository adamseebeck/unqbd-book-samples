using System.Net;
using Microsoft.Extensions.Caching.Hybrid;

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

// Activity: HybridCache instead of cache-aside. GetOrCreateAsync
// lets one caller fill a missing entry while the others wait for
// it, so ten requests at once make one call. It keeps entries in
// memory and, when one is registered, in IDistributedCache too.
public sealed class StockAvailability(HybridCache cache,
    InventoryClient client)
{
    static readonly HybridCacheEntryOptions Entry = new()
    {
        Expiration = TimeSpan.FromSeconds(30),
        LocalCacheExpiration = TimeSpan.FromSeconds(30)
    };

    public async Task<WarehouseStock?> GetAsync(string sku,
        CancellationToken ct) =>
        await cache.GetOrCreateAsync("stock:v1:" + sku,
            async token => await client.GetAsync(sku, token),
            Entry, cancellationToken: ct);
}

public static class InventoryClientSetup
{
    public static IServiceCollection AddInventoryClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHybridCache();
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
