using Microsoft.EntityFrameworkCore;

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int StockQuantity { get; set; }
}
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
}


public static class Program
{
    public static void Main()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=EfCoreBook_Inspect;Integrated Security=true;TrustServerCertificate=true").Options;
        using var db = new InventoryDbContext(options);
        var threshold = 5;
        var query = db.Products.Where(p => p.StockQuantity < threshold).OrderBy(p => p.Id);
        Console.WriteLine(query.ToQueryString());
    }
}
