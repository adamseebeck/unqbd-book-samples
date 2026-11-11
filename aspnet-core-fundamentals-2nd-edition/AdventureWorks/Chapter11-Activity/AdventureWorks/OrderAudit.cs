using System.Collections.Concurrent;
using AdventureWorks.Authorization;
using AdventureWorks.Core;

namespace AdventureWorks;

public sealed record OrderAuditEntry(DateTimeOffset At,
    string Operation, string Outcome, string? OrderNumber);

// The last 20 order outcomes, kept for the life of the app: a
// singleton, safe for many requests at once.
public sealed class OrderAudit(TimeProvider clock)
{
    private readonly ConcurrentQueue<OrderAuditEntry> _entries
        = new();
    private readonly object _gate = new();

    public void Record(string operation, OrderResult result)
    {
        lock (_gate)
        {
            _entries.Enqueue(new(clock.GetUtcNow(), operation,
                result.Outcome.ToString(),
                result.Order?.OrderNumber));
            while (_entries.Count > 20) _entries.TryDequeue(out _);
        }
    }

    public OrderAuditEntry[] Recent()
    {
        lock (_gate)
            return [.. _entries.Reverse()];
    }
}

// A second decorator. It wraps a scoped service, so it is scoped;
// it may use the singleton OrderAudit.
public sealed class AuditingOrderService(IOrderService inner,
    OrderAudit audit) : IOrderService
{
    public async Task<OrderResult> CreateAsync(string ownerId,
        CreateOrderRequest request,
        CancellationToken ct = default)
    {
        var result =
            await inner.CreateAsync(ownerId, request, ct);
        audit.Record("Create", result);
        return result;
    }

    public async Task<OrderResult> CancelAsync(string ownerId,
        int id, CancellationToken ct = default)
    {
        var result = await inner.CancelAsync(ownerId, id, ct);
        audit.Record("Cancel", result);
        return result;
    }
}

public static class OrderAuditEndpoints
{
    public static IEndpointRouteBuilder MapOrderAudit(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/orders/audit",
                (OrderAudit audit) => audit.Recent())
            .WithTags("Orders")
            .RequireAuthorization(StorePolicies.AdminOnly);
        return app;
    }
}
