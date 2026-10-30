using System.Text.Json;
using WarehouseApi.Contracts;
using WarehouseApi.Mappings;
using WarehouseApi.Models;
using WarehouseApi.Persistence;

namespace WarehouseApi.Services;

public class OrderService : IOrderService
{
    private readonly InMemoryWarehouseStore _store;
    private readonly IInventoryService _inventoryService;
    private readonly IdempotencyContext _idempotencyContext;

    public OrderService(InMemoryWarehouseStore store, IInventoryService inventoryService, IdempotencyContext idempotencyContext)
    {
        _store = store;
        _inventoryService = inventoryService;
        _idempotencyContext = idempotencyContext;
    }

    public Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult<OrderResponse?>(
                _store.Orders.TryGetValue(id, out var order) ? order.ToResponse() : null);
        }
    }

    public Task<IReadOnlyList<OrderResponse>> GetOrdersAsync(OrderStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            // The ID breaks ties, so orders created at the same moment keep their page
            var query = _store.Orders.Values.OrderBy(o => o.CreatedDate).ThenBy(o => o.Id).AsEnumerable();

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            IReadOnlyList<OrderResponse> orders = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => o.ToResponse())
                .ToList();

            return Task.FromResult(orders);
        }
    }

    public Task<OrderResponse?> CreateAsync(CreateOrderRequest request, string idempotencyKey, CancellationToken cancellationToken)
    {
        // The whole request identifies a retry; warehouse codes compare without regard to case
        var requestHash = _idempotencyContext.CreateRequestHash("create-order|" + JsonSerializer.Serialize(
            request with { FulfillmentWarehouseCode = request.FulfillmentWarehouseCode?.ToUpperInvariant() }));

        // The key check, the new order, and the stored result run as one step under the store's lock
        lock (_store.SyncRoot)
        {
            if (_idempotencyContext.TryGetEntry(idempotencyKey, out var existingEntry))
            {
                // A retry gets the order its first attempt created; the same key with another payload gets null
                return Task.FromResult(existingEntry.RequestHash == requestHash
                    ? (OrderResponse?)existingEntry.Response
                    : null);
            }

            // Validation has checked the request's shape; these rules need the stored data
            foreach (var line in request.Lines)
            {
                if (!_store.Products.TryGetValue(line.Sku, out var product) || !product.IsActive)
                {
                    throw new ArgumentException($"Product {line.Sku} does not exist or is not active.");
                }
            }

            if (request.FulfillmentWarehouseCode is { } code && !_store.Warehouses.ContainsKey(code))
            {
                throw new ArgumentException($"Warehouse {code} does not exist.");
            }

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerReference = request.CustomerReference,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow,
                FulfillmentWarehouseCode = request.FulfillmentWarehouseCode,
                Lines = request.Lines.Select(l => new OrderLine
                {
                    Id = Guid.NewGuid(),
                    Sku = l.Sku,
                    RequestedQuantity = l.Quantity
                }).ToList()
            };

            _store.Orders.Add(order.Id, order);

            // A rejected request stores nothing, so only a created order answers the key's retries
            var response = order.ToResponse();
            _idempotencyContext.Store(idempotencyKey, response, 201, requestHash);
            return Task.FromResult<OrderResponse?>(response);
        }
    }

    public Task<OrderResponse> AllocateAsync(Guid id, string warehouseCode, CancellationToken cancellationToken) =>
        RunExclusiveAsync(id, async order =>
        {
            if (order.Status == OrderStatus.Allocated
                && string.Equals(order.FulfillmentWarehouseCode, warehouseCode, StringComparison.OrdinalIgnoreCase))
            {
                return; // A repeated request changes nothing
            }

            EnsureStatus(order, OrderStatus.Pending, "allocated");
            // The stored code: an allocation sent with wh-001 records WH-001
            var code = EnsureOperational(warehouseCode);

            var reserved = new List<OrderLine>();
            try
            {
                foreach (var line in order.Lines)
                {
                    await _inventoryService.ReserveStockAsync(code, line.Sku, line.RequestedQuantity, cancellationToken);
                    reserved.Add(line);
                }
            }
            catch
            {
                // Undo the lines reserved before the failure, even if the client has gone away
                foreach (var line in reserved)
                {
                    await _inventoryService.ReleaseStockAsync(code, line.Sku, line.RequestedQuantity, CancellationToken.None);
                }

                throw;
            }

            lock (_store.SyncRoot)
            {
                foreach (var line in order.Lines)
                {
                    line.ReservedQuantity = line.RequestedQuantity;
                }

                order.FulfillmentWarehouseCode = code;
                order.Status = OrderStatus.Allocated;
            }
        }, cancellationToken);

    public Task<OrderResponse> FulfillAsync(Guid id, CancellationToken cancellationToken) =>
        RunExclusiveAsync(id, async order =>
        {
            if (order.Status == OrderStatus.Fulfilled)
            {
                return;
            }

            EnsureStatus(order, OrderStatus.Allocated, "fulfilled");

            foreach (var line in order.Lines)
            {
                await _inventoryService.ShipStockAsync(order.FulfillmentWarehouseCode!, line.Sku, line.ReservedQuantity, cancellationToken);
            }

            lock (_store.SyncRoot)
            {
                foreach (var line in order.Lines)
                {
                    line.ShippedQuantity = line.ReservedQuantity;
                    line.ReservedQuantity = 0;
                }

                order.Status = OrderStatus.Fulfilled;
            }
        }, cancellationToken);

    public Task<OrderResponse> CancelAsync(Guid id, CancellationToken cancellationToken) =>
        RunExclusiveAsync(id, async order =>
        {
            if (order.Status == OrderStatus.Cancelled)
            {
                return;
            }

            if (order.Status == OrderStatus.Fulfilled)
            {
                throw new InvalidOperationException($"Order {id} is Fulfilled and cannot be cancelled.");
            }

            if (order.Status == OrderStatus.Allocated)
            {
                foreach (var line in order.Lines)
                {
                    await _inventoryService.ReleaseStockAsync(order.FulfillmentWarehouseCode!, line.Sku, line.ReservedQuantity, cancellationToken);
                }
            }

            lock (_store.SyncRoot)
            {
                foreach (var line in order.Lines)
                {
                    line.ReservedQuantity = 0;
                }

                order.Status = OrderStatus.Cancelled;
            }
        }, cancellationToken);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _store.OrderWorkflowGate.WaitAsync(cancellationToken);
        try
        {
            lock (_store.SyncRoot)
            {
                if (!_store.Orders.TryGetValue(id, out var order))
                {
                    return false;
                }

                // An allocated order holds stock and a fulfilled order is history; neither may vanish
                if (order.Status is OrderStatus.Allocated or OrderStatus.Fulfilled)
                {
                    throw new InvalidOperationException($"Order {id} is {order.Status}. Only pending or cancelled orders can be deleted.");
                }

                return _store.Orders.Remove(id);
            }
        }
        finally
        {
            _store.OrderWorkflowGate.Release();
        }
    }

    // Runs one workflow at a time, because a workflow awaits several inventory calls in a row
    private async Task<OrderResponse> RunExclusiveAsync(Guid id, Func<Order, Task> workflow, CancellationToken cancellationToken)
    {
        await _store.OrderWorkflowGate.WaitAsync(cancellationToken);
        try
        {
            Order? order;
            lock (_store.SyncRoot)
            {
                order = _store.Orders.GetValueOrDefault(id);
            }

            if (order is null)
            {
                throw new KeyNotFoundException($"Order {id} not found.");
            }

            await workflow(order);

            lock (_store.SyncRoot)
            {
                return order.ToResponse();
            }
        }
        finally
        {
            _store.OrderWorkflowGate.Release();
        }
    }

    private string EnsureOperational(string warehouseCode)
    {
        lock (_store.SyncRoot)
        {
            if (!_store.Warehouses.TryGetValue(warehouseCode, out var warehouse) || !warehouse.IsOperational)
            {
                throw new ArgumentException($"Warehouse {warehouseCode} does not exist or is not operational.");
            }

            return warehouse.Code;
        }
    }

    private static void EnsureStatus(Order order, OrderStatus expected, string action)
    {
        if (order.Status != expected)
        {
            throw new InvalidOperationException($"Order {order.Id} is {order.Status} and cannot be {action}.");
        }
    }
}
