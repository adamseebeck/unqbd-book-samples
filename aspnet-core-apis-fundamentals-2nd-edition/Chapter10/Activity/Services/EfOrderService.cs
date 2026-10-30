using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Contracts;
using WarehouseApi.Mappings;
using WarehouseApi.Models;
using WarehouseApi.Persistence;

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

    public async Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Lines.OrderBy(l => l.Id))
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return order?.ToResponse();
    }

    public async Task<IReadOnlyList<OrderResponse>> GetOrdersAsync(OrderStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Orders.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var orders = await query
            .OrderBy(o => o.CreatedDate)
            .ThenBy(o => o.Id) // the ID breaks ties, so orders created at the same moment keep their page
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.Lines.OrderBy(l => l.Id))
            .ToListAsync(cancellationToken);

        return orders.Select(o => o.ToResponse()).ToList();
    }

    public async Task<OrderResponse?> CreateAsync(CreateOrderRequest request, string idempotencyKey, CancellationToken cancellationToken)
    {
        // The whole request identifies a retry; warehouse codes compare without regard to case
        var requestHash = CreateRequestHash("create-order|" + JsonSerializer.Serialize(
            request with { FulfillmentWarehouseCode = request.FulfillmentWarehouseCode?.ToUpperInvariant() }));

        var record = await _context.IdempotencyRecords.FindAsync([idempotencyKey], cancellationToken);
        if (record is not null && IsCurrent(record))
        {
            return Replay(record, requestHash);
        }

        // Validation has checked the request's shape; these rules need the stored data
        foreach (var line in request.Lines)
        {
            if (!await _context.Products.AnyAsync(p => p.Sku == line.Sku && p.IsActive, cancellationToken))
            {
                throw new ArgumentException($"Product {line.Sku} does not exist or is not active.");
            }
        }

        if (request.FulfillmentWarehouseCode is { } code &&
            !await _context.Warehouses.AnyAsync(w => w.Code == code, cancellationToken))
        {
            throw new ArgumentException($"Warehouse {code} does not exist.");
        }

        var order = new Order
        {
            CustomerReference = request.CustomerReference,
            Status = OrderStatus.Pending,
            CreatedDate = DateTime.UtcNow,
            FulfillmentWarehouseCode = request.FulfillmentWarehouseCode,
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

    public async Task<OrderResponse> AllocateAsync(Guid id, string warehouseCode, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(id, cancellationToken);

        if (order.Status == OrderStatus.Allocated
            && string.Equals(order.FulfillmentWarehouseCode, warehouseCode, StringComparison.OrdinalIgnoreCase))
        {
            return order.ToResponse(); // A repeated request changes nothing
        }

        EnsureStatus(order, OrderStatus.Pending, "allocated");

        // The stored code: an allocation sent with wh-001 records WH-001
        var code = await _context.Warehouses
            .Where(w => w.Code == warehouseCode && w.IsOperational)
            .Select(w => w.Code)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ArgumentException($"Warehouse {warehouseCode} does not exist or is not operational.");

        // Each reservation is saved on its own; if a later step fails, the earlier ones are undone
        var reserved = new List<OrderLine>();
        try
        {
            foreach (var line in order.Lines)
            {
                await _inventoryService.ReserveStockAsync(code, line.Sku, line.RequestedQuantity, cancellationToken);
                reserved.Add(line);
            }

            foreach (var line in order.Lines)
            {
                line.ReservedQuantity = line.RequestedQuantity;
            }

            order.FulfillmentWarehouseCode = code;
            order.Status = OrderStatus.Allocated;

            // The order's RowVersion makes this save fail if another request changed the order meanwhile
            await _context.SaveChangesAsync(cancellationToken);
            return order.ToResponse();
        }
        catch (Exception ex)
        {
            // A failed save leaves its changes in the change tracker; drop them before undoing
            _context.ChangeTracker.Clear();

            foreach (var line in reserved)
            {
                // Finish the undo even if the client has gone away
                await _inventoryService.ReleaseStockAsync(code, line.Sku, line.RequestedQuantity, CancellationToken.None);
            }

            if (ex is DbUpdateConcurrencyException)
            {
                throw ChangedByAnotherRequest(id, ex);
            }

            throw;
        }
    }

    public async Task<OrderResponse> FulfillAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(id, cancellationToken);

        if (order.Status == OrderStatus.Fulfilled)
        {
            return order.ToResponse();
        }

        EnsureStatus(order, OrderStatus.Allocated, "fulfilled");

        try
        {
            // Each shipment is saved on its own, then the order
            foreach (var line in order.Lines)
            {
                await _inventoryService.ShipStockAsync(order.FulfillmentWarehouseCode!, line.Sku, line.ReservedQuantity, cancellationToken);
            }

            foreach (var line in order.Lines)
            {
                line.ShippedQuantity = line.ReservedQuantity;
                line.ReservedQuantity = 0;
            }

            order.Status = OrderStatus.Fulfilled;
            await _context.SaveChangesAsync(cancellationToken);
            return order.ToResponse();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw ChangedByAnotherRequest(id, ex);
        }
    }

    public async Task<OrderResponse> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(id, cancellationToken);

        if (order.Status == OrderStatus.Cancelled)
        {
            return order.ToResponse();
        }

        if (order.Status == OrderStatus.Fulfilled)
        {
            throw new InvalidOperationException($"Order {id} is Fulfilled and cannot be cancelled.");
        }

        try
        {
            if (order.Status == OrderStatus.Allocated)
            {
                foreach (var line in order.Lines)
                {
                    await _inventoryService.ReleaseStockAsync(order.FulfillmentWarehouseCode!, line.Sku, line.ReservedQuantity, cancellationToken);
                }
            }

            foreach (var line in order.Lines)
            {
                line.ReservedQuantity = 0;
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync(cancellationToken);
            return order.ToResponse();
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
            throw new InvalidOperationException($"Order {id} is {order.Status}. Only pending or cancelled orders can be deleted.");
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

    private async Task<Order> LoadOrderAsync(Guid id, CancellationToken cancellationToken) =>
        await _context.Orders
            .Include(o => o.Lines.OrderBy(l => l.Id))
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
        ?? throw new KeyNotFoundException($"Order {id} not found.");

    // A lost-update conflict becomes the exception the controller answers with 409 Conflict
    private static InvalidOperationException ChangedByAnotherRequest(Guid id, Exception inner) =>
        new($"Order {id} or its stock was changed by another request. Read them again and retry.", inner);

    private static void EnsureStatus(Order order, OrderStatus expected, string action)
    {
        if (order.Status != expected)
        {
            throw new InvalidOperationException($"Order {order.Id} is {order.Status} and cannot be {action}.");
        }
    }

    private static bool IsCurrent(IdempotencyRecord record) =>
        record.CreatedAt > DateTime.UtcNow - IdempotencyWindow;

    // The stored order answers a retry; the same key with another payload gets null (422)
    private static OrderResponse? Replay(IdempotencyRecord record, string requestHash) =>
        record.RequestHash == requestHash
            ? JsonSerializer.Deserialize<OrderResponse>(record.ResponseBody)
            : null;

    private static string CreateRequestHash(string requestSummary) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(requestSummary)));
}
