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
        // JSON can contain null even with a nonnullable item type.
        if (request.Items.Any(item => item is null))
            return Invalid("Each order line is required.");
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
                products[line.ProductId].Price))];
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
        return Saved(OrderOutcome.Created,
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
        // False: the order was deleted after the read above.
        if (!await writer.SetStatusAsync(
                id, OrderStatus.Cancelled, order.Version, ct))
            return new(OrderOutcome.NotFound);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (OrderDeletedException)
        {
            return new(OrderOutcome.NotFound);
        }
        catch (OrderChangedException)
        {
            return new(OrderOutcome.Conflict, Error:
                "The order changed; read it again.");
        }
        return Saved(OrderOutcome.Cancelled,
            await orders.GetByIdAsync(id));
    }

    public async Task<OrderPage> ListAsync(string ownerId,
        int page, int pageSize) =>
        new(await orders.ListAsync(ownerId, page, pageSize),
            await orders.CountAsync(ownerId));

    // The same staging and version check as CancelAsync, for the
    // notes the customer may change.
    public async Task<OrderResult> UpdateNotesAsync(
        string ownerId, int id, string? notes, Guid version,
        CancellationToken ct = default)
    {
        var order = await orders.GetByIdAsync(id);
        if (order is null || order.OwnerId != ownerId)
            return new(OrderOutcome.NotFound);
        if (!await writer.SetNotesAsync(id, notes, version, ct))
            return new(OrderOutcome.NotFound);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (OrderDeletedException)
        {
            return new(OrderOutcome.NotFound);
        }
        catch (OrderChangedException)
        {
            return new(OrderOutcome.Conflict, Error:
                "The order changed; read it again.");
        }
        return Saved(OrderOutcome.Updated,
            await orders.GetByIdAsync(id));
    }

    static OrderResult Invalid(string error) =>
        new(OrderOutcome.Invalid, Error: error);

    // The order read back after a save, for the response. Null:
    // it was deleted since, so the caller gets NotFound.
    static OrderResult Saved(OrderOutcome outcome,
        Order? order) =>
        order is null
            ? new(OrderOutcome.NotFound) : new(outcome, order);
}
