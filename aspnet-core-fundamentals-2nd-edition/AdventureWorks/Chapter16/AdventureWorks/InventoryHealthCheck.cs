using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AdventureWorks;

// Readiness of the warehouse service: one plain call to its
// /health route, without the resilience handler's retries. A slow
// answer is Degraded, a failed one Unhealthy; /health/ready turns
// both into 503.
public sealed class InventoryHealthCheck(
    IHttpClientFactory clients) : IHealthCheck
{
    public const string Client = "inventory-health";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var http = clients.CreateClient(Client);
        try
        {
            using var response = await http.GetAsync(
                "health", cancellationToken);
            var status = (int)response.StatusCode;
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy(
                    $"Inventory answered {status}.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded(
                "Inventory did not answer within a second.");
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Unhealthy(
                "Inventory is unreachable.", ex);
        }
    }
}
