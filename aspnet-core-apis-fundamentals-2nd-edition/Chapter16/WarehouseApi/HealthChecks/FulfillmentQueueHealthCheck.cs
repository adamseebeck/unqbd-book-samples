using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using WarehouseApi.Fulfillment;

namespace WarehouseApi.HealthChecks;

// A deep queue means the ERP calls fall behind; the instance still works, so Degraded rather than Unhealthy
public class FulfillmentQueueHealthCheck : IHealthCheck
{
    private readonly IFulfillmentJobQueue _queue;
    private readonly int _degradedDepth;

    public FulfillmentQueueHealthCheck(IFulfillmentJobQueue queue, IOptions<FulfillmentOptions> options)
    {
        _queue = queue;
        _degradedDepth = options.Value.DegradedQueueDepth;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var depth = _queue.Count;
        var data = new Dictionary<string, object> { ["depth"] = depth };

        return Task.FromResult(depth >= _degradedDepth
            ? HealthCheckResult.Degraded($"{depth} shipment requests are waiting", data: data)
            : HealthCheckResult.Healthy($"{depth} shipment requests are waiting", data));
    }
}
