public static class Seed
{
    public static async Task<Product> ProductAsync(
        InventoryDbContext db)
    {
        var product = new Product
        {
            Sku = "BOOK-001",
            Name = "C# Notebook",
            Price = 12.50m,
            StockQuantity = 10,
            Category = new Category { Name = "Books" }
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    public static async Task CatalogAsync(InventoryDbContext db)
    {
        var notebook = await ProductAsync(db);
        var pad = new Product
        {
            Sku = "PAD-002", Name = "Desk Pad", Price = 12.50m,
            StockQuantity = 2, CategoryId = notebook.CategoryId
        };
        var pen = new Product
        {
            Sku = "PEN-003", Name = "Gel Pen", Price = 2.25m,
            StockQuantity = 5,
            Category = new Category { Name = "Office" }
        };
        db.Products.AddRange(pad, pen);
        db.Orders.Add(new Order
        {
            Customer = new Customer
            {
                FirstName = "Alex", LastName = "Lee",
                Email = "alex@example.test",
                CreatedAt = DateTime.UtcNow
            },
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            OrderItems =
            [
                new OrderItem
                {
                    Product = notebook, Quantity = 1,
                    UnitPrice = notebook.Price
                }
            ]
        });
        await db.SaveChangesAsync();
    }
}
