namespace AdventureWorks.Core;

// The order rules in one place, over Core contracts only, so the
// endpoints stay small and the rules can be tested without HTTP.
public sealed class OrderService(IOrderRepository orders,
    IOrderWriter writer, IProductService catalog,
    IUnitOfWork unitOfWork) : IOrderService
{
    public async Task<OrderResult> CreateAsync(string ownerId,
        CreateOrderRequest request,
        CancellationToken ct = default)
    {
        int[] ids = [.. request.Items.Select(i => i.ProductId)
            .Distinct()];
        if (ids.Length != request.Items.Count)
            return Invalid("Use one line per product.");
        var products = (await catalog.GetByIdsAsync(ids, ct))
            .ToDictionary(p => p.Id);
        if (products.Count != ids.Length)
            return Invalid("A product on the order is not sold.");
        // The catalog sets each price, whatever the caller sent.
        OrderItem[] items = [.. request.Items.Select(line =>
            new OrderItem(line.ProductId, line.Quantity,
                UnitPrice(products[line.ProductId].Price,
                    line.Quantity)))];
        var number = await writer.NextOrderNumberAsync(ct);
        writer.Add(new NewOrder(ownerId, number, items));
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (OrderNumberConflictException)
        {
            return new(OrderOutcome.Conflict, Error:
                "Another order took that number; send it again.");
        }
        return new(OrderOutcome.Created,
            await orders.GetByNumberAsync(number));
    }

    public async Task<OrderResult> CancelAsync(string ownerId,
        int id, CancellationToken ct = default)
    {
        var order = await orders.GetByIdAsync(id);
        if (order is null || order.OwnerId != ownerId)
            return new(OrderOutcome.NotFound);
        if (order.Status == OrderStatus.Cancelled)
            return Invalid("The order is already cancelled.");
        // Staged with the version just read; the save checks it.
        await writer.SetStatusAsync(
            id, OrderStatus.Cancelled, order.Version, ct);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (OrderChangedException)
        {
            return new(OrderOutcome.Conflict, Error:
                "The order changed; read it again.");
        }
        return new(OrderOutcome.Cancelled,
            await orders.GetByIdAsync(id));
    }

    public async Task<OrderPage> ListAsync(string ownerId,
        int page, int pageSize, CancellationToken ct = default) =>
        new(await orders.ListAsync(ownerId, page, pageSize), page,
            pageSize, await orders.CountAsync(ownerId));

    // The same staging and version check as CancelAsync, for the
    // notes the customer may change.
    public async Task<OrderResult> UpdateNotesAsync(
        string ownerId, int id, string? notes, Guid version,
        CancellationToken ct = default)
    {
        var order = await orders.GetByIdAsync(id);
        if (order is null || order.OwnerId != ownerId)
            return new(OrderOutcome.NotFound);
        await writer.SetNotesAsync(id, notes, version, ct);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (OrderChangedException)
        {
            return new(OrderOutcome.Conflict, Error:
                "The order changed; read it again.");
        }
        return new(OrderOutcome.Updated,
            await orders.GetByIdAsync(id));
    }

    // Debugging lab: ten or more of one product cost 10% less
    // each, rounded to the cent.
    const int DiscountQuantity = 10;

    static decimal UnitPrice(decimal price, int quantity)
    {
        var discounted = quantity > DiscountQuantity;
        if (!discounted) return price;
        return decimal.Round(price * 0.9m, 2);
    }

    static OrderResult Invalid(string error) =>
        new(OrderOutcome.Invalid, Error: error);
}
