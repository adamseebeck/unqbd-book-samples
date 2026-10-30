using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public class HealthService : IHealthService
{
    public Task<HealthCheckResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HealthCheckResponse(
            Status: "Healthy",
            Timestamp: DateTime.UtcNow,
            Version: "1.0.0"
        ));
    }
}
