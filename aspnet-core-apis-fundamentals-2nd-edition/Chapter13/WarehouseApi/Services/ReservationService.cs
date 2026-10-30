using System.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Contracts;
using WarehouseApi.Exceptions;
using WarehouseApi.Mappings;
using WarehouseApi.Persistence;
using WarehouseApi.Telemetry;

namespace WarehouseApi.Services;

public class ReservationService : IReservationService
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMilliseconds(50);

    private readonly WarehouseDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(
        WarehouseDbContext context,
        IInventoryService inventoryService,
        ILogger<ReservationService> logger)
    {
        _context = context;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task<OrderResponse> ReserveAsync(ReservationRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = WarehouseTelemetry.Source.StartActivity(WarehouseTelemetry.ActivityNames.ReserveStock);
        activity?.SetTag(WarehouseTelemetry.Tags.OrderId, request.OrderId.ToString());
        activity?.SetTag(WarehouseTelemetry.Tags.Warehouse, request.WarehouseCode);

        // The correlation middleware's scope already adds the correlation ID to these entries
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["OrderId"] = request.OrderId
        });

        var delay = InitialDelay;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var order = await ReserveOnceAsync(request, cancellationToken);

                activity?.SetTag(WarehouseTelemetry.Tags.RetryCount, attempt - 1);
                _logger.LogInformation(
                    "Reserved stock for order {OrderId} in {WarehouseCode} on attempt {Attempt}",
                    request.OrderId, request.WarehouseCode, attempt);

                return order;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                // Another request changed a stock row or the order: wait, then read current values again
                activity?.SetTag(WarehouseTelemetry.Tags.RetryCount, attempt);
                _logger.LogWarning(
                    "Concurrency conflict reserving stock for order {OrderId}, attempt {Attempt} of {MaxAttempts}; retrying in {DelayMs} ms",
                    request.OrderId, attempt, MaxAttempts, delay.TotalMilliseconds);

                // The random part keeps competing requests from retrying in step
                await Task.Delay(delay + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 25)), cancellationToken);
                delay *= 2;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Concurrency conflict");
                _logger.LogWarning(ex,
                    "Concurrency conflict for order {OrderId} persisted after {MaxAttempts} attempts",
                    request.OrderId, MaxAttempts);

                throw new ConflictException(
                    $"Stock for order {request.OrderId} kept changing during the reservation. Try again later.", "CONCURRENCY_CONFLICT", ex);
            }
        }
    }

    private Task<OrderResponse> ReserveOnceAsync(ReservationRequest request, CancellationToken cancellationToken) =>
        _context.RunInTransactionAsync(async ct =>
        {
            var order = await _context.Orders
                .Include(o => o.Lines.OrderBy(l => l.Id))
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
                ?? throw new OrderNotFoundException(request.OrderId);

            if (order.Status == OrderStatus.Allocated
                && string.Equals(order.FulfillmentWarehouseCode, request.WarehouseCode, StringComparison.OrdinalIgnoreCase))
            {
                return order.ToResponse(); // A repeated request changes nothing
            }

            if (order.Status != OrderStatus.Pending)
            {
                throw new ConflictException($"Order {order.Id} is {order.Status} and cannot be allocated.", "INVALID_ORDER_STATE");
            }

            // The stored code: an allocation sent with wh-001 records WH-001
            var warehouseCode = await _context.Warehouses
                .Where(w => w.Code == request.WarehouseCode && w.IsOperational)
                .Select(w => w.Code)
                .FirstOrDefaultAsync(ct);

            if (warehouseCode is null)
            {
                throw new DomainValidationException(
                    "The order cannot be allocated to that warehouse.",
                    new Dictionary<string, string[]>
                    {
                        [nameof(request.WarehouseCode)] = [$"Warehouse {request.WarehouseCode} does not exist or is not operational."]
                    });
            }

            // Each call reads its stock row inside this transaction and checks availability again;
            // its SaveChangesAsync joins the transaction, so nothing commits until the end
            foreach (var line in order.Lines)
            {
                await _inventoryService.ReserveStockAsync(
                    warehouseCode, line.Sku, line.RequestedQuantity, order.Id.ToString(), ct);
                line.ReservedQuantity = line.RequestedQuantity;
            }

            order.FulfillmentWarehouseCode = warehouseCode;
            order.Status = OrderStatus.Allocated;

            // The order's RowVersion detects a request that changed the order after it was read
            await _context.SaveChangesAsync(ct);
            return order.ToResponse();
        }, IsolationLevel.ReadCommitted, cancellationToken);
}
