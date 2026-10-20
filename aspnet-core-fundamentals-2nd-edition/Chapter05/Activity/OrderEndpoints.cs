using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
public sealed class CreateOrderRequest
{
    [Required, StringLength(100)] public string CustomerName { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "100000")] public decimal Total { get; set; }
}
public sealed record OrderDto(int Id, string CustomerName, decimal Total);
public sealed class OrderStore
{
    private int _nextId;
    private readonly ConcurrentDictionary<int, OrderDto> _orders = new();
    public OrderDto Create(CreateOrderRequest request)
    {
        var result = new OrderDto(Interlocked.Increment(ref _nextId), request.CustomerName, request.Total);
        _orders[result.Id] = result;
        return result;
    }
    public OrderDto? Find(int id) => _orders.GetValueOrDefault(id);
}
public sealed class OrderTimingFilter(ILogger<OrderTimingFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var started = Stopwatch.GetTimestamp();
        try { return await next(context); }
        finally { logger.LogInformation("Order exercise elapsed {Ms} ms", Stopwatch.GetElapsedTime(started).TotalMilliseconds); }
    }
}
public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders").AddEndpointFilter<OrderTimingFilter>();
        group.MapGet("/{id:int}", Results<Ok<OrderDto>, NotFound> (int id, OrderStore store) =>
            store.Find(id) is { } order ? TypedResults.Ok(order) : TypedResults.NotFound()).WithName("ActivityGetOrder");
        group.MapPost("/", (CreateOrderRequest request, OrderStore store) =>
        {
            var order = store.Create(request);
            return TypedResults.CreatedAtRoute(order, "ActivityGetOrder", new { id = order.Id });
        }).AddEndpointFilter<ValidationFilter<CreateOrderRequest>>().ProducesValidationProblem();
        return app;
    }
}
