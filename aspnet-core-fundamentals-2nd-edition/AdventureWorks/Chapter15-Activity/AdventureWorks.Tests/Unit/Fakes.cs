using AdventureWorks.Core;

namespace AdventureWorks.Tests.Unit;

// Hand-written stand-ins for the contracts OrderService uses.
// The writer and the repository share one list, as the real pair
// share one DbContext.
public sealed class FakeOrders : IOrderRepository, IOrderWriter
{
    public List<Order> Saved { get; } = [];
    public string? StagedStatus { get; private set; }

    public Task<Order?> GetByIdAsync(int id) =>
        Task.FromResult(Saved.FirstOrDefault(o => o.Id == id));

    public Task<Order?> GetByNumberAsync(string orderNumber) =>
        Task.FromResult(Saved.FirstOrDefault(o =>
            o.OrderNumber == orderNumber));

    public Task<OrderSummary[]> ListAsync(
        string ownerId, int page, int pageSize) =>
        Task.FromResult(Array.Empty<OrderSummary>());

    public Task<int> CountAsync(string ownerId) =>
        Task.FromResult(Saved.Count(o => o.OwnerId == ownerId));

    public Task<bool> ArchiveAsync(int id) =>
        Task.FromResult(false);

    public Task<bool> DeleteAsync(int id) =>
        Task.FromResult(false);

    public Task<string> NextOrderNumberAsync(
        CancellationToken ct) =>
        Task.FromResult($"AW-{Saved.Count + 101:0000}");

    public void Add(NewOrder order) => Saved.Add(new Order(
        Saved.Count + 101, order.OrderNumber, order.OwnerId,
        order.Items.Sum(i => i.Quantity * i.UnitPrice),
        OrderStatus.Pending, false, null, Guid.NewGuid(),
        order.Items));

    public Task<bool> SetStatusAsync(int id, string status,
        Guid version, CancellationToken ct)
    {
        StagedStatus = status;
        return Task.FromResult(true);
    }

    public Task<bool> SetNotesAsync(int id, string? notes,
        Guid version, CancellationToken ct) =>
        Task.FromResult(true);
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }
    public Exception? Failure { get; set; }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        Saves++;
        return Failure is null
            ? Task.CompletedTask : Task.FromException(Failure);
    }
}
