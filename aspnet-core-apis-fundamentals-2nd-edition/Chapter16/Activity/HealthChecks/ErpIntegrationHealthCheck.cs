using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using WarehouseApi.Integration;

namespace WarehouseApi.HealthChecks;

// Whether the ERP answers its own health endpoint: for monitoring, not for the load balancer's readiness
// probe, because orders and allocations keep working while shipment requests wait in the queue
public class ErpIntegrationHealthCheck : IHealthCheck
{
    public const string ClientName = "ErpClient";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExternalShipmentServiceOptions _options;

    public ErpIntegrationHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalShipmentServiceOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // A named client without the resilience handler: a probe reports the ERP's state, it does not retry
        var client = _httpClientFactory.CreateClient(ClientName);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, new Uri(new Uri(_options.BaseUrl), "health"));
            using var response = await client.SendAsync(request, timeout.Token);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("The ERP is available");
            }

            return response.StatusCode == HttpStatusCode.ServiceUnavailable
                ? HealthCheckResult.Degraded("The ERP reports that it is temporarily unavailable")
                : HealthCheckResult.Unhealthy($"The ERP health endpoint answered {(int)response.StatusCode}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded($"The ERP did not answer within {Timeout.TotalSeconds} seconds");
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Unhealthy("The ERP cannot be reached", ex);
        }
    }
}
