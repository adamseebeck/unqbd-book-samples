using WarehouseApi.Contracts;
using WarehouseApi.Exceptions;
using WarehouseApi.Services;

namespace WarehouseApi.Tests;

// A test double: each operation fails the way one test needs, so no database is involved
public class ThrowingOrderService : IOrderService
{
    public Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult<OrderResponse?>(null);

    public Task<IReadOnlyList<OrderResponse>> GetOrdersAsync(OrderStatus? status, int page, int pageSize, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OrderResponse>>([]);

    public Task<OrderResponse?> CreateAsync(CreateOrderRequest request, string idempotencyKey, CancellationToken cancellationToken) =>
        throw new NotSupportedException("The error tests do not create orders.");

    public Task<OrderResponse> CancelAsync(Guid id, CancellationToken cancellationToken) =>
        throw new OrderNotFoundException(id);

    public Task<OrderResponse> FulfillAsync(Guid id, CancellationToken cancellationToken) =>
        throw new ConcurrentModificationException("StockItem", "WH-001/SKU-001");

    // An infrastructure failure whose message must never reach a client
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Login failed for user 'warehouse_app'. Server=sql-prod-01");
}
