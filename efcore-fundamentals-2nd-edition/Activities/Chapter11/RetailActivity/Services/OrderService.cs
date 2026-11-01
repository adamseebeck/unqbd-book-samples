using Microsoft.EntityFrameworkCore;

public static class OrderService
{
    public static async Task<int> PlaceAsync(
        InventoryDbContext db, int customerId, int productId,
        int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity));
        if (!await db.Customers.AnyAsync(c => c.Id == customerId))
            throw new InvalidOperationException(
                "Unknown customer.");
        var product = await db.Products
            .SingleAsync(p => p.Id == productId);
        if (product.StockQuantity < quantity)
            throw new InvalidOperationException(
                "Insufficient stock.");

        product.StockQuantity -= quantity;
        var order = new Order
        {
            CustomerId = customerId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            OrderItems =
            [
                new OrderItem
                {
                    ProductId = productId, Quantity = quantity,
                    UnitPrice = product.Price
                }
            ]
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }
}
