using Microsoft.EntityFrameworkCore;

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<Product> Products { get; set; } = [];
}
public sealed class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public List<ProductCategory> ProductCategories { get; set; } = [];
    public byte[] RowVersion { get; set; } = [];
}
public enum CustomerStatus { Active, Inactive, Suspended }
public enum OrderStatus { Pending, Completed, Cancelled }
public sealed class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public CustomerStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Order> Orders { get; set; } = [];
    public CustomerProfile? Profile { get; set; }
}
public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderItem> OrderItems { get; set; } = [];
}
public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
public sealed class ProductCategory
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int StockThreshold { get; set; }
}
public sealed class CustomerProfile
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string LoyaltyTier { get; set; } = "Standard";
}

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Category>().Property(x => x.Name).HasMaxLength(100);
        model.Entity<Category>().HasIndex(x => x.Name).IsUnique();
        model.Entity<Product>().ToTable("Products");
        model.Entity<Product>().Property(x => x.Name).HasMaxLength(200);
        model.Entity<Product>().Property(x => x.Sku).HasMaxLength(40);
        model.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        model.Entity<Product>().Property(x => x.Price).HasPrecision(18, 2);
        model.Entity<Product>().HasOne(x => x.Category).WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Customer>().Property(x => x.Email).HasMaxLength(255);
        model.Entity<Customer>().HasIndex(x => x.Email).IsUnique();
        model.Entity<Customer>().Property(x => x.Status).HasConversion<string>();
        model.Entity<Order>().Property(x => x.Status).HasConversion<string>();
        model.Entity<Order>().HasOne(x => x.Customer).WithMany(x => x.Orders)
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OrderItem>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        model.Entity<OrderItem>().HasOne(x => x.Order).WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OrderItem>().HasOne(x => x.Product).WithMany()
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ProductCategory>().HasKey(x => new { x.ProductId, x.CategoryId });
        model.Entity<ProductCategory>().HasOne(x => x.Product).WithMany(x => x.ProductCategories)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<ProductCategory>().HasOne(x => x.Category).WithMany()
            .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<CustomerProfile>().HasKey(x => x.CustomerId);
        model.Entity<CustomerProfile>().HasOne(x => x.Customer).WithOne(x => x.Profile)
            .HasForeignKey<CustomerProfile>(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);

        model.Entity<Product>().Property(x => x.RowVersion).IsRowVersion();
    }
}

public sealed class LocalLab : IAsyncDisposable
{
    public DbContextOptions<InventoryDbContext> Options { get; }
    public string ConnectionString { get; }
    public LocalLab(int chapter)
    {
        var instance = Environment.GetEnvironmentVariable("EFCORE_BOOK_LOCALDB")
            ?? @"(localdb)\MSSQLLocalDB";
        if (!System.Text.RegularExpressions.Regex.IsMatch(instance,
            @"^\(localdb\)\\[A-Za-z0-9_]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            throw new InvalidOperationException("Use a local LocalDB instance for these disposable labs.");
        var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = instance, InitialCatalog = $"EfCoreBook_Ch{chapter:00}_{Guid.NewGuid():N}",
            IntegratedSecurity = true, Encrypt = true, TrustServerCertificate = true
        };
        ConnectionString = connection.ConnectionString;
        Console.WriteLine($"Local lab database: {connection.InitialCatalog}");
        Options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.UseCompatibilityLevel(160)).Options;
    }
    public InventoryDbContext Create() => new(Options);
    public async ValueTask DisposeAsync()
    {
        await using var db = Create();
        await db.Database.EnsureDeletedAsync();
    }
}

public static class Seed
{
    public static async Task<Product> ProductAsync(InventoryDbContext db)
    {
        var product = new Product { Name = "C# Notebook", Sku = "BOOK-001", Price = 12.50m,
            StockQuantity = 10, Category = new Category { Name = "Books" } };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }
}


public sealed record ProductView(int Id, string Name, decimal Price, int StockQuantity, string Version);
public sealed record EditProduct(string Name, decimal Price, int StockQuantity, string Version);
public sealed class LowStockSpecification(int threshold)
{
    public System.Linq.Expressions.Expression<Func<Product, bool>> Criteria => p => p.StockQuantity < threshold;
}
public static class ProductApi
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/products/page", async Task<IResult> (int? page, int? take, int? threshold,
            InventoryDbContext db, CancellationToken ct) =>
        {
            var number = page ?? 0; var size = take ?? 20; var limit = threshold ?? 20;
            if (number is < 0 or > 10000 || size is < 1 or > 100 || limit < 0) return Results.BadRequest();
            var spec = new LowStockSpecification(limit);
            var filtered = db.Products.AsNoTracking().Where(spec.Criteria);
            var total = await filtered.CountAsync(ct);
            var rows = await filtered.OrderBy(p => p.Id).Skip(number * size).Take(size)
                .Select(p => new { p.Id, p.Name, p.Price, p.StockQuantity, p.RowVersion }).ToListAsync(ct);
            return Results.Ok(new { Page = number, Total = total, TotalPages = (total + (long)size - 1) / size,
                Items = rows.Select(p => new ProductView(p.Id, p.Name, p.Price,
                    p.StockQuantity, Convert.ToBase64String(p.RowVersion))) });
        });
        app.MapGet("/products", async Task<IResult> (int? take, InventoryDbContext db, CancellationToken ct) =>
        {
            var size = take ?? 20;
            if (size is < 1 or > 100) return Results.BadRequest();
            var rows = await db.Products.AsNoTracking().OrderBy(p => p.Id).Take(size)
                .Select(p => new { p.Id, p.Name, p.Price, p.StockQuantity, p.RowVersion }).ToListAsync(ct);
            return Results.Ok(rows.Select(p => new ProductView(p.Id, p.Name, p.Price,
                p.StockQuantity, Convert.ToBase64String(p.RowVersion))));
        });
        app.MapPut("/products/{id:int}", async Task<IResult> (int id, EditProduct input,
            InventoryDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 200 ||
                input.Price is < 0 or > 1000000 || decimal.Round(input.Price, 2) != input.Price || input.StockQuantity < 0 || input.Version is null ||
                input.Version.Length > 16) return Results.BadRequest();
            var token = new byte[8];
            if (!Convert.TryFromBase64String(input.Version, token, out var count) || count != 8)
                return Results.BadRequest();
            var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id, ct);
            if (product is null) return Results.NotFound();
            if (!token.AsSpan().SequenceEqual(product.RowVersion)) return Results.Conflict();
            db.Entry(product).Property(p => p.RowVersion).OriginalValue = token;
            product.Name = input.Name.Trim(); product.Price = input.Price;
            product.StockQuantity = input.StockQuantity;
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict(); }
            return Results.Ok(new ProductView(product.Id, product.Name, product.Price,
                product.StockQuantity, Convert.ToBase64String(product.RowVersion)));
        });
    }
}

public sealed record CreateProduct(string Name, string Sku, decimal Price, int StockQuantity, int CategoryId);
public static class ProductCreates
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/products/{id:int}", async Task<IResult> (int id, InventoryDbContext db, CancellationToken ct) =>
        {
            var p = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);
            return p is null ? Results.NotFound() : Results.Ok(new ProductView(p.Id, p.Name, p.Price,
                p.StockQuantity, Convert.ToBase64String(p.RowVersion)));
        });
        app.MapPost("/products", async Task<IResult> (CreateProduct input, InventoryDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 200 ||
                string.IsNullOrWhiteSpace(input.Sku) || input.Sku.Length > 40 ||
                input.Price is < 0 or > 1000000 || decimal.Round(input.Price, 2) != input.Price || input.StockQuantity < 0 ||
                !await db.Categories.AnyAsync(c => c.Id == input.CategoryId, ct)) return Results.BadRequest();
            var p = new Product { Name = input.Name.Trim(), Sku = input.Sku.Trim(), Price = input.Price,
                StockQuantity = input.StockQuantity, CategoryId = input.CategoryId };
            db.Add(p);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql && sql.Number is 2601 or 2627)
                { return Results.Conflict(); }
            return Results.Created($"/products/{p.Id}", new ProductView(p.Id, p.Name, p.Price,
                p.StockQuantity, Convert.ToBase64String(p.RowVersion)));
        });
    }
}

public static class HttpChecks
{
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        Console.WriteLine($"PASS: {name}");
    }
    public static async Task RunAsync(HttpClient client, Func<InventoryDbContext> create)
    {
        int category;
        await using (var db = create()) category = await db.Categories.Select(c => c.Id).SingleAsync();
        var created = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(client, "/products",
            new CreateProduct("HTTP Item", "HTTP-001", 2.50m, 4, category));
        Check(created.StatusCode == System.Net.HttpStatusCode.Created, "HTTP create returns 201");
        var location = created.Headers.Location ?? throw new InvalidOperationException("Missing Location.");
        var p = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<ProductView>((await client.GetAsync(location)).Content)
            ?? throw new InvalidOperationException("Missing product body.");
        Check(p.Name == "HTTP Item", "Created Location can be followed");
        var input = new EditProduct("Edited HTTP Item", 3m, 5, p.Version);
        var updated = await System.Net.Http.Json.HttpClientJsonExtensions.PutAsJsonAsync(client, location, input);
        Check(updated.StatusCode == System.Net.HttpStatusCode.OK, "HTTP edit succeeds");
        Check((await System.Net.Http.Json.HttpClientJsonExtensions.PutAsJsonAsync(client, location, input)).StatusCode == System.Net.HttpStatusCode.Conflict,
            "Old client token returns 409");
        var duplicate = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(client, "/products",
            new CreateProduct("Duplicate", "HTTP-001", 2m, 1, category));
        Check(duplicate.StatusCode == System.Net.HttpStatusCode.Conflict, "Duplicate SKU returns 409");
        var invalid = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(client, "/products",
            new CreateProduct("Bad category", "BAD-CAT", 2m, 1, int.MaxValue));
        Check(invalid.StatusCode == System.Net.HttpStatusCode.BadRequest, "Unknown category returns 400");
        Check((await client.GetAsync("/products?take=0")).StatusCode == System.Net.HttpStatusCode.BadRequest, "Zero page returns 400");
        Check((await System.Net.Http.Json.HttpClientJsonExtensions.PutAsJsonAsync(client, location,
            new EditProduct("Invalid", 1m, 1, "bad"))).StatusCode == System.Net.HttpStatusCode.BadRequest, "Invalid token returns 400");
        Check((await client.GetAsync("/products/2147483647")).StatusCode == System.Net.HttpStatusCode.NotFound, "Missing product read returns 404");
        Check((await System.Net.Http.Json.HttpClientJsonExtensions.PutAsJsonAsync(client, "/products/2147483647", input)).StatusCode == System.Net.HttpStatusCode.NotFound,
            "Missing product edit returns 404");
        using var page = System.Text.Json.JsonDocument.Parse(await client.GetStringAsync("/products/page?page=0&take=1&threshold=20"));
        Check(page.RootElement.GetProperty("total").GetInt32() == 2 && page.RootElement.GetProperty("totalPages").GetInt64() == 2 &&
            page.RootElement.GetProperty("items").GetArrayLength() == 1, "Paged count and rows share the filter");
        using var filtered = System.Text.Json.JsonDocument.Parse(await client.GetStringAsync("/products/page?threshold=6"));
        Check(filtered.RootElement.GetProperty("total").GetInt32() == 1, "Specification threshold filters count");
        Check((await client.GetAsync("/products/page?page=-1")).StatusCode == System.Net.HttpStatusCode.BadRequest &&
            (await client.GetAsync("/products/page?take=101")).StatusCode == System.Net.HttpStatusCode.BadRequest &&
            (await client.GetAsync("/products/page?threshold=-1")).StatusCode == System.Net.HttpStatusCode.BadRequest,
            "Invalid page, size and threshold return 400");
        await using var read = create();
        Check(await read.Products.CountAsync() == 2, "Rejected HTTP writes add no rows");
        Check((await read.Products.SingleAsync(x => x.Id == p.Id)).Name == "Edited HTTP Item", "HTTP edit persisted in fresh context");
    }
}

public static class Program
{
    public static async Task Main()
    {
        await using var lab = new LocalLab(11);
        await using (var setup = lab.Create()) { await setup.Database.EnsureCreatedAsync(); await Seed.ProductAsync(setup); }
        var builder = WebApplication.CreateBuilder(); builder.WebHost.UseUrls("http://127.0.0.1:50731");
        builder.Services.AddDbContext<InventoryDbContext>(o => o.UseSqlServer(lab.ConnectionString));
        await using var app = builder.Build(); ProductApi.Map(app); ProductCreates.Map(app);
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:50731") };
            await HttpChecks.RunAsync(client, lab.Create);
        }
        finally { await app.StopAsync(); }
    }
}
