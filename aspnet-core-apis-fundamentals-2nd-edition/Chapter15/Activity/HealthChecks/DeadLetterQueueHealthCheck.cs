using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using WarehouseApi.Fulfillment;

namespace WarehouseApi.HealthChecks;

public class DeadLetterQueueHealthCheck : IHealthCheck
{
    private readonly IFulfillmentDeadLetterQueue _deadLetters;
    private readonly DeadLetterOptions _options;

    public DeadLetterQueueHealthCheck(IFulfillmentDeadLetterQueue deadLetters, IOptions<DeadLetterOptions> options)
    {
        _deadLetters = deadLetters;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var status = await _deadLetters.GetStatusAsync(cancellationToken);
        var data = new Dictionary<string, object> { ["depth"] = status.Depth };

        if (status.Depth > _options.UnhealthyThreshold)
        {
            return HealthCheckResult.Unhealthy($"{status.Depth} failed shipment requests", data: data);
        }

        if (status.Depth > _options.DegradedThreshold)
        {
            return HealthCheckResult.Degraded($"{status.Depth} failed shipment requests", data: data);
        }

        return HealthCheckResult.Healthy($"{status.Depth} failed shipment requests", data);
    }
}
