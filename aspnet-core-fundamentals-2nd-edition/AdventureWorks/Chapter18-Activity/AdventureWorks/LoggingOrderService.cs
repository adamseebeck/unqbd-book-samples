using System.Diagnostics;
using AdventureWorks.Core;

namespace AdventureWorks;

// A decorator: the same interface, wrapped around OrderService,
// adds a log line for each outcome without touching the rules.
public sealed class LoggingOrderService(IOrderService inner,
    ILogger<LoggingOrderService> logger) : IOrderService
{
    public Task<OrderResult> CreateAsync(string ownerId,
        CreateOrderRequest request,
        CancellationToken ct = default) =>
        LogAsync("Create", () =>
            inner.CreateAsync(ownerId, request, ct));

    public Task<OrderResult> CancelAsync(string ownerId, int id,
        CancellationToken ct = default) =>
        LogAsync("Cancel", () =>
            inner.CancelAsync(ownerId, id, ct));

    public Task<OrderPage> ListAsync(string ownerId, int page,
        int pageSize) =>
        inner.ListAsync(ownerId, page, pageSize);

    public Task<OrderResult> UpdateNotesAsync(string ownerId,
        int id, string? notes, Guid version,
        CancellationToken ct = default) =>
        LogAsync("Update", () => inner.UpdateNotesAsync(
            ownerId, id, notes, version, ct));

    async Task<OrderResult> LogAsync(string operation,
        Func<Task<OrderResult>> call)
    {
        var timer = Stopwatch.StartNew();
        var result = await call();
        logger.LogInformation(
            "{Operation} order: {Outcome} in {ElapsedMs} ms",
            operation, result.Outcome, timer.ElapsedMilliseconds);
        return result;
    }
}
