using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public interface IStockReleaseService
{
    // null when the idempotency key already belongs to a different request
    Task<OrderResponse?> ReleaseAsync(ReleaseRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}
