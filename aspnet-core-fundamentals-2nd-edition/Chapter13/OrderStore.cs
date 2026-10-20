using System.Collections.Concurrent;

public interface IOrderService
{
    PagedResponse<OrderDto> GetPage(PaginationRequest request);
    OrderDto? Get(Guid id);
    OrderDto Create(CreateOrderRequest request);
    bool Update(Guid id, UpdateOrderRequest request);
    bool Delete(Guid id);
}

public sealed class OrderService : IOrderService
{
    private readonly ConcurrentDictionary<Guid, OrderDto> _orders = new();

    public PagedResponse<OrderDto> GetPage(PaginationRequest request)
    {
        IEnumerable<OrderDto> query = _orders.Values;
        if (!string.IsNullOrWhiteSpace(request.CustomerName))
            query = query.Where(o => o.CustomerName.Contains(request.CustomerName,
                StringComparison.OrdinalIgnoreCase));
        var ordered = request.SortDirection == "desc"
            ? query.OrderByDescending(o => o.CustomerName).ThenBy(o => o.Id)
            : query.OrderBy(o => o.CustomerName).ThenBy(o => o.Id);
        var snapshot = ordered.ToArray();
        return new(snapshot.Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize).ToArray(), request.PageNumber, request.PageSize,
            snapshot.Length);
    }

    public OrderDto? Get(Guid id) => _orders.GetValueOrDefault(id);

    public OrderDto Create(CreateOrderRequest request)
    {
        var order = new OrderDto(Guid.NewGuid(), request.CustomerName.Trim(),
            DateTimeOffset.UtcNow, request.Items.ToArray());
        _orders[order.Id] = order;
        return order;
    }

    public bool Update(Guid id, UpdateOrderRequest request)
    {
        while (_orders.TryGetValue(id, out var existing))
        {
            var updated = existing with
            {
                CustomerName = request.CustomerName?.Trim() ?? existing.CustomerName,
                OrderDate = request.OrderDate ?? existing.OrderDate
            };
            if (_orders.TryUpdate(id, updated, existing))
                return true;
        }
        return false;
    }

    public bool Delete(Guid id) => _orders.TryRemove(id, out _);
}
