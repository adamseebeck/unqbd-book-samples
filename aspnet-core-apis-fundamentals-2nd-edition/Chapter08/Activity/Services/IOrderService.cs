using WarehouseApi.Contracts;

namespace WarehouseApi.Services;

public interface IOrderService
{
    Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderResponse>> GetOrdersAsync(OrderStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    // null when the idempotency key already belongs to a different request
    Task<OrderResponse?> CreateAsync(CreateOrderRequest request, string idempotencyKey, CancellationToken cancellationToken);
    Task<OrderResponse?> UpdateStatusAsync(Guid id, OrderStatus newStatus, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
