using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Contracts;
using WarehouseApi.Exceptions;
using WarehouseApi.Mappings;
using WarehouseApi.Models;
using WarehouseApi.Persistence;
using WarehouseApi.Security;

namespace WarehouseApi.Services;

public class EfOrderService : IOrderService
{
    // A stored result answers retries for 24 hours, as for stock adjustments
    private static readonly TimeSpan IdempotencyWindow = TimeSpan.FromHours(24);
    private readonly WarehouseDbContext _context;
    private readonly IInventoryService _inventoryService;

    public EfOrderService(WarehouseDbContext context, IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    public async Task<OrderResponse?> GetByIdAsync(Guid id, OrderReadScope scope, CancellationToken cancellationToken)
    {
        // An order outside the caller's scope is not found, the same answer as an order that does not exist
        var order = await Readable(scope)
            .Include(o => o.Lines.OrderBy(l => l.Id))
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return order?.ToResponse();
    }

    public async Task<IReadOnlyList<OrderResponse>> GetOrdersAsync(
        OrderStatus? status, int page, int pageSize, OrderReadScope scope, CancellationToken cancellationToken)
    {
        var query = Readable(scope);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        // The ID breaks ties between orders created at the same moment, so every page is stable
        var orders = await query
            .OrderBy(o => o.CreatedDate)
            .ThenBy(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.Lines.OrderBy(l => l.Id))
            .ToListAsync(cancellationToken);

        return orders.Select(o => o.ToResponse()).ToList();
    }

    public async Task<PagedResult<OrderResponse>> GetOrderPageAsync(
        OrderStatus? status, PaginationParameters pagination, OrderReadScope scope, CancellationToken cancellationToken)
    {
        var query = Readable(scope);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        // The total lets V2 clients page through the orders like the products and the stock;
        // the ID breaks ties between orders created at the same moment
        var totalCount = await query.CountAsync(cancellationToken);
        var orders = await query
            .OrderBy(o => o.CreatedDate)
            .ThenBy(o => o.Id)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .Include(o => o.Lines.OrderBy(l => l.Id))
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderResponse>(
            orders.Select(o => o.ToResponse()).ToList(), totalCount, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<OrderResponse?> CreateAsync(CreateOrderRequest request, string idempotencyKey, string? createdBy, CancellationToken cancellationToken)
    {
        // The caller and the whole request identify a retry: the same key from another caller is another
        // request, so it gets 422, never this caller's order. Warehouse codes compare without regard to case
        var requestHash = CreateRequestHash("create-order|" + JsonSerializer.Serialize(new
        {
            CreatedBy = createdBy,
            Request = request with { FulfillmentWarehouseCode = request.FulfillmentWarehouseCode?.ToUpperInvariant() }
        }));

        var record = await _context.IdempotencyRecords.FindAsync([idempotencyKey], cancellationToken);
        if (record is not null && IsCurrent(record))
        {
            return Replay(record, requestHash);
        }

        // Validation has checked the request's shape; these rules need the stored data
        var errors = new Dictionary<string, string[]>();

        for (var i = 0; i < request.Lines.Count; i++)
        {
            var sku = request.Lines[i].Sku;
            if (!await _context.Products.AnyAsync(p => p.Sku == sku && p.IsActive, cancellationToken))
            {
                errors[$"Lines[{i}].Sku"] = [$"Product {sku} does not exist or is not active."];
            }
        }

        if (request.FulfillmentWarehouseCode is { } code &&
            !await _context.Warehouses.AnyAsync(w => w.Code == code, cancellationToken))
        {
            errors[nameof(request.FulfillmentWarehouseCode)] = [$"Warehouse {code} does not exist."];
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("The order refers to products or a warehouse that cannot be used.", errors);
        }

        var order = new Order
        {
            CustomerReference = request.CustomerReference,
            Status = OrderStatus.Pending,
            CreatedDate = DateTime.UtcNow,
            FulfillmentWarehouseCode = request.FulfillmentWarehouseCode,
            CreatedBy = createdBy,
            Lines = request.Lines.Select(l => new OrderLine
            {
                Sku = l.Sku,
                RequestedQuantity = l.Quantity
            }).ToList()
        };

        // Add assigns the Guid keys; EF Core generates sequential values, so lines sort in the order added
        _context.Orders.Add(order);

        // A new key gets a row; an expired key's row is reused
        var response = order.ToResponse();
        record ??= _context.IdempotencyRecords.Add(new IdempotencyRecord { Key = idempotencyKey }).Entity;
        record.RequestHash = requestHash;
        record.StatusCode = 201;
        record.ResponseBody = JsonSerializer.Serialize(response);
        record.CreatedAt = DateTime.UtcNow;

        try
        {
            // One SaveChangesAsync call: the order, its lines, and the stored result commit together
            await _context.SaveChangesAsync(cancellationToken);
            return response;
        }
        catch (DbUpdateException ex) when (SqlServerErrors.IsDuplicateKey(ex))
        {
            // Another request with this key stored its order first; nothing from this attempt was saved
            _context.ChangeTracker.Clear();

            var winner = await _context.IdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Key == idempotencyKey, cancellationToken);

            if (winner is not null && IsCurrent(winner))
            {
                return Replay(winner, requestHash);
            }

            throw;
        }
    }

    public async Task<OrderResponse> FulfillAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            // The shipments and the order commit together, or not at all
            return await _context.RunInTransactionAsync(async ct =>
            {
                var order = await LoadOrderAsync(id, ct);

                if (order.Status == OrderStatus.Fulfilled)
                {
                    return order.ToResponse();
                }

                EnsureStatus(order, OrderStatus.Allocated, "fulfilled");

                foreach (var line in order.Lines)
                {
                    await _inventoryService.ShipStockAsync(order.FulfillmentWarehouseCode!, line.Sku, line.ReservedQuantity, order.Id.ToString(), ct);
                }

                foreach (var line in order.Lines)
                {
                    line.ShippedQuantity = line.ReservedQuantity;
                    line.ReservedQuantity = 0;
                }

                order.Status = OrderStatus.Fulfilled;
                await _context.SaveChangesAsync(ct);
                return order.ToResponse();
            }, IsolationLevel.ReadCommitted, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw ChangedByAnotherRequest(id, ex);
        }
    }

    public async Task<OrderResponse> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            // The releases and the order commit together, or not at all
            return await _context.RunInTransactionAsync(async ct =>
            {
                var order = await LoadOrderAsync(id, ct);

                if (order.Status == OrderStatus.Cancelled)
                {
                    return order.ToResponse();
                }

                if (order.Status == OrderStatus.Fulfilled)
                {
                    throw new ConflictException($"Order {id} is Fulfilled and cannot be cancelled.", "INVALID_ORDER_STATE");
                }

                if (order.Status == OrderStatus.Allocated)
                {
                    foreach (var line in order.Lines)
                    {
                        await _inventoryService.ReleaseStockAsync(order.FulfillmentWarehouseCode!, line.Sku, line.ReservedQuantity, order.Id.ToString(), ct);
                    }
                }

                foreach (var line in order.Lines)
                {
                    line.ReservedQuantity = 0;
                }

                order.Status = OrderStatus.Cancelled;
                await _context.SaveChangesAsync(ct);
                return order.ToResponse();
            }, IsolationLevel.ReadCommitted, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw ChangedByAnotherRequest(id, ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return false;
        }

        // An allocated order holds stock and a fulfilled order is history; neither may vanish
        if (order.Status is OrderStatus.Allocated or OrderStatus.Fulfilled)
        {
            throw new ConflictException($"Order {id} is {order.Status}. Only pending or cancelled orders can be deleted.", "INVALID_ORDER_STATE");
        }

        try
        {
            // The database deletes the order's lines with it (cascade delete)
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw ChangedByAnotherRequest(id, ex);
        }
    }

    private IQueryable<Order> Readable(OrderReadScope scope)
    {
        var orders = _context.Orders.AsNoTracking();

        return scope.AllOrders
            ? orders
            : orders.Where(o => (scope.WarehouseCode != null && o.FulfillmentWarehouseCode == scope.WarehouseCode)
                || (scope.CreatedBy != null && o.CreatedBy == scope.CreatedBy));
    }

    private async Task<Order> LoadOrderAsync(Guid id, CancellationToken cancellationToken) =>
        await _context.Orders
            .Include(o => o.Lines.OrderBy(l => l.Id))
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
        ?? throw new OrderNotFoundException(id);

    // A lost-update conflict becomes a 409 Conflict with a reason clients can test for
    private static ConflictException ChangedByAnotherRequest(Guid id, Exception inner) =>
        new($"Order {id} or its stock was changed by another request. Read them again and retry.", "CONCURRENCY_CONFLICT", inner);

    private static void EnsureStatus(Order order, OrderStatus expected, string action)
    {
        if (order.Status != expected)
        {
            throw new ConflictException($"Order {order.Id} is {order.Status} and cannot be {action}.", "INVALID_ORDER_STATE");
        }
    }

    private static bool IsCurrent(IdempotencyRecord record) =>
        record.CreatedAt > DateTime.UtcNow - IdempotencyWindow;

    // The stored order answers a retry; the same key with another payload or caller gets null (422)
    private static OrderResponse? Replay(IdempotencyRecord record, string requestHash) =>
        record.RequestHash == requestHash
            ? JsonSerializer.Deserialize<OrderResponse>(record.ResponseBody)
            : null;

    private static string CreateRequestHash(string requestSummary) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(requestSummary)));
}
