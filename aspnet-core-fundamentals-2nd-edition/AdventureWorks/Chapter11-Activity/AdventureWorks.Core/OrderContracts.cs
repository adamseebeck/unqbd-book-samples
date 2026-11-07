using System.ComponentModel.DataAnnotations;

namespace AdventureWorks.Core;

public static class OrderStatus
{
    public const string Pending = "Pending";
    public const string Cancelled = "Cancelled";
}

// What a caller may send: products and quantities. The owner is
// the signed-in user, and prices come from the catalog.
public sealed class OrderLineRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 20)]
    public int Quantity { get; set; }
}

public sealed class CreateOrderRequest
{
    [Required, MinLength(1), MaxLength(20)]
    public List<OrderLineRequest> Items { get; set; } = [];
}

public sealed record NewOrder(string OwnerId, string OrderNumber,
    IReadOnlyList<OrderItem> Items);

public enum OrderOutcome { Created, Cancelled, NotFound, Invalid,
    Conflict }

public sealed record OrderResult(OrderOutcome Outcome,
    Order? Order = null, string? Error = null);

public interface IOrderService
{
    Task<OrderResult> CreateAsync(string ownerId,
        CreateOrderRequest request,
        CancellationToken ct = default);
    Task<OrderResult> CancelAsync(string ownerId, int id,
        CancellationToken ct = default);
}

// Writes that wait for the unit of work: nothing here saves.
public interface IOrderWriter
{
    Task<string> NextOrderNumberAsync(CancellationToken ct);
    void Add(NewOrder order);
    Task<bool> SetStatusAsync(int id, string status,
        Guid version, CancellationToken ct);
}

// One save for everything the request staged.
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}

public sealed class OrderNumberConflictException(Exception inner)
    : Exception("Another order took that number.", inner);

public sealed class OrderChangedException(Exception inner)
    : Exception("The order changed after it was read.", inner);

public sealed class OrderDeletedException(Exception inner)
    : Exception("The order was deleted after the read.", inner);
