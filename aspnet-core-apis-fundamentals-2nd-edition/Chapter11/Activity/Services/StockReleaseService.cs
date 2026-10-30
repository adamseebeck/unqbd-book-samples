using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Contracts;
using WarehouseApi.Mappings;
using WarehouseApi.Models;
using WarehouseApi.Persistence;
using WarehouseApi.Telemetry;

namespace WarehouseApi.Services;

public class StockReleaseService : IStockReleaseService
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan IdempotencyWindow = TimeSpan.FromHours(24);

    private readonly WarehouseDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<StockReleaseService> _logger;

    public StockReleaseService(
        WarehouseDbContext context,
        IInventoryService inventoryService,
        ILogger<StockReleaseService> logger)
    {
        _context = context;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task<OrderResponse?> ReleaseAsync(ReleaseRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        using var activity = WarehouseTelemetry.Source.StartActivity(WarehouseTelemetry.ActivityNames.ReleaseStock);
        activity?.SetTag(WarehouseTelemetry.Tags.OrderId, request.OrderId.ToString());
        activity?.SetTag(WarehouseTelemetry.Tags.Sku, request.Sku);

        // The correlation middleware's scope already adds the correlation ID to these entries
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            // Validation has already rejected a body without an order ID
            ["OrderId"] = request.OrderId!.Value
        });

        var delay = InitialDelay;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var order = await ReleaseOnceAsync(request, idempotencyKey, cancellationToken);

                activity?.SetTag(WarehouseTelemetry.Tags.RetryCount, attempt - 1);
                _logger.LogInformation(
                    "Released {Quantity} units of {Sku} for order {OrderId} on attempt {Attempt}",
                    request.Quantity, request.Sku, request.OrderId, attempt);

                return order;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                activity?.SetTag(WarehouseTelemetry.Tags.RetryCount, attempt);
                _logger.LogWarning(
                    "Concurrency conflict releasing stock for order {OrderId}, attempt {Attempt} of {MaxAttempts}; retrying in {DelayMs} ms",
                    request.OrderId, attempt, MaxAttempts, delay.TotalMilliseconds);

                await Task.Delay(delay + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 25)), cancellationToken);
                delay *= 2;
            }
            catch (DbUpdateException ex) when (SqlServerErrors.IsDuplicateKey(ex) && attempt < MaxAttempts)
            {
                // A concurrent request with the same key stored its release first; the next attempt replays it
            }
            catch (DbUpdateConcurrencyException ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Concurrency conflict");
                throw new InvalidOperationException(
                    $"Stock for order {request.OrderId} kept changing during the release. Try again later.", ex);
            }
        }
    }

    private Task<OrderResponse?> ReleaseOnceAsync(ReleaseRequest request, string idempotencyKey, CancellationToken cancellationToken) =>
        _context.RunInTransactionAsync(async ct =>
        {
            // A release is not idempotent: repeating "release 3" would release three more units. The key's
            // record commits with the release, so a repeat by the client or by the execution strategy replays it
            var requestHash = CreateRequestHash("release|" + JsonSerializer.Serialize(request));
            var record = await _context.IdempotencyRecords.FindAsync([idempotencyKey], ct);
            if (record is not null && record.CreatedAt > DateTime.UtcNow - IdempotencyWindow)
            {
                return record.RequestHash == requestHash
                    ? JsonSerializer.Deserialize<OrderResponse>(record.ResponseBody)
                    : null;
            }

            // Preconditions first; the stock row is read and changed by the inventory service below
            var order = await _context.Orders
                .Include(o => o.Lines.OrderBy(l => l.Id))
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
                ?? throw new KeyNotFoundException($"Order {request.OrderId} not found.");

            var line = order.Lines.FirstOrDefault(l => l.Sku == request.Sku)
                ?? throw new KeyNotFoundException($"Order {order.Id} has no line for {request.Sku}.");

            if (order.Status != OrderStatus.Allocated)
            {
                throw new InvalidOperationException($"Order {order.Id} is {order.Status}; only an allocated order can release stock.");
            }

            if (request.Quantity >= line.ReservedQuantity)
            {
                throw new InvalidOperationException(
                    $"Order {order.Id} has {line.ReservedQuantity} units of {request.Sku} reserved. Release fewer, or cancel the order to release them all.");
            }

            // Records an ORDER_RELEASE history row with the correlation ID and the order as the reference
            await _inventoryService.ReleaseStockAsync(
                order.FulfillmentWarehouseCode!, line.Sku, request.Quantity, order.Id.ToString(), ct);

            line.ReservedQuantity -= request.Quantity;
            line.RequestedQuantity -= request.Quantity;

            // A change to a line is a change to the order. EF Core checks the order's RowVersion only when
            // it updates the order row, so mark the order modified: the save then checks and renews it
            _context.Entry(order).State = EntityState.Modified;

            // The stored result commits with the release; an expired key's row is reused
            var response = order.ToResponse();
            record ??= _context.IdempotencyRecords.Add(new IdempotencyRecord { Key = idempotencyKey }).Entity;
            record.RequestHash = requestHash;
            record.StatusCode = 200;
            record.ResponseBody = JsonSerializer.Serialize(response);
            record.CreatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return (OrderResponse?)response;
        }, IsolationLevel.ReadCommitted, cancellationToken);

    private static string CreateRequestHash(string requestSummary) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(requestSummary)));
}
