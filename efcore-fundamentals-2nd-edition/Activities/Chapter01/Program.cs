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
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(
            @"Server=(localdb)\MSSQLLocalDB;Database=EfCoreBook_Inspect;Integrated Security=true;TrustServerCertificate=true").Options;
        using var db = new InventoryDbContext(options);
        var threshold = 10;
        var full = db.Products.Where(p => p.StockQuantity < threshold).OrderBy(p => p.Id).ToQueryString();
        var projected = db.Products.Where(p => p.StockQuantity < threshold).OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.Name }).ToQueryString();
        var selected = projected.Split("FROM")[0].Split("SELECT").Last();
        if (selected.Contains("StockQuantity") || !selected.Contains("Name"))
            throw new InvalidOperationException("Projection selected unexpected columns.");
        Console.WriteLine(full); Console.WriteLine(projected);
        Console.WriteLine("PASS: projection changes selected columns without a connection");
    }
}
