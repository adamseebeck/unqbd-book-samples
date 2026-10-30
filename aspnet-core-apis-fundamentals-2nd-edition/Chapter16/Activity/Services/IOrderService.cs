using WarehouseApi.Contracts;
using WarehouseApi.Security;

namespace WarehouseApi.Services;

public interface IOrderService
{
    Task<OrderResponse?> GetByIdAsync(Guid id, OrderReadScope scope, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderResponse>> GetOrdersAsync(OrderStatus? status, int page, int pageSize, OrderReadScope scope, CancellationToken cancellationToken);
    Task<PagedResult<OrderResponse>> GetOrderPageAsync(OrderStatus? status, PaginationParameters pagination, OrderReadScope scope, CancellationToken cancellationToken);
    // null when the idempotency key already belongs to a different request or to another caller
    Task<OrderResponse?> CreateAsync(CreateOrderRequest request, string idempotencyKey, string? createdBy, CancellationToken cancellationToken);
    Task<OrderResponse> FulfillAsync(Guid id, CancellationToken cancellationToken);
    Task<OrderResponse> CancelAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<ShipmentResponse?> GetShipmentAsync(Guid id, OrderReadScope scope, CancellationToken cancellationToken);
}
