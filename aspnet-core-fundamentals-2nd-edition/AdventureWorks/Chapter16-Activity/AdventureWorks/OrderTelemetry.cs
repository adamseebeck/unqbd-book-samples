using System.Diagnostics;
using System.Diagnostics.Metrics;
using AdventureWorks.Core;

namespace AdventureWorks;

// The order flow's own signals: a span per service call, a count
// of created orders and how long each call took. OpenTelemetry
// (Observability.cs) collects them by these names.
public sealed class OrderTelemetry
{
    public const string Name = "AdventureWorks.Orders";

    public ActivitySource Source { get; } = new(Name);
    public Counter<long> Created { get; }
    public Histogram<double> Duration { get; }
    public Counter<long> Cancelled { get; }

    public OrderTelemetry(IMeterFactory meters)
    {
        var meter = meters.Create(Name);
        Created = meter.CreateCounter<long>(
            "orders.created", unit: "{order}");
        Duration = meter.CreateHistogram<double>(
            "orders.processing.duration", unit: "s");
        Cancelled = meter.CreateCounter<long>(
            "orders.cancelled", unit: "{order}");
    }
}

// Chapter 11's decorator chain, one link longer: this one wraps
// the logging decorator and records the signals.
public sealed class MeteredOrderService(IOrderService inner,
    OrderTelemetry telemetry) : IOrderService
{
    public Task<OrderResult> CreateAsync(string ownerId,
        CreateOrderRequest request,
        CancellationToken ct = default) =>
        TrackAsync("create", () =>
            inner.CreateAsync(ownerId, request, ct));

    public Task<OrderResult> CancelAsync(string ownerId, int id,
        CancellationToken ct = default) =>
        TrackAsync("cancel", () =>
            inner.CancelAsync(ownerId, id, ct));

    public Task<OrderResult> UpdateNotesAsync(string ownerId,
        int id, string? notes, Guid version,
        CancellationToken ct = default) =>
        TrackAsync("update", () => inner.UpdateNotesAsync(
            ownerId, id, notes, version, ct));

    public Task<OrderPage> ListAsync(string ownerId, int page,
        int pageSize, CancellationToken ct = default) =>
        inner.ListAsync(ownerId, page, pageSize, ct);

    async Task<OrderResult> TrackAsync(string operation,
        Func<Task<OrderResult>> call)
    {
        using var span = telemetry.Source.StartActivity(
            "orders." + operation);
        var start = Stopwatch.GetTimestamp();
        var result = await call();
        TagList tags = new()
        {
            { "operation", operation },
            { "outcome", result.Outcome.ToString() }
        };
        telemetry.Duration.Record(
            Stopwatch.GetElapsedTime(start).TotalSeconds, tags);
        if (result.Outcome == OrderOutcome.Created)
            telemetry.Created.Add(1);
        if (result.Outcome == OrderOutcome.Cancelled)
            telemetry.Cancelled.Add(1);
        span?.SetTag("order.outcome", result.Outcome.ToString());
        span?.SetTag("order.number", result.Order?.OrderNumber);
        return result;
    }
}
