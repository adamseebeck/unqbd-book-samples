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
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var connection = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Set ConnectionStrings:DefaultConnection.");
        builder.Services.AddDbContext<InventoryDbContext>(options => options.UseSqlServer(connection,
            sql => { if (!builder.Environment.IsDevelopment()) sql.EnableRetryOnFailure(3); }));
        var app = builder.Build();
        app.MapGet("/configuration", (InventoryDbContext db) =>
            Results.Ok(new { Provider = db.Database.ProviderName }));
        app.Run();
    }
}
