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
            Description = "Lined pages",
            Category = new Category { Name = "Books" }
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }
}
