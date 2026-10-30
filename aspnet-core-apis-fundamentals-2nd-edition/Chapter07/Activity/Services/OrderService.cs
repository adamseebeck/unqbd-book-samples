using System.Text.Json;
using WarehouseApi.Contracts;
using WarehouseApi.Models;

namespace WarehouseApi.Services;

public class OrderService : IOrderService
{
    // The service is Scoped, so each request has its own instance; a lock shared by all of them is static
    private static readonly Lock CreateLock = new();

    private readonly IOrderRepository _repository;
    private readonly IdempotencyContext _idempotencyContext;

    public OrderService(IOrderRepository repository, IdempotencyContext idempotencyContext)
    {
        _repository = repository;
        _idempotencyContext = idempotencyContext;
    }

    public Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = _repository.GetById(id);
        return Task.FromResult(order == null ? null : MapToResponse(order));
    }

    public Task<IReadOnlyList<OrderResponse>> GetOrdersAsync(
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _repository.GetAll().AsEnumerable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var pagedOrders = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<OrderResponse>>(pagedOrders);
    }

    public Task<PagedResult<OrderResponse>> SearchAsync(OrderSearchRequest request, CancellationToken cancellationToken)
    {
        var query = _repository.GetAll().AsEnumerable();

        if (request.Status.HasValue)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerReference))
        {
            query = query.Where(o => o.CustomerReference.Contains(request.CustomerReference, StringComparison.OrdinalIgnoreCase));
        }

        var matches = query.ToList();
        var items = matches
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(MapToResponse)
            .ToList();

        // No match is still a successful search: an empty page, not a 404
        return Task.FromResult(new PagedResult<OrderResponse>(items, matches.Count, request.PageNumber, request.PageSize));
    }

    public Task<OrderResponse?> CreateAsync(CreateOrderRequest request, string idempotencyKey, CancellationToken cancellationToken)
    {
        // The whole request identifies a retry; warehouse codes compare without regard to case
        var requestHash = _idempotencyContext.CreateRequestHash("create-order|" + JsonSerializer.Serialize(
            request with { FulfillmentWarehouseCode = request.FulfillmentWarehouseCode?.ToUpperInvariant() }));

        // As for stock adjustments (Chapter 6), the key check, the new order, and the stored result are one step
        lock (CreateLock)
        {
            if (_idempotencyContext.TryGetEntry(idempotencyKey, out var existingEntry))
            {
                // A retry gets the order its first attempt created; the same key with another payload gets null
                return Task.FromResult(existingEntry.RequestHash == requestHash
                    ? (OrderResponse?)existingEntry.Response
                    : null);
            }

            return Task.FromResult<OrderResponse?>(CreateOrder(request, idempotencyKey, requestHash));
        }
    }

    private OrderResponse CreateOrder(CreateOrderRequest request, string idempotencyKey, string requestHash)
    {
        // [ApiController] has already rejected a request without a customer reference or lines
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
                RequestedQuantity = l.Quantity,
                ReservedQuantity = 0,
                ShippedQuantity = 0
            }).ToList()
        };

        // Creating an order reserves no stock; from Chapter 9 on, allocating it does

        _repository.Add(order);

        // A failed request stores nothing, so only a created order answers the key's retries
        var response = MapToResponse(order);
        _idempotencyContext.Store(idempotencyKey, response, 201, requestHash);
        return response;
    }

    public Task<OrderResponse?> UpdateStatusAsync(Guid id, OrderStatus newStatus, CancellationToken cancellationToken)
    {
        while (_repository.GetById(id) is { } order)
        {
            var currentStatus = order.Status;

            if (!IsValidStatusTransition(currentStatus, newStatus))
            {
                throw new InvalidOperationException($"Cannot transition from {currentStatus} to {newStatus}");
            }

            // Fails only if another request changed the status after it was read; then read it again
            if (_repository.TryUpdateStatus(id, currentStatus, newStatus))
            {
                return Task.FromResult<OrderResponse?>(MapToResponse(order));
            }
        }

        return Task.FromResult<OrderResponse?>(null);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_repository.Remove(id));
    }

    private static bool IsValidStatusTransition(OrderStatus current, OrderStatus next)
    {
        if (current == next) return true; // Idempotent same-state handling

        return (current, next) switch
        {
            (OrderStatus.Pending, OrderStatus.Allocated) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Allocated, OrderStatus.Fulfilled) => true,
            (OrderStatus.Allocated, OrderStatus.Cancelled) => true,
            _ => false
        };
    }

    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse(
            order.Id,
            order.CustomerReference,
            order.Status,
            order.CreatedDate,
            order.FulfillmentWarehouseCode,
            order.Lines.Select(l => new OrderLineResponse(
                l.Sku,
                l.RequestedQuantity,
                l.ReservedQuantity,
                l.ShippedQuantity
            )).ToList()
        );
    }
}
