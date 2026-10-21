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


public sealed class DatabaseReadiness(IServiceScopeFactory scopes)
    : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        return await db.Database.CanConnectAsync(cancellationToken)
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database unavailable");
    }
}

public sealed class OverlapGate
{
    public TaskCompletionSource<Guid> WorkerEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<Guid> RequestEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
public sealed class AuditMiddleware(RequestDelegate next, OverlapGate overlap)
{
    public async Task InvokeAsync(HttpContext http, InventoryDbContext db)
    {
        http.Items["OperationId"] = http.TraceIdentifier;
        overlap.RequestEntered.TrySetResult(db.ContextId.InstanceId);
        await overlap.Release.Task.WaitAsync(http.RequestAborted);
        http.Items["ProductCount"] = await db.Products.CountAsync(http.RequestAborted);
        await next(http);
    }
}
public sealed class InventoryWorker(IServiceScopeFactory scopes, TaskCompletionSource<int> observed, OverlapGate overlap) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            overlap.WorkerEntered.TrySetResult(db.ContextId.InstanceId);
            await overlap.Release.Task.WaitAsync(stoppingToken);
            observed.TrySetResult(await db.Products.CountAsync(stoppingToken));
        }
        catch (Exception ex) { observed.TrySetException(ex); }
    }
}
public sealed class MissingDatabaseReadiness(string localConnection) : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(localConnection).Options;
        await using var db = new InventoryDbContext(options);
        return await db.Database.CanConnectAsync(cancellationToken)
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Local readiness database unavailable");
    }
}
public static class Program
{
    public static async Task Main()
    {
        await using var lab = new LocalLab(10);
        await using (var setup = lab.Create()) { await setup.Database.EnsureCreatedAsync(); await Seed.ProductAsync(setup); }
        var builder = WebApplication.CreateBuilder(); builder.WebHost.UseUrls("http://127.0.0.1:50730");
        builder.Services.AddDbContext<InventoryDbContext>(o => o.UseSqlServer(lab.ConnectionString));
        var observed = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var overlap = new OverlapGate();builder.Services.AddSingleton(overlap);
        builder.Services.AddSingleton(observed); builder.Services.AddHostedService<InventoryWorker>();
        var absentDatabase = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(lab.ConnectionString)
        {
            InitialCatalog = "EfCoreBook_Absent_" + Guid.NewGuid().ToString("N"), ConnectTimeout = 2, ConnectRetryCount = 0
        };
        builder.Services.AddHealthChecks().AddCheck("database", new MissingDatabaseReadiness(absentDatabase.ConnectionString));
        await using var app = builder.Build();
        app.UseWhen(http => http.Request.Path.StartsWithSegments("/observed"), branch => branch.UseMiddleware<AuditMiddleware>());
        app.MapGet("/health/live", () => Results.Ok());
        app.MapGet("/observed", (HttpContext http) => Results.Ok(new { OperationId = http.Items["OperationId"], ProductCount = http.Items["ProductCount"] }));
        app.MapHealthChecks("/health/ready"); await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:50730") };
            var request = client.GetStringAsync("/observed");
            await Task.WhenAll(overlap.WorkerEntered.Task, overlap.RequestEntered.Task).WaitAsync(TimeSpan.FromSeconds(10));
            if (request.IsCompleted || observed.Task.IsCompleted || overlap.WorkerEntered.Task.Result == overlap.RequestEntered.Task.Result)
                throw new InvalidOperationException("Both independent context lifetimes did not overlap at the unreleased barrier.");
            overlap.Release.TrySetResult(true);
            await Task.WhenAll(request, observed.Task).WaitAsync(TimeSpan.FromSeconds(10));
            using var body = System.Text.Json.JsonDocument.Parse(await request);
            if (observed.Task.Result != 1 || body.RootElement.GetProperty("productCount").GetInt32() != 1 ||
                string.IsNullOrEmpty(body.RootElement.GetProperty("operationId").GetString()))
                throw new InvalidOperationException("Independent reads or operation ID failed.");
            if ((await client.GetAsync("/health/live")).StatusCode != System.Net.HttpStatusCode.OK ||
                (await client.GetAsync("/health/ready")).StatusCode != System.Net.HttpStatusCode.ServiceUnavailable)
                throw new InvalidOperationException("Liveness/readiness separation failed.");
            Console.WriteLine("PASS: middleware scope, worker scope, live 200 and deliberate ready 503");
        }
        finally { overlap.Release.TrySetResult(true);await app.StopAsync(); }
    }
}
