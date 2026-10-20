using System.Diagnostics;
using AdventureWorks.DataSample;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
namespace AdventureWorks.ServicesSample;

public sealed record RequestedItem(int ProductId, int Quantity);
public sealed record CreateOrderRequest(string OrderNumber, List<RequestedItem> Items);
public sealed record OrderResult(bool Succeeded, Guid? OrderId, string? Error)
{
    public static OrderResult Failure(string error) => new(false, null, error);
}
public interface IOrderRepository
{
    Task<Order?> FindOwnedAsync(Guid id, string ownerId, CancellationToken ct);
    void Add(Order order);
}
public sealed class OrderRepository(ApplicationDbContext db) : IOrderRepository
{
    public Task<Order?> FindOwnedAsync(Guid id, string ownerId, CancellationToken ct)
        => db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == id && o.OwnerId == ownerId, ct);
    public void Add(Order order) => db.Orders.Add(order);
}
public interface ICatalogRepository
{
    Task<Dictionary<int, Product>> FindAsync(int[] ids, CancellationToken ct);
}
public sealed class CatalogRepository(ApplicationDbContext db) : ICatalogRepository
{
    public Task<Dictionary<int, Product>> FindAsync(int[] ids, CancellationToken ct)
        => db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
}
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}
public sealed class OrderNumberConflictException(Exception innerException)
    : Exception("The order number is unavailable.", innerException);
public sealed class UnitOfWork(ApplicationDbContext db) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        try { return await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException
            { SqliteExtendedErrorCode: 2067 } sqlite &&
            sqlite.Message.Contains("Orders.OrderNumber", StringComparison.Ordinal))
        {
            throw new OrderNumberConflictException(ex);
        }
    }
}
public interface IOrderService
{
    Task<OrderResult> CreateAsync(string ownerId, CreateOrderRequest request, CancellationToken ct);
    Task<Order?> GetAsync(string ownerId, Guid id, CancellationToken ct);
    Task<OrderResult> CancelAsync(string ownerId, Guid id, CancellationToken ct);
}
public sealed class OrderService(IOrderRepository orders, ICatalogRepository catalog, IUnitOfWork unit) : IOrderService
{
    public async Task<OrderResult> CreateAsync(string ownerId, CreateOrderRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ownerId)) return OrderResult.Failure("An authenticated owner is required.");
        if (string.IsNullOrWhiteSpace(request.OrderNumber) || request.OrderNumber.Length > 50)
            return OrderResult.Failure("Use an order number up to 50 characters.");
        if (request.Items is null || request.Items.Count is < 1 or > 20 || request.Items.Any(i => i is null || i.Quantity is < 1 or > 20))
            return OrderResult.Failure("Use 1-20 lines with quantities from 1 to 20.");
        var ids = request.Items.Select(i => i.ProductId).Distinct().ToArray();
        if (ids.Length != request.Items.Count) return OrderResult.Failure("Combine duplicate product lines.");
        var products = await catalog.FindAsync(ids, ct);
        if (products.Count != ids.Length) return OrderResult.Failure("A selected product is unavailable.");
        var order = new Order { OwnerId = ownerId, OrderNumber = request.OrderNumber,
            Items = request.Items.Select(i => new OrderItem { ProductId = i.ProductId,
                Quantity = i.Quantity, UnitPriceCents = products[i.ProductId].UnitPriceCents }).ToList() };
        orders.Add(order);
        try { await unit.SaveChangesAsync(ct); }
        catch (OrderNumberConflictException)
        {
            return OrderResult.Failure("The order could not be created with that number.");
        }
        return new OrderResult(true, order.Id, null);
    }
    public Task<Order?> GetAsync(string ownerId, Guid id, CancellationToken ct)
        => orders.FindOwnedAsync(id, ownerId, ct);
    public async Task<OrderResult> CancelAsync(string ownerId, Guid id, CancellationToken ct)
    {
        var order = await orders.FindOwnedAsync(id, ownerId, ct);
        if (order is null) return OrderResult.Failure("Order not found.");
        if (order.Status == "Shipped") return OrderResult.Failure("A shipped order cannot be cancelled.");
        order.Status = "Cancelled";
        try { await unit.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return OrderResult.Failure("Order changed; reload before retrying."); }
        return new OrderResult(true, order.Id, null);
    }
}
public sealed class LoggingOrderServiceDecorator(IOrderService inner, ILogger<LoggingOrderServiceDecorator> logger) : IOrderService
{
    public async Task<OrderResult> CreateAsync(string ownerId, CreateOrderRequest request, CancellationToken ct)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            var result = await inner.CreateAsync(ownerId, request, ct);
            logger.LogInformation("Create order completed. Succeeded={Succeeded}; ElapsedMs={ElapsedMs}",
                result.Succeeded, watch.ElapsedMilliseconds);
            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogError(ex, "Create order failed after {ElapsedMs}ms", watch.ElapsedMilliseconds); throw; }
    }
    public Task<Order?> GetAsync(string ownerId, Guid id, CancellationToken ct) => inner.GetAsync(ownerId, id, ct);
    public Task<OrderResult> CancelAsync(string ownerId, Guid id, CancellationToken ct) => inner.CancelAsync(ownerId, id, ct);
}
