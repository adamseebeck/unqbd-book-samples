using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public interface IHealthService
{
    Task<HealthCheckResponse> GetHealthAsync(CancellationToken cancellationToken = default);
}
