using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public interface IOrderService
{
    Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderResponse>> GetOrdersAsync(OrderStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    // null when the idempotency key already belongs to a different request
    Task<OrderResponse?> CreateAsync(CreateOrderRequest request, string idempotencyKey, CancellationToken cancellationToken);
    Task<OrderResponse> AllocateAsync(Guid id, string warehouseCode, CancellationToken cancellationToken);
    Task<OrderResponse> FulfillAsync(Guid id, CancellationToken cancellationToken);
    Task<OrderResponse> CancelAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
