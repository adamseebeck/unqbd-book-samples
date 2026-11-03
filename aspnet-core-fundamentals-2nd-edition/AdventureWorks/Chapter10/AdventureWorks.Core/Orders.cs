namespace AdventureWorks.Core;

public sealed record OrderItem(
    int ProductId, int Quantity, decimal UnitPrice);

public sealed record Order(int Id, string OrderNumber,
    string OwnerId, decimal Total, string Status,
    bool Archived, string? Notes, Guid Version,
    IReadOnlyList<OrderItem> Items);

// One row of an owner's order list.
public sealed record OrderSummary(int Id, string OrderNumber,
    decimal Total, string Status, bool Archived);

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<OrderSummary[]> ListAsync(string ownerId, int page);
    Task<UpdateOutcome> UpdateNotesAsync(
        int id, string? notes, Guid version);
    Task<bool> ArchiveAsync(int id);
    Task<bool> DeleteAsync(int id);
}

public interface ICacheService
{
    string? Get(string key);
    void Set(string key, string value);
}

public interface IEmailValidator
{
    bool IsValid(string address);
}
