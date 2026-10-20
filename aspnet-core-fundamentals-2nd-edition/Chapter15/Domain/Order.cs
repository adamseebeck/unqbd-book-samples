namespace AdventureWorks.Domain;

public sealed record CreateOrderRequest(int ProductId, int Quantity);
public sealed class Order
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
public interface IInventoryService
{
    Task<bool> IsAvailableAsync(int productId, int quantity, CancellationToken ct);
}
public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct);
}
public sealed class InsufficientInventoryException : Exception { }
public sealed class OrderService(IInventoryService inventory, IOrderRepository repository)
{
    public async Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        if (request.ProductId < 1 || request.Quantity is < 1 or > 1000)
            throw new ArgumentException("Supply a product and quantity 1..1000.");
        if (!await inventory.IsAvailableAsync(request.ProductId, request.Quantity, ct))
            throw new InsufficientInventoryException();
        var order = new Order { Id = Guid.NewGuid(), ProductId = request.ProductId, Quantity = request.Quantity };
        await repository.AddAsync(order, ct);
        return order;
    }
}
