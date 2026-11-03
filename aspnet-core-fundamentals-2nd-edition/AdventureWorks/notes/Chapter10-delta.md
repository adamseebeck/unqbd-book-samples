# Chapter 10 delta: Entity Framework Core Integration and Data Access Patterns

Generated November 3, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter10` and the operations that rebuild it from the Chapter 9 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 10 moves the store from memory into the SQLite database Chapter 8 created for Identity. The store's tables join the one context, `AdventureWorksDbContext`, migrations replace EnsureCreated, and EF Core implementations replace the in-memory ones behind the same Core interfaces.

- Tools: a local tool manifest (`dotnet-tools.json` in the working folder) pins dotnet-ef 11.0.0; the web project gets Microsoft.EntityFrameworkCore.Design so the tool can run its startup code. The reader drops Chapter 8's EnsureCreated database first.
- Infrastructure `Data/`: `StoreEntities.cs` (ProductEntity, OrderEntity, OrderItemEntity, InventoryItemEntity, and Money, which converts decimal to whole cents and back), `StoreConfiguration.cs` (one IEntityTypeConfiguration per entity: check constraints, unique indexes, foreign keys, the order's Version concurrency token), `SqliteErrors.cs`, `EfProductService`, `EfOrderRepository` and `EfInventoryRepository`. `Migrations/` holds the generated InitialCreate. The context gains three DbSets and renews a changed order's Version in both SaveChanges overloads.
- Core: `Order(Id, OrderNumber, OwnerId, Total, Status, Archived, Notes, Version, Items)` with `OrderItem` lines and an `OrderSummary` row; IOrderRepository is asynchronous and gains ListAsync and UpdateNotesAsync. `Inventory.cs`: StockLevel, StockTransfer (validated), TransferOutcome and IInventoryRepository.
- Web: the product, inventory and order registrations are scoped (as is CatalogAvailability); the order handlers await the repository; new routes `GET /api/orders/report` (the signed-in owner's orders, ten per page, totals summed in SQL), `PUT /api/orders/{id}` (notes, with the Version the client read: 409 when stale), and the Admin's `GET /api/admin/inventory/{productId}` and `POST /api/admin/inventory/transfer` (two saves in one transaction). `StoreSeed.cs` seeds four products, two stock locations and twelve orders; the Development seed calls MigrateAsync. Program.cs gains one line: 77 lines.
- Deleted: `InMemoryProductService.cs` and the in-memory OrderRepository class.

## Files added or changed

| File | Change |
| --- | --- |
| `AdventureWorks.Infrastructure/Data/StoreEntities.cs` | added |
| `AdventureWorks.Infrastructure/Data/StoreConfiguration.cs` | added |
| `AdventureWorks.Infrastructure/AdventureWorksDbContext.cs` | replaced |
| `AdventureWorks/DevelopmentSeed.cs` | changed |
| `AdventureWorks.Infrastructure/Data/SqliteErrors.cs` | added |
| `AdventureWorks.Infrastructure/Data/EfProductService.cs` | added |
| `AdventureWorks.Infrastructure/InMemoryProductService.cs` | deleted |
| `AdventureWorks/CatalogEndpoints.cs` | changed |
| `AdventureWorks.Core/Orders.cs` | changed |
| `AdventureWorks.Infrastructure/Data/EfOrderRepository.cs` | added |
| `AdventureWorks.Infrastructure/Services.cs` | changed |
| `AdventureWorks/OrderEndpoints.cs` | changed |
| `AdventureWorks/OrderInputs.cs` | changed |
| `AdventureWorks/StoreSeed.cs` | added |
| `AdventureWorks.Core/Inventory.cs` | added |
| `AdventureWorks.Infrastructure/Data/EfInventoryRepository.cs` | added |
| `AdventureWorks/InventoryEndpoints.cs` | added |
| `AdventureWorks/Program.cs` | changed |
| (command) `dotnet new tool-manifest` | creates or edits project files |
| (command) `dotnet tool install dotnet-ef@11.0.0` | creates or edits project files |
| (command) `dotnet add package Microsoft.EntityFrameworkCore.Design@11.0.0` | creates or edits project files |
| (command) `dotnet ef migrations add InitialCreate -s ..\AdventureWorks` | generates the migration files in `AdventureWorks.Infrastructure/Migrations` |

The reader types 818 lines of code: 563 in 9 new files and 255 new or changed in existing files, plus 7 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 73 of 73 files identical and no differences.

### Step 1: Install the EF Core tools and drop the Chapter 8 database

EF Core maps C# classes to database tables: a DbContext is one session with the database, each DbSet is a table, and a migration is a generated C# file that changes the schema from one version to the next. Chapter 8 already used EF Core as Identity's store; this chapter adds the store's own tables and switches from EnsureCreated to migrations. The dotnet-ef tool generates and applies migrations. A local tool manifest pins its version in the working folder, so everyone who clones the folder runs the same tool after dotnet tool restore. The tool runs the app's startup code to find the DbContext, so the web project needs the Design package.

Run this from the working folder.

```shell
dotnet new tool-manifest
```

It prints:

```text
The template "Dotnet local tool manifest file" was created successfully.
```

Run this from the working folder.

```shell
dotnet tool install dotnet-ef@11.0.0
```

It prints:

```text
You can invoke the tool from this directory using the following commands: 'dotnet tool run dotnet-ef' or 'dotnet dotnet-ef'.
Tool 'dotnet-ef' (version '11.0.0') was successfully installed. Entry is added to the manifest file dotnet-tools.json.
```

Run this from the `AdventureWorks` folder.

```shell
dotnet add package Microsoft.EntityFrameworkCore.Design@11.0.0
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.EntityFrameworkCore.Design' into project 'AdventureWorks\AdventureWorks.csproj'.
info : Restoring packages for AdventureWorks\AdventureWorks.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.11.42.14/vulnerability.update.json
info : Package 'Microsoft.EntityFrameworkCore.Design' is compatible with all the specified frameworks in project 'AdventureWorks\AdventureWorks.csproj'.
info : PackageReference for package 'Microsoft.EntityFrameworkCore.Design' version '11.0.0' added to file 'AdventureWorks\AdventureWorks.csproj'.
info : Generating MSBuild file AdventureWorks\obj\AdventureWorks.csproj.nuget.g.props.
(3 more lines)
```

Stop the app if it is running. EnsureCreated built Chapter 8's database without a migrations history, so migrations cannot take it over: the first migration would try to create AspNetUsers again and fail with "table already exists". Drop it. Chapter 10's Development seed recreates the test accounts. The tool runs from the Infrastructure folder, where the DbContext and the migrations live; `-s` (`--startup-project`) names the web project, whose configuration holds the connection string.

Run this from the working folder. dotnet ef reads each project's restore output and fails with "Unable to retrieve project metadata" on a project that was never restored or built, such as a fresh clone.

```shell
dotnet build
```

Run this from the `AdventureWorks.Infrastructure` folder. The drop deletes adventureworks.db. SQLite keeps adventureworks.db-wal and adventureworks.db-shm beside the database while connections are open, and the drop leaves those two behind; they are harmless (the next database update creates a fresh, intact database and they disappear), or delete them too.

```shell
dotnet ef database drop --force -s ..\AdventureWorks
```

It prints:

```text
Build started...
Build succeeded.
Dropping database 'main' on server 'AdventureWorks\adventureworks.db'.
Successfully dropped database 'main'.
```

### Step 2: Map the store's tables

The database rows are separate classes in Infrastructure, so Core's records stay the app's model and the EF Core code converts at the boundary. Money stays decimal in Core and in every API contract; the tables store whole cents (PriceCents, UnitPriceCents), because SQLite has no exact decimal type and whole cents keep money exact and simple to compare; the order list sums the cents in SQL. Money.ToCents rounds to the nearest cent, and FromCents returns two decimal places, so 129900 reads back as 1299.00.

Create `AdventureWorks.Infrastructure/Data/StoreEntities.cs` (54 lines).

StoreEntities.cs, part 1 of 2 (25 lines):

```csharp
namespace AdventureWorks.Infrastructure.Data;

// The rows EF Core maps to tables. Core's records stay the app's
// model; the EF Core classes convert at this boundary.
public sealed class ProductEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Sku { get; set; } = "";
    public long PriceCents { get; set; }
    public string Category { get; set; } = "";
    public int Version { get; set; } = 1;
}

public sealed class OrderEntity
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public bool Archived { get; set; }
    public string? Notes { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public List<OrderItemEntity> Items { get; set; } = [];
}
```

StoreEntities.cs, part 2 of 2 (28 lines):

```csharp
public sealed class OrderItemEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public long UnitPriceCents { get; set; }
}

public sealed class InventoryItemEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Location { get; set; } = "";
    public int Quantity { get; set; }
}

// Core and the API use decimal; the database stores whole cents.
// SQLite has no exact decimal type, and whole cents keep money
// exact and simple to compare.
public static class Money
{
    public static long ToCents(decimal amount) =>
        (long)decimal.Round(amount * 100,
            MidpointRounding.AwayFromZero);

    public static decimal FromCents(long cents) => cents * 0.01m;
}
```

The configuration classes hold the rules the database itself enforces: check constraints on prices and quantities, unique indexes on SKUs, order numbers and stock locations, foreign keys that refuse to delete a product that is on an order or a user who owns orders, and the order's Version concurrency token. SQLite stores text without a length limit, so HasMaxLength is metadata there; the API validation enforces the lengths.

Create `AdventureWorks.Infrastructure/Data/StoreConfiguration.cs` (79 lines).

StoreConfiguration.cs, part 1 of 3 (23 lines):

```csharp
using AdventureWorks.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdventureWorks.Infrastructure.Data;

// The database's rules; SQLite does not enforce HasMaxLength.
internal sealed class ProductConfiguration
    : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(
        EntityTypeBuilder<ProductEntity> product)
    {
        product.ToTable("Products", table =>
            table.HasCheckConstraint(
                "CK_Products_PriceCents", "PriceCents >= 0"));
        product.Property(p => p.Name).HasMaxLength(100);
        product.Property(p => p.Sku).HasMaxLength(8);
        product.Property(p => p.Category).HasMaxLength(20);
        product.HasIndex(p => p.Sku).IsUnique();
    }
}
```

StoreConfiguration.cs, part 2 of 3 (19 lines):

```csharp
internal sealed class OrderConfiguration
    : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> order)
    {
        order.ToTable("Orders");
        order.Property(o => o.OrderNumber).HasMaxLength(20);
        order.HasIndex(o => o.OrderNumber).IsUnique();
        order.Property(o => o.Status).HasMaxLength(20);
        order.Property(o => o.Notes).HasMaxLength(500);
        // Tracked UPDATEs check this value; see SaveChangesAsync.
        order.Property(o => o.Version).IsConcurrencyToken();
        order.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(o => o.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
        order.HasMany(o => o.Items).WithOne()
            .HasForeignKey(i => i.OrderId);
    }
}
```

StoreConfiguration.cs, part 3 of 3 (36 lines):

```csharp
internal sealed class OrderItemConfiguration
    : IEntityTypeConfiguration<OrderItemEntity>
{
    public void Configure(EntityTypeBuilder<OrderItemEntity> item)
    {
        item.ToTable("OrderItems", table =>
        {
            table.HasCheckConstraint(
                "CK_OrderItems_Quantity", "Quantity > 0");
            table.HasCheckConstraint(
                "CK_OrderItems_UnitPriceCents",
                "UnitPriceCents >= 0");
        });
        // A product that is on an order cannot be deleted.
        item.HasOne<ProductEntity>().WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InventoryItemConfiguration
    : IEntityTypeConfiguration<InventoryItemEntity>
{
    public void Configure(
        EntityTypeBuilder<InventoryItemEntity> stock)
    {
        stock.ToTable("InventoryItems", table =>
            table.HasCheckConstraint(
                "CK_InventoryItems_Quantity", "Quantity >= 0"));
        stock.Property(s => s.Location).HasMaxLength(50);
        stock.HasIndex(s => new { s.ProductId, s.Location })
            .IsUnique();
        stock.HasOne<ProductEntity>().WithMany()
            .HasForeignKey(s => s.ProductId);
    }
}
```

The context gains three DbSets and applies every configuration class in the assembly. Both SaveChanges overloads give a changed order a new Version; the UPDATE still compares the old value, so a writer holding a stale Version changes no row. Code that bypasses SaveChanges (ExecuteUpdate, raw SQL) must keep that rule itself.

Replace the whole of `AdventureWorks.Infrastructure/AdventureWorksDbContext.cs` with the following.

AdventureWorksDbContext.cs, part 1 of 2 (28 lines):

```csharp
using AdventureWorks.Infrastructure.Data;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.Infrastructure;

// One context for the whole app. Chapter 8 stores Identity users
// and refresh tokens; Chapter 10 adds the catalog and orders.
public sealed class AdventureWorksDbContext(
    DbContextOptions<AdventureWorksDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens =>
        Set<RefreshToken>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<InventoryItemEntity> InventoryItems =>
        Set<InventoryItemEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash).IsUnique();
        builder.ApplyConfigurationsFromAssembly(
            typeof(AdventureWorksDbContext).Assembly);
    }
```

AdventureWorksDbContext.cs, part 2 of 2 (26 lines):

```csharp
    public override int SaveChanges(
        bool acceptAllChangesOnSuccess)
    {
        RenewVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        RenewVersions();
        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess, cancellationToken);
    }

    // A changed order gets a new Version. The UPDATE still checks
    // the old one, so a stale writer changes no row.
    void RenewVersions()
    {
        var changed = ChangeTracker.Entries<OrderEntity>()
            .Where(e => e.State == EntityState.Modified);
        foreach (var order in changed)
            order.Entity.Version = Guid.NewGuid();
    }
}
```

### Step 3: Create and apply the first migration

migrations add compares the model with the last migration (there is none yet) and writes C# that creates every table: Identity's and the store's. Inspect it before applying it. database update runs it and records it in __EFMigrationsHistory.

Run this from the `AdventureWorks.Infrastructure` folder.

```shell
dotnet ef migrations add InitialCreate -s ..\AdventureWorks
```

It prints:

```text
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
```

Run this from the `AdventureWorks.Infrastructure` folder. Each SQL command it runs is logged; the full output lists every CREATE TABLE and CREATE INDEX.

```shell
dotnet ef database update -s ..\AdventureWorks
```

It prints:

```text
Build started...
Build succeeded.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (25ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      PRAGMA journal_mode = 'wal';
info: Microsoft.EntityFrameworkCore.Migrations[20411]
      Acquiring an exclusive lock for migration application. See https://aka.ms/efcore-docs-migrations-lock for more information if this takes too long.
Acquiring an exclusive lock for migration application. See https://aka.ms/efcore-docs-migrations-lock for more information if this takes too long.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (12ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
(208 more lines)
```

In Development the seed now calls MigrateAsync instead of EnsureCreatedAsync, so a fresh clone starts on F5 without the commands above. MigrateAsync applies only the migrations the database lacks. Outside Development nothing migrates or seeds at startup: apply migrations as a deployment step (dotnet ef database update, a migration bundle or a SQL script).

In `AdventureWorks/DevelopmentSeed.cs`, add these lines after the line `using Microsoft.AspNetCore.Identity;`. MigrateAsync is an extension method in this namespace.

```csharp
using Microsoft.EntityFrameworkCore;
```

In `AdventureWorks/DevelopmentSeed.cs`, replace these lines.

```csharp
// Development only. EnsureCreated makes the local database;
// Chapter 10 explains EF Core and replaces this with migrations.
```

With these:

```csharp
// Development only. MigrateAsync applies the migrations the
// database lacks; then each part adds only what is missing.
```

In `AdventureWorks/DevelopmentSeed.cs`, replace these lines. Inside SeedDevelopmentDataAsync.

```csharp
        await db.Database.EnsureCreatedAsync();
```

With these:

```csharp
        await db.Database.MigrateAsync();
```

### Step 4: The catalog from the database, behind the same interface

This is the payoff of Chapter 2's layering. EfProductService implements Chapter 4's IProductService over the context, and the registration is the only line consumers see change: the catalog, the v1 controller, the v2 endpoints, the Razor Pages and the SKU check all keep their code. Reads use AsNoTracking because nothing changes them. Updates are single ExecuteUpdate statements; the version-checked overload puts the version in the WHERE clause, so Chapter 6's edit page still gets its conflict. The unique SKU index and the order-item foreign key are the final checks: their SQLite errors become Chapter 3's DomainValidationException, so the API answers 400.

Create `AdventureWorks.Infrastructure/Data/SqliteErrors.cs` (21 lines).

```csharp
using Microsoft.Data.Sqlite;

namespace AdventureWorks.Infrastructure.Data;

// SQLite's extended result codes for the constraints the store
// relies on (https://sqlite.org/rescode.html).
internal static class SqliteErrors
{
    public const int Check = 275;
    public const int Unique = 2067;
    // ON DELETE RESTRICT runs as an internal trigger, so a
    // refused delete reports SQLITE_CONSTRAINT_TRIGGER.
    public const int RestrictedDelete = 1811;

    // SaveChanges wraps the provider's exception; ExecuteUpdate
    // and ExecuteDelete throw it directly.
    public static bool Is(Exception exception, int code) =>
        (exception as SqliteException
            ?? exception.InnerException as SqliteException)
        ?.SqliteExtendedErrorCode == code;
}
```

Create `AdventureWorks.Infrastructure/Data/EfProductService.cs` (113 lines).

EfProductService.cs, part 1 of 4 (38 lines):

```csharp
using AdventureWorks.Core;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.Infrastructure.Data;

// The catalog in the database, behind Chapter 4's interface. The
// catalog, the v1 and v2 APIs and the pages do not change.
public sealed class EfProductService(AdventureWorksDbContext db)
    : IProductService
{
    public async Task<Product?> GetByIdAsync(int id) =>
        ToProduct(await db.Products.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id));

    public async Task<Product?> GetBySkuAsync(string sku,
        CancellationToken cancellationToken = default) =>
        ToProduct(await db.Products.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Sku == sku,
                cancellationToken));

    public async Task<Product[]> SearchAsync(
        string? query, int page, int pageSize)
    {
        var rows = db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query))
            rows = rows.Where(p =>
                p.Name.ToLower().Contains(query.ToLower()));
        return Map(await rows.OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToArrayAsync());
    }

    public async Task<Product[]> ListByCategoryAsync(
        string category) =>
        Map(await db.Products.AsNoTracking()
            .Where(p => p.Category.ToLower() ==
                category.ToLower())
            .OrderBy(p => p.Id).ToArrayAsync());
```

EfProductService.cs, part 2 of 4 (30 lines):

```csharp
    public async Task<Product> CreateAsync(
        CreateProductRequest request)
    {
        var row = new ProductEntity
        {
            Name = request.Name,
            Sku = request.Sku,
            PriceCents = Money.ToCents(request.Price),
            Category = request.Category
        };
        db.Products.Add(row);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (SqliteErrors.Is(ex, SqliteErrors.Unique))
        {
            // The unique index is the final check, even when two
            // requests race.
            throw new DomainValidationException(
                $"SKU {request.Sku} is already in the catalog.");
        }
        return ToProduct(row)!;
    }

    public async Task<bool> UpdateAsync(
        int id, UpdateProductRequest request) =>
        await SetAsync(db.Products.Where(p => p.Id == id),
            request) == 1;
```

EfProductService.cs, part 3 of 4 (26 lines):

```csharp
    // The version check is part of the UPDATE's WHERE clause, so
    // no other writer can slip in between the check and the save.
    public async Task<UpdateOutcome> UpdateAsync(int id,
        UpdateProductRequest request, int expectedVersion)
    {
        var updated = await SetAsync(db.Products.Where(p =>
            p.Id == id && p.Version == expectedVersion), request);
        if (updated == 1) return UpdateOutcome.Updated;
        return await db.Products.AnyAsync(p => p.Id == id)
            ? UpdateOutcome.Conflict : UpdateOutcome.NotFound;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            return await db.Products.Where(p => p.Id == id)
                .ExecuteDeleteAsync() == 1;
        }
        catch (Exception ex) when (
            SqliteErrors.Is(ex, SqliteErrors.RestrictedDelete))
        {
            throw new DomainValidationException(
                $"Product {id} is on orders, so it stays.");
        }
    }
```

EfProductService.cs, part 4 of 4 (16 lines):

```csharp
    static Task<int> SetAsync(IQueryable<ProductEntity> rows,
        UpdateProductRequest request) =>
        rows.ExecuteUpdateAsync(set => set
            .SetProperty(p => p.Name, request.Name)
            .SetProperty(p => p.PriceCents,
                Money.ToCents(request.Price))
            .SetProperty(p => p.Version, p => p.Version + 1));

    static Product? ToProduct(ProductEntity? row) =>
        row is null ? null : new Product(row.Id, row.Name,
            row.Sku, Money.FromCents(row.PriceCents),
            row.Category, row.Version);

    static Product[] Map(ProductEntity[] rows) =>
        [.. rows.Select(row => ToProduct(row)!)];
}
```

Delete `AdventureWorks.Infrastructure/InMemoryProductService.cs`. Its four products move to the Development seed (Step 5).

In `AdventureWorks/CatalogEndpoints.cs`, replace these lines. Inside AddCatalog. A singleton CatalogAvailability would capture one scoped context for the life of the app; Development's scope validation refuses to start with that.

```csharp
        services.AddSingleton<IProductService,
            InMemoryProductService>();
        services.AddSingleton<CatalogAvailability>();
```

With these:

```csharp
        // The catalog lives in the database now. A DbContext is
        // scoped, so everything that uses one is scoped too.
        services.AddScoped<IProductService, EfProductService>();
        services.AddScoped<CatalogAvailability>();
```

In `AdventureWorks/CatalogEndpoints.cs`, replace these lines.

```csharp
using AdventureWorks.Infrastructure;
```

With these:

```csharp
using AdventureWorks.Infrastructure.Data;
```

### Step 5: Orders in the database, and the Development seed

Order now carries its items, status, notes and Version. Chapter 2's repository was synchronous; the EF Core one is asynchronous, so the order handlers await it, and their routes and results do not change. Two routes are new: GET /api/orders/report, the signed-in owner's orders ten per page with totals summed in SQL, and PUT /api/orders/{id}, which updates the notes only if the client sends back the Version it read. The repository is scoped again, like the context it uses.

In `AdventureWorks.Core/Orders.cs`, replace these lines.

```csharp
public sealed record Order(int Id, string OrderNumber,
    string OwnerId, decimal Total, bool Archived = false);

public interface IOrderRepository
{
    Order? GetById(int id);
    Order Add(string ownerId, decimal total);
    bool Delete(int id);
    bool Archive(int id);
}
```

With these:

```csharp
public sealed record OrderItem(
    int ProductId, int Quantity, decimal UnitPrice);

public sealed record Order(int Id, string OrderNumber,
    string OwnerId, decimal Total, string Status,
    bool Archived, string? Notes, Guid Version,
    IReadOnlyList<OrderItem> Items);

// One row of an owner's order list.
public sealed record OrderSummary(int Id, string OrderNumber,
    decimal Total, string Status, bool Archived);

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<OrderSummary[]> ListAsync(string ownerId, int page);
    Task<UpdateOutcome> UpdateNotesAsync(
        int id, string? notes, Guid version);
    Task<bool> ArchiveAsync(int id);
    Task<bool> DeleteAsync(int id);
}
```

UpdateNotesAsync loads the order, then sets the Version's original value to the one the client sent. EF Core puts the original value in the UPDATE's WHERE clause, so if anyone saved the order after the client read it, no row matches and SaveChangesAsync throws DbUpdateConcurrencyException. Archive and delete are single statements. Chapter 3's simulated failure for order 500 is gone with the in-memory store: order 500 is now an ordinary missing order, 404.

Create `AdventureWorks.Infrastructure/Data/EfOrderRepository.cs` (87 lines).

EfOrderRepository.cs, part 1 of 3 (37 lines):

```csharp
using AdventureWorks.Core;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.Infrastructure.Data;

public sealed class EfOrderRepository(AdventureWorksDbContext db)
    : IOrderRepository
{
    public const int PageSize = 10;

    public async Task<Order?> GetByIdAsync(int id)
    {
        var row = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == id);
        return row is null ? null : ToOrder(row);
    }

    // One bounded page, newest first. SQL sums the cents.
    public async Task<OrderSummary[]> ListAsync(
        string ownerId, int page)
    {
        var rows = await db.Orders.AsNoTracking()
            .Where(o => o.OwnerId == ownerId)
            .OrderByDescending(o => o.Id)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .Select(o => new
            {
                o.Id, o.OrderNumber, o.Status, o.Archived,
                Cents = o.Items.Sum(i =>
                    i.Quantity * i.UnitPriceCents)
            })
            .ToArrayAsync();
        return [.. rows.Select(r => new OrderSummary(r.Id,
            r.OrderNumber, Money.FromCents(r.Cents), r.Status,
            r.Archived))];
    }
```

EfOrderRepository.cs, part 2 of 3 (22 lines):

```csharp
    public async Task<UpdateOutcome> UpdateNotesAsync(
        int id, string? notes, Guid version)
    {
        var row = await db.Orders.FindAsync(id);
        if (row is null) return UpdateOutcome.NotFound;
        // Compare with the version the client read, not the one
        // just loaded: the UPDATE runs WHERE Version = version.
        db.Entry(row).Property(o => o.Version).OriginalValue =
            version;
        row.Notes = notes;
        try
        {
            await db.SaveChangesAsync();
            return UpdateOutcome.Updated;
        }
        catch (DbUpdateConcurrencyException)
        {
            // No row matched: the order changed, or was deleted.
            return await db.Orders.AnyAsync(o => o.Id == id)
                ? UpdateOutcome.Conflict : UpdateOutcome.NotFound;
        }
    }
```

EfOrderRepository.cs, part 3 of 3 (26 lines):

```csharp
    // ExecuteUpdate bypasses SaveChanges, so it sets the new
    // Version itself.
    public async Task<bool> ArchiveAsync(int id)
    {
        var rows = await db.Orders.Where(o => o.Id == id)
            .ExecuteUpdateAsync(set => set
                .SetProperty(o => o.Archived, true)
                .SetProperty(o => o.Version, Guid.NewGuid()));
        return rows == 1;
    }

    // The database deletes the order's items with it (cascade).
    public async Task<bool> DeleteAsync(int id) =>
        await db.Orders.Where(o => o.Id == id)
            .ExecuteDeleteAsync() == 1;

    static Order ToOrder(OrderEntity row)
    {
        OrderItem[] items = [.. row.Items.OrderBy(i => i.Id)
            .Select(i => new OrderItem(i.ProductId, i.Quantity,
                Money.FromCents(i.UnitPriceCents)))];
        return new Order(row.Id, row.OrderNumber, row.OwnerId,
            items.Sum(i => i.Quantity * i.UnitPrice), row.Status,
            row.Archived, row.Notes, row.Version, items);
    }
}
```

In `AdventureWorks.Infrastructure/Services.cs`, delete the OrderRepository class (the in-memory one).

In `AdventureWorks/OrderEndpoints.cs`, replace the AddOrderServices method with the following.

```csharp
    public static IServiceCollection AddOrderServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FeatureFlags>()
            .Bind(configuration.GetSection("FeatureFlags"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<ICacheService,
            MemoryCacheService>();
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddTransient<IEmailValidator, EmailValidator>();
        return services;
    }
```

In `AdventureWorks/OrderEndpoints.cs`, replace the MapOrderEndpoints method with the following. Two new routes: the report and the notes update.

```csharp
    public static IEndpointRouteBuilder MapOrderEndpoints(
        this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/api/orders")
            .WithTags("Orders");
        orders.MapGet("/{id:int}", GetOrder)
            .WithName("GetOrderById");
        orders.MapGet("/report", GetReport);
        orders.MapPut("/{id:int}", UpdateNotes);
        orders.MapDelete("/{id:int}", DeleteOrder)
            .RequireAuthorization(StorePolicies.AdminOnly);
        orders.MapPost("/{id:int}/archive", ArchiveOrder)
            .RequireAuthorization(
                StorePolicies.FinanceDepartment);
        orders.MapPost("/validate", (OrderApiInput input) =>
            TypedResults.Ok(new { input.Sku, input.Quantity }));
        return app;
    }
```

In `AdventureWorks/OrderEndpoints.cs`, replace the GetOrder method with the following. Only the repository call changes: it is awaited now.

```csharp
    static async Task<IResult> GetOrder(int id,
        IOrderRepository orders, IOptions<FeatureFlags> flags,
        IAuthorizationService authorization, ClaimsPrincipal user)
    {
        if (!flags.Value.EnableNewCheckout)
            return Results.StatusCode(
                StatusCodes.Status503ServiceUnavailable);
        if (id <= 0)
            throw new DomainValidationException(
                "Order ID must be positive.");
        var order = await orders.GetByIdAsync(id) ??
            throw new ResourceNotFoundException(
                $"Order {id} was not found.");
        // Load first, then authorize against the loaded order.
        var access = await authorization.AuthorizeAsync(
            user, order, StorePolicies.OrderAccess);
        return access.Succeeded
            ? Results.Ok(order) : Results.Forbid();
    }
```

In `AdventureWorks/OrderEndpoints.cs`, add the following below `static async Task<IResult> GetOrder(int id,`. Chapter 7's AddValidation checks the [Range] on the page parameter: page=0 returns 400.

```csharp
    // The signed-in owner's orders, one bounded page at a time.
    static async Task<IResult> GetReport(
        IOrderRepository orders, ClaimsPrincipal user,
        [Range(1, 1000)] int page = 1)
    {
        var owner = user.FindFirstValue(
            ClaimTypes.NameIdentifier)!;
        return Results.Ok(new
        {
            Page = page,
            Orders = await orders.ListAsync(owner, page)
        });
    }
```

In `AdventureWorks/OrderEndpoints.cs`, add the following below `static async Task<IResult> GetReport(`. Load, authorize the owner (Chapter 9's OrderOwner policy), then update with the version.

```csharp
    // The client sends back the Version it read. If the order has
    // changed since, the update changes nothing: 409.
    static async Task<IResult> UpdateNotes(int id,
        OrderNotesInput input, IOrderRepository orders,
        IAuthorizationService authorization,
        ClaimsPrincipal user, HttpContext context,
        IAntiforgery antiforgery)
    {
        if (!await HasValidTokenAsync(context, antiforgery))
            return Results.BadRequest();
        var order = await orders.GetByIdAsync(id);
        if (order is null) return Results.NotFound();
        var owner = await authorization.AuthorizeAsync(
            user, order, StorePolicies.OrderOwner);
        if (!owner.Succeeded) return Results.Forbid();
        var outcome = await orders.UpdateNotesAsync(
            id, input.Notes, input.Version);
        return outcome switch
        {
            // Null: deleted after the update, before this read.
            UpdateOutcome.Updated =>
                await orders.GetByIdAsync(id) is { } saved
                    ? Results.Ok(saved) : Results.NotFound(),
            UpdateOutcome.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "The order has changed",
                detail: "Read the order again and send its "
                    + "current version."),
            _ => Results.NotFound()
        };
    }
```

In `AdventureWorks/OrderEndpoints.cs`, replace the DeleteOrder method with the following.

```csharp
    static async Task<IResult> DeleteOrder(int id,
        IOrderRepository orders, HttpContext context,
        IAntiforgery antiforgery)
    {
        if (!await HasValidTokenAsync(context, antiforgery))
            return Results.BadRequest();
        return await orders.DeleteAsync(id)
            ? Results.NoContent() : Results.NotFound();
    }
```

In `AdventureWorks/OrderEndpoints.cs`, replace the ArchiveOrder method with the following.

```csharp
    // The endpoint policy already required Department:Finance.
    // The owner rule needs the loaded order, so it runs here.
    static async Task<IResult> ArchiveOrder(int id,
        IOrderRepository orders,
        IAuthorizationService authorization,
        ClaimsPrincipal user, HttpContext context,
        IAntiforgery antiforgery)
    {
        if (!await HasValidTokenAsync(context, antiforgery))
            return Results.BadRequest();
        var order = await orders.GetByIdAsync(id);
        if (order is null) return Results.NotFound();
        var owner = await authorization.AuthorizeAsync(
            user, order, StorePolicies.OrderOwner);
        if (!owner.Succeeded) return Results.Forbid();
        return await orders.ArchiveAsync(id)
            ? Results.NoContent() : Results.NotFound();
    }
```

In `AdventureWorks/OrderEndpoints.cs`, add these lines above the line `using System.Security.Claims;`.

```csharp
using System.ComponentModel.DataAnnotations;
```

In `AdventureWorks/OrderEndpoints.cs`, add these lines after the line `using AdventureWorks.Infrastructure;`.

```csharp
using AdventureWorks.Infrastructure.Data;
```

In `AdventureWorks/OrderInputs.cs`, add the following below `public sealed class OrderApiInput`.

```csharp
// New notes for an order, with the Version the client read.
public sealed class OrderNotesInput
{
    [StringLength(500)]
    public string? Notes { get; set; }

    public Guid Version { get; set; }
}
```

The seed fills each store table only while it is empty, so a restart keeps every change. The four products keep their IDs; casey owns eleven orders (AW-0001 and AW-0003 to AW-0012), so her report has two pages, and jordan owns AW-0002. The helmet has two stock locations for Step 6.

Create `AdventureWorks/StoreSeed.cs` (81 lines).

StoreSeed.cs, part 1 of 3 (21 lines):

```csharp
using AdventureWorks.Infrastructure;
using AdventureWorks.Infrastructure.Data;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks;

// Development data for the store tables. Each part fills only an
// empty table, so a restart keeps every change.
public static class StoreSeed
{
    // id, name, SKU, category, price in cents
    static readonly (int, string, string, string, long)[]
        Catalog =
    [
        (1, "Trail Helmet", "HLM-1001", "accessories", 2999),
        (2, "Trail Jersey", "JRS-2001", "clothing", 4999),
        (3, "Mountain-200 Bike", "BIK-3001", "bikes", 129900),
        (4, "Two-Person Tent", "TNT-4001", "camping", 18900)
    ];
```

StoreSeed.cs, part 2 of 3 (35 lines):

```csharp
    public static async Task AddCatalogAsync(
        AdventureWorksDbContext db)
    {
        if (await db.Products.AnyAsync()) return;
        foreach (var (id, name, sku, category, cents) in Catalog)
            db.Products.Add(new ProductEntity
            {
                Id = id, Name = name, Sku = sku,
                Category = category, PriceCents = cents
            });
        // Two locations for the helmet, for stock transfers.
        db.InventoryItems.AddRange(
            new() { ProductId = 1, Location = "Main",
                Quantity = 10 },
            new() { ProductId = 1, Location = "Overflow" });
        await db.SaveChangesAsync();
    }

    // AW-0002 is jordan's. casey owns the other eleven, so her
    // order list has two pages.
    public static async Task AddOrdersAsync(
        AdventureWorksDbContext db,
        UserManager<ApplicationUser> users)
    {
        if (await db.Orders.AnyAsync()) return;
        var casey = await IdAsync(users, "casey");
        var jordan = await IdAsync(users, "jordan");
        db.Orders.Add(Order(1, casey, productId: 2, 4999));
        db.Orders.Add(Order(2, jordan, productId: 3, 129900));
        db.Orders.Add(Order(3, casey, productId: 4, 18900));
        for (var id = 4; id <= 12; id++)
            db.Orders.Add(Order(id, casey, productId: 1, 2999,
                quantity: id % 3 + 1));
        await db.SaveChangesAsync();
    }
```

StoreSeed.cs, part 3 of 3 (23 lines):

```csharp
    static OrderEntity Order(int id, string ownerId,
        int productId, long unitPriceCents, int quantity = 1) =>
        new()
        {
            Id = id,
            OrderNumber = $"AW-{id:0000}",
            OwnerId = ownerId,
            Items =
            [
                new()
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPriceCents = unitPriceCents
                }
            ]
        };

    static async Task<string> IdAsync(
        UserManager<ApplicationUser> users, string name) =>
        (await users.FindByNameAsync(
            $"{name}@{DevelopmentSeed.Domain}"))!.Id;
}
```

In `AdventureWorks/DevelopmentSeed.cs`, replace the SeedDevelopmentDataAsync method with the following.

```csharp
    public static async Task SeedDevelopmentDataAsync(
        this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services
            .GetRequiredService<AdventureWorksDbContext>();
        await db.Database.MigrateAsync();
        await StoreSeed.AddCatalogAsync(db);
        var password = app.Configuration["Training:Password"];
        if (string.IsNullOrEmpty(password))
        {
            app.Logger.LogWarning(
                "Set Training:Password to create test accounts.");
            return;
        }
        var users = services
            .GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var account in Accounts)
            await CreateAsync(users, account, password);
        await AddAdminAsync(services, users);
        await StoreSeed.AddOrdersAsync(db, users);
    }
```

In `AdventureWorks/DevelopmentSeed.cs`, delete the AddOrders method.

In `AdventureWorks/DevelopmentSeed.cs`, delete these lines.

```csharp
using AdventureWorks.Core;
```

### Step 6: Two saves need an explicit transaction

Each SaveChanges call is its own transaction. A stock transfer saves twice: first the destination gains the quantity, then the source loses it. If the second save fails, the first has already committed, unless both run inside one BeginTransactionAsync. Here the second save fails when the source would go negative (the CK_InventoryItems_Quantity check constraint), and leaving the method without CommitAsync rolls the first save back. The transfer API is for the Admin only.

Create `AdventureWorks.Core/Inventory.cs` (37 lines).

```csharp
using System.ComponentModel.DataAnnotations;

namespace AdventureWorks.Core;

public sealed record StockLevel(string Location, int Quantity);

// Move stock of one product between two of its locations.
public sealed class StockTransfer : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Required, StringLength(50)]
    public string From { get; set; } = "";

    [Required, StringLength(50)]
    public string To { get; set; } = "";

    [Range(1, 1000)]
    public int Quantity { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext context)
    {
        if (From == To)
            yield return new ValidationResult(
                "Use two different locations.", [nameof(To)]);
    }
}

public enum TransferOutcome { Moved, NotFound, NotEnoughStock }

public interface IInventoryRepository
{
    Task<StockLevel[]> GetAsync(int productId);
    Task<TransferOutcome> TransferAsync(StockTransfer transfer);
}
```

Create `AdventureWorks.Infrastructure/Data/EfInventoryRepository.cs` (50 lines).

EfInventoryRepository.cs, part 1 of 2 (14 lines):

```csharp
using AdventureWorks.Core;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.Infrastructure.Data;

public sealed class EfInventoryRepository(
    AdventureWorksDbContext db) : IInventoryRepository
{
    public Task<StockLevel[]> GetAsync(int productId) =>
        db.InventoryItems.AsNoTracking()
            .Where(s => s.ProductId == productId)
            .OrderBy(s => s.Location)
            .Select(s => new StockLevel(s.Location, s.Quantity))
            .ToArrayAsync();
```

EfInventoryRepository.cs, part 2 of 2 (35 lines):

```csharp
    // Two saves, one transaction. The second save fails when the
    // source would go negative; the rollback undoes the first.
    public async Task<TransferOutcome> TransferAsync(
        StockTransfer transfer)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync();
        var from = await FindAsync(transfer.ProductId,
            transfer.From);
        var to = await FindAsync(transfer.ProductId, transfer.To);
        if (from is null || to is null)
            return TransferOutcome.NotFound;
        to.Quantity += transfer.Quantity;
        await db.SaveChangesAsync();
        from.Quantity -= transfer.Quantity;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (SqliteErrors.Is(ex, SqliteErrors.Check))
        {
            // CK_InventoryItems_Quantity refused it. Leaving
            // without a commit rolls the transaction back.
            return TransferOutcome.NotEnoughStock;
        }
        await transaction.CommitAsync();
        return TransferOutcome.Moved;
    }

    Task<InventoryItemEntity?> FindAsync(
        int productId, string location) =>
        db.InventoryItems.SingleOrDefaultAsync(s =>
            s.ProductId == productId && s.Location == location);
}
```

Create `AdventureWorks/InventoryEndpoints.cs` (41 lines).

InventoryEndpoints.cs, part 1 of 2 (21 lines):

```csharp
using AdventureWorks.Authorization;
using AdventureWorks.Core;
using Microsoft.AspNetCore.Antiforgery;

namespace AdventureWorks;

// Stock levels and transfers, for the store's Admin.
public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(
        this IEndpointRouteBuilder app)
    {
        var inventory = app.MapGroup("/api/admin/inventory")
            .WithTags("Inventory")
            .RequireAuthorization(StorePolicies.AdminOnly);
        inventory.MapGet("/{productId:int}", async (
            int productId, IInventoryRepository stock) =>
            Results.Ok(await stock.GetAsync(productId)));
        inventory.MapPost("/transfer", Transfer);
        return app;
    }
```

InventoryEndpoints.cs, part 2 of 2 (19 lines):

```csharp
    static async Task<IResult> Transfer(StockTransfer transfer,
        IInventoryRepository stock, HttpContext context,
        IAntiforgery antiforgery)
    {
        if (!await antiforgery.IsRequestValidAsync(context))
            return Results.BadRequest();
        return await stock.TransferAsync(transfer) switch
        {
            TransferOutcome.Moved => Results.Ok(
                await stock.GetAsync(transfer.ProductId)),
            TransferOutcome.NotEnoughStock => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Not enough stock",
                detail: $"{transfer.From} has fewer than "
                    + $"{transfer.Quantity}."),
            _ => Results.NotFound()
        };
    }
}
```

In `AdventureWorks/CatalogEndpoints.cs`, replace the AddCatalog method with the following. One more scoped registration: the inventory repository.

```csharp
    public static IServiceCollection AddCatalog(
        this IServiceCollection services)
    {
        // The catalog lives in the database now. A DbContext is
        // scoped, so everything that uses one is scoped too.
        services.AddScoped<IProductService, EfProductService>();
        services.AddScoped<IInventoryRepository,
            EfInventoryRepository>();
        services.AddScoped<CatalogAvailability>();
        services.Configure<RouteOptions>(options =>
            options.ConstraintMap.Add("productCategory",
                typeof(ProductCategoryConstraint)));
        return services;
    }
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.MapOrderEndpoints();`.

```csharp
app.MapInventoryEndpoints();
```

## Program.cs at the checkpoint (77 lines)

Program.cs, part 1 of 3 (32 lines):

```csharp
using AdventureWorks;
using AdventureWorks.Authorization;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});
builder.Services.AddOrderServices(builder.Configuration);
builder.Services.AddCatalog();
builder.Services.AddAccountServices(
    builder.Configuration, builder.Environment);
builder.Services.AddStorePolicies();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["correlationId"] =
            context.HttpContext.Items["CorrelationId"]);
builder.Services.AddTransient<CorrelationIdMiddleware>();
builder.Services.AddTransient<DomainExceptionMiddleware>();
builder.Services.AddOpenApi();
builder.Services.AddControllers(options =>
    options.Filters.Add<RequestLoggingFilter>());
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery(options =>
    options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddLocalization();
builder.Services.AddValidation();
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = 1024 * 1024);
```

Program.cs, part 2 of 3 (25 lines):

```csharp
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}
app.UseHttpsRedirection();
app.MapWhen(context => context.Request.Path == "/ping",
    ping => ping.Run(context =>
        context.Response.WriteAsync("pong")));
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();
app.UseMiddleware<DomainExceptionMiddleware>();
app.UseStaticFiles();
app.UseRequestLocalization("en", "fr");
app.UseRouting();
app.UseMiddleware<EndpointLoggingMiddleware>();
app.UseWhen(context =>
    context.Request.Path.StartsWithSegments("/api"),
    api => api.Use(async (context, next) =>
    {
        context.Response.Headers["X-API-Pipeline"] = "true";
        await next();
    }));
```

Program.cs, part 3 of 3 (19 lines):

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStatusEndpoints();
app.MapOrderEndpoints();
app.MapInventoryEndpoints();
app.MapCatalogEndpoints();
app.MapControllers();
app.MapProductEndpointsV2();
app.MapRazorPages();
app.MapAccountEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapDevelopmentEndpoints();
    app.MapDevelopmentSignIn();
    await app.SeedDevelopmentDataAsync();
}
app.Run();
```

## Commands the reader runs and what they print

Build from the working folder:

```shell
dotnet build
```

Result: exit 0, 0 warning(s), 0 error(s) (built with warnings as errors).

Run from the working folder:

```shell
dotnet run --project AdventureWorks
```

It prints (trimmed):

```text
Using launch settings from AdventureWorks\Properties\launchSettings.json...
Building...
```

In Visual Studio 2026, open `AdventureWorks.slnx` and press F5; the https profile starts the same app on https://localhost:7001. A headless Visual Studio 2026 rebuild of this checkpoint (devenv /rebuild Debug) rebuilt 3 project(s) with 0 warning(s) and 0 error(s).

## Requests and real responses

`requests.http` holds these requests in this order; 27 of 27 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `GET /api/catalog` returned **200**

  ```text
  {"items":[{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":...
  ```

- `GET /api/catalog/search?q=helmet` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1}]
  ```

- `GET /api/v2/products/3` returned **200**

  ```text
  {"id":3,"name":"Mountain-200 Bike","sku":"BIK-3001","price":1299.00,"category":"bikes","version":1}
  ```

- `POST /dev/sign-in/casey` returned **200**

  ```text
  {"userName":"casey@adventure-works.test"}
  ```

- `GET /api/orders/1` returned **200**

  ```text
  {"id":1,"orderNumber":"AW-0001","ownerId":"99ff5315-409e-4956-a3de-f3f914f000c1","total":49.99,"status":"Pending","archived":false,"notes":null,"version":"f43056bf-c12a-4f46-8d48-89e6fe8e8d4e","items":[{"productId":2,"qu...
  ```

- `GET /api/orders/report` returned **200**

  ```text
  {"page":1,"orders":[{"id":12,"orderNumber":"AW-0012","total":29.99,"status":"Pending","archived":false},{"id":11,"orderNumber":"AW-0011","total":89.97,"status":"Pending","archived":false},{"id":10,"orderNumber":"AW-0010"...
  ```

- `GET /api/orders/report?page=2` returned **200**

  ```text
  {"page":2,"orders":[{"id":1,"orderNumber":"AW-0001","total":49.99,"status":"Pending","archived":false}]}
  ```

- `GET /api/orders/report?page=0` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"page":["The field page must be between 1 and 1000."]},"traceId":"00-8c205c600dabb4eaa...
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqZ2_fOZ3VUWhRYreSw3N6OJ6PT13BmwFm5ugWAB8GvaJqrfNv75Ar2QToGK6LuGvUXWsWR569XAl7KE3qTlcKQTF1_zW-c8YmcwAE4TLPI12nEA1KyGduSB5l4avHDQzJ4A-GozWYbDn1jvL344pmMWw9adBaROJzaSMjh0bMDmtQ"}
  ```

- `PUT /api/orders/1` returned **200**

  ```text
  {"id":1,"orderNumber":"AW-0001","ownerId":"99ff5315-409e-4956-a3de-f3f914f000c1","total":49.99,"status":"Pending","archived":false,"notes":"Leave it at the side door.","version":"e9317894-ab97-4fb1-857b-3b65114f632f","it...
  ```

- `PUT /api/orders/1` returned **409**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.10","title":"The order has changed","status":409,"detail":"Read the order again and send its current version.","traceId":"00-86ee90bca9e7c63e7a90880a946ee5cd-0f63...
  ```

- `PUT /api/orders/1` returned **200**

  ```text
  {"id":1,"orderNumber":"AW-0001","ownerId":"99ff5315-409e-4956-a3de-f3f914f000c1","total":49.99,"status":"Pending","archived":false,"notes":null,"version":"ebfaf359-fae8-42f6-a208-2b343028a0ae","items":[{"productId":2,"qu...
  ```

- `PUT /api/orders/2` returned **403**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.4","title":"Forbidden","status":403,"traceId":"00-867e72410d86fb09578c30364e748e6a-a4078b3a88462702-02","correlationId":"5305b68f3c914129845b2b60dc351d0e"}
  ```

- `GET /api/admin/inventory/1` returned **403**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.4","title":"Forbidden","status":403,"traceId":"00-ef0673076a2672e59b596be72a24eff4-0c6c8c89d041c031-02","correlationId":"f263cb5fed8c4d44873fd1100871a06b"}
  ```

- `POST /dev/sign-in/avery` returned **200**

  ```text
  {"userName":"avery@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqZRoLdezJ4Edme0xvBVBUUI_I2d7rqYNDChGs9etiwW3-ThwQaTyukEq_oQDsomtfTi6cZV_oftIHsl8mKIiwwyW9c2fWupuIhE8IzlUqpxobvtvKDEsRVy_R-6M1_vY4FMW18yjJ-1eo_52FUpN9nvKMs0JnnrHwx7Xzq05I-_KQ"}
  ```

- `GET /api/admin/inventory/1` returned **200**

  ```text
  [{"location":"Main","quantity":10},{"location":"Overflow","quantity":0}]
  ```

- `POST /api/admin/inventory/transfer` returned **200**

  ```text
  [{"location":"Main","quantity":7},{"location":"Overflow","quantity":3}]
  ```

- `POST /api/admin/inventory/transfer` returned **409**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.10","title":"Not enough stock","status":409,"detail":"Overflow has fewer than 50.","traceId":"00-41726e91a5ee94bcdb6c44da1a9aa502-124c4ba7d3cee080-02","correlatio...
  ```

- `GET /api/admin/inventory/1` returned **200**

  ```text
  [{"location":"Main","quantity":7},{"location":"Overflow","quantity":3}]
  ```

- `POST /api/admin/inventory/transfer` returned **200**

  ```text
  [{"location":"Main","quantity":10},{"location":"Overflow","quantity":0}]
  ```

- `POST /api/admin/inventory/transfer` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"To":["Use two different locations."]},"traceId":"00-5b2cf187e835d74747b67bf03df25ef9-...
  ```

- `POST /api/v2/products` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Invalid request","status":400,"detail":"SKU HLM-1001 is already in the catalog.","traceId":"00-a15e8d7e108ed0ea293d6e0b71e4558a-5f0fbafe239c2c5c-02","...
  ```

- `POST /api/v2/products` returned **201** (Location: https://localhost:7001/api/v2/products/5)

  ```text
  {"id":5,"name":"Trail Bottle","sku":"BTL-5001","price":24.99,"category":"accessories","version":1}
  ```

- `DELETE /api/v2/products/5` returned **204**

- `DELETE /api/v2/products/1` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Invalid request","status":400,"detail":"Product 1 is on orders, so it stays.","traceId":"00-593699327d68d8886c1e3438d0bc00f6-a2c6d13011d37d96-02","cor...
  ```

- `POST /dev/sign-out` returned **204**

### Other runs

- requests.http ran twice against the same database (the requests run, then the README's curl.exe run): 27 of 27 both times, because every change the file makes is undone by a later request.
- Persistence: one process saved a note on order AW-0003; a new process on the same database read it back, then cleared it. Checked.
- Production: with the database deleted, `dotnet tool restore` and `dotnet ef database update -s ..\AdventureWorks` (the tool's default environment is Production) created the schema; started in Production, the test sign-in answered 302 and `/api/catalog` 200 with no products, because nothing migrates or seeds outside Development. Checked.
- Chapter 9's EnsureCreated database (with its -wal and -shm files) under Chapter 10's code: startup fails with `SQLite Error 1: 'table "AspNetRoles" already exists'`, thrown by MigrateAsync. Checked; this is why Step 1 drops the database.
- The reader-path run applied Step 1 to a copy of Chapter 9's real database: `dotnet ef database drop` printed "Successfully dropped database 'main'." and left adventureworks.db-wal and adventureworks.db-shm behind; `dotnet ef database update` then created a fresh database (PRAGMA integrity_check ok, no Chapter 9 accounts) and the two files were gone.

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks.Infrastructure/Data/EfOrderRepository.cs` on line 51:

```csharp
await db.SaveChangesAsync();
```

Press F5 (the https profile), then send PUT https://localhost:7001/api/orders/1 twice as casey, both times with the version read before the first PUT. When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
id = 1 [Int32]
notes = Leave it at the side door. [String]
version = 86c7c101-5696-4acb-a701-429b89891aad [Guid]
row.Version = 86c7c101-5696-4acb-a701-429b89891aad [Guid]
row.Notes = Leave it at the side door. [String]
id = 1 [Int32]
notes = Ring the bell. [String]
version = 86c7c101-5696-4acb-a701-429b89891aad [Guid]
row.Version = 1421f58a-0eb3-456b-a44d-5f2b11aa30f9 [Guid]
row.Notes = Ring the bell. [String]
```

The breakpoint stops twice. On the first PUT, version (what the client sent) equals row.Version (what FindAsync just loaded), so the UPDATE matches the row and SaveChangesAsync gives the order a new Version. On the second PUT the client sends the same, now stale, version: row.Version is the new value, and Step Over throws DbUpdateConcurrencyException, because the UPDATE ran WHERE Version = the stale value and changed no row. The catch returns Conflict, and the endpoint answers 409.

## What the Activity adds

The original Activity asked for SQL Server "if available" without steps (C10-6), and for checks the main checkpoint now makes (report pages, the stale version, constraints, the rollback). This Activity runs the whole store on SQL Server LocalDB and keeps SQLite as the default:

- A new class library, `AdventureWorks.SqlServerMigrations`, holds the SQL Server migrations, because migrations are provider-specific (column types, identity columns). The web project references it so MigrateAsync and the ef tools find it.
- `AddAccountServices` chooses the provider from `Database:Provider`: SqlServer uses `ConnectionStrings:SqlServer` or LocalDB's default instance (database AdventureWorksStore, so an existing AdventureWorks sample database is never touched) with `MigrationsAssembly("AdventureWorks.SqlServerMigrations")`; anything else keeps SQLite.
- `StoreErrors.cs` replaces `SqliteErrors.cs` and recognizes both providers: SQLite 2067, 275 and 1811; SQL Server 2601 or 2627 (duplicate key) and 547 (CHECK or FOREIGN KEY).
- The seed's fixed IDs need IDENTITY_INSERT on SQL Server; `SaveWithIdsAsync` turns it on for one table inside a transaction, so all three statements share one connection.
- A `sqlserver` launch profile sets `Database__Provider`; the ef tools need the variable set in the session.

Checked: the build, the Visual Studio rebuild (four projects) and the migration on RC3; requests.http and the README's curl.exe commands both passed on a LocalDB instance (SQL Server 2025 LocalDB, 17.0) with the sqlserver profile, the same 27 statuses as on SQLite, including the 409 from the CHECK constraint and the 400s from the unique index and the restricted delete; the https profile still runs on SQLite (27 of 27). The check dropped its SQL Server database afterward.

The reader-path check rebuilt `AdventureWorks/Chapter10-Activity` from the Chapter 10 checkpoint: no differences.
The Activity adds 103 lines of code: 33 in 1 new file and 70 new or changed in existing files, plus 8 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

### Activity: the same store on SQL Server

Work in a copy of the Chapter 10 checkpoint folder; Chapter 11 starts from the checkpoint. The Activity runs the same model on SQL Server LocalDB, which Visual Studio 2026 installs with its ASP.NET and data workloads, and keeps SQLite as the default. Migrations are provider-specific: SQLite and SQL Server generate different column types and identity rules, so the SQL Server migrations live in their own class library, selected with MigrationsAssembly.

Run this from the working folder.

```shell
dotnet new classlib -o AdventureWorks.SqlServerMigrations
```

It prints:

```text
The template "Class Library" was created successfully.
Processing post-creation actions...
Restoring AdventureWorks.SqlServerMigrations\AdventureWorks.SqlServerMigrations.csproj:
  Determining projects to restore...
  Restored AdventureWorks.SqlServerMigrations\AdventureWorks.SqlServerMigrations.csproj (in 95 ms).
Restore succeeded.
```

Delete `AdventureWorks.SqlServerMigrations/Class1.cs`.

Run this from the working folder.

```shell
dotnet sln add AdventureWorks.SqlServerMigrations
```

It prints:

```text
Project `AdventureWorks.SqlServerMigrations\AdventureWorks.SqlServerMigrations.csproj` added to the solution.
```

Run this from the `AdventureWorks.SqlServerMigrations` folder.

```shell
dotnet add reference ..\AdventureWorks.Infrastructure
```

It prints:

```text
Reference `..\AdventureWorks.Infrastructure\AdventureWorks.Infrastructure.csproj` added to the project.
```

Run this from the `AdventureWorks` folder. The web project references the migrations library, so its assembly is in the app's output for MigrateAsync and for the dotnet ef tools.

```shell
dotnet add reference ..\AdventureWorks.SqlServerMigrations
```

It prints:

```text
Reference `..\AdventureWorks.SqlServerMigrations\AdventureWorks.SqlServerMigrations.csproj` added to the project.
```

Run this from the `AdventureWorks.Infrastructure` folder.

```shell
dotnet add package Microsoft.EntityFrameworkCore.SqlServer@11.0.0
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.EntityFrameworkCore.SqlServer' into project 'AdventureWorks.Infrastructure\AdventureWorks.Infrastructure.csproj'.
info : Restoring packages for AdventureWorks.Infrastructure\AdventureWorks.Infrastructure.csproj...
info :   GET https://api.nuget.org/v3/vulnerabilities/index.json
info :   OK https://api.nuget.org/v3/vulnerabilities/index.json 4ms
info :   GET https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   GET https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.11.42.14/vulnerability.update.json
info :   OK https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json 4ms
info :   OK https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.11.42.14/vulnerability.update.json 12ms
(8 more lines)
```

The SQL Server provider brings Microsoft.Data.SqlClient, which on RC3 depends on System.Security.Cryptography.Pkcs 9.0.18. NuGet audit reports two known vulnerabilities in that version as warnings NU1902 and NU1903 (errors with warnings as errors). A direct reference to the 11.0.0 package overrides it for every project that references Infrastructure.

Run this from the `AdventureWorks.Infrastructure` folder.

```shell
dotnet add package System.Security.Cryptography.Pkcs@11.0.0
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'System.Security.Cryptography.Pkcs' into project 'AdventureWorks.Infrastructure\AdventureWorks.Infrastructure.csproj'.
info : Restoring packages for AdventureWorks.Infrastructure\AdventureWorks.Infrastructure.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.11.42.14/vulnerability.update.json
info : Package 'System.Security.Cryptography.Pkcs' is compatible with all the specified frameworks in project 'AdventureWorks.Infrastructure\AdventureWorks.Infrastructure.csproj'.
info : PackageReference for package 'System.Security.Cryptography.Pkcs' version '11.0.0' added to file 'AdventureWorks.Infrastructure\AdventureWorks.Infrastructure.csproj'.
info : Writing assets file to disk. Path: AdventureWorks.Infrastructure\obj\project.assets.json
(1 more lines)
```

### Activity: choose the provider from configuration

Database:Provider selects SQL Server; anything else keeps SQLite. The connection string comes from ConnectionStrings:SqlServer, or LocalDB's default instance. The database is named AdventureWorksStore, so it does not touch an existing AdventureWorks sample database.

In `AdventureWorks/AccountServices.cs`, replace these lines. Inside AddAccountServices.

```csharp
        services.AddDbContext<AdventureWorksDbContext>(options =>
            options.UseSqlite(database.ConnectionString));
```

With these:

```csharp
        services.AddDbContext<AdventureWorksDbContext>(options =>
        {
            if (configuration["Database:Provider"] == "SqlServer")
                options.UseSqlServer(
                    configuration.GetConnectionString("SqlServer")
                        ?? LocalDb,
                    sql => sql.MigrationsAssembly(
                        "AdventureWorks.SqlServerMigrations"));
            else
                options.UseSqlite(database.ConnectionString);
        });
```

In `AdventureWorks/AccountServices.cs`, add these lines above the line `public static IServiceCollection AddAccountServices(`. At the top of the class.

```csharp
    // Visual Studio's ASP.NET and data workloads install LocalDB.
    // To use another server, set ConnectionStrings:SqlServer.
    const string LocalDb = @"Server=(localdb)\MSSQLLocalDB;"
        + "Database=AdventureWorksStore;Trusted_Connection=True";
```

### Activity: constraint errors from both providers

SQL Server reports a duplicate key as error 2601 or 2627 and a refused CHECK or FOREIGN KEY constraint as 547. StoreErrors answers the three questions the store asks, for either provider, and replaces SqliteErrors.

Create `AdventureWorks.Infrastructure/Data/StoreErrors.cs` (33 lines).

```csharp
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace AdventureWorks.Infrastructure.Data;

// The constraint errors the store relies on, for both providers.
// SQLite: extended result codes (sqlite.org/rescode.html).
// SQL Server: error numbers 2601 and 2627 (duplicate key) and 547
// (a CHECK or FOREIGN KEY constraint refused the statement).
internal static class StoreErrors
{
    public static bool IsDuplicateKey(Exception exception) =>
        Sqlite(exception) == 2067
        || SqlServer(exception) is 2601 or 2627;

    public static bool IsCheckFailure(Exception exception) =>
        Sqlite(exception) == 275 || SqlServer(exception) == 547;

    // SQLite reports ON DELETE RESTRICT as a trigger error, 1811.
    public static bool IsRestrictedDelete(Exception exception) =>
        Sqlite(exception) == 1811 || SqlServer(exception) == 547;

    // SaveChanges wraps the provider's exception; ExecuteUpdate
    // and ExecuteDelete throw it directly.
    static int? Sqlite(Exception exception) =>
        (exception as SqliteException
            ?? exception.InnerException as SqliteException)
        ?.SqliteExtendedErrorCode;

    static int? SqlServer(Exception exception) =>
        (exception as SqlException
            ?? exception.InnerException as SqlException)?.Number;
}
```

Delete `AdventureWorks.Infrastructure/Data/SqliteErrors.cs`.

In `AdventureWorks.Infrastructure/Data/EfProductService.cs`, replace these lines. In CreateAsync.

```csharp
            when (SqliteErrors.Is(ex, SqliteErrors.Unique))
```

With these:

```csharp
            when (StoreErrors.IsDuplicateKey(ex))
```

In `AdventureWorks.Infrastructure/Data/EfProductService.cs`, replace these lines. In DeleteAsync.

```csharp
        catch (Exception ex) when (
            SqliteErrors.Is(ex, SqliteErrors.RestrictedDelete))
```

With these:

```csharp
        catch (Exception ex)
            when (StoreErrors.IsRestrictedDelete(ex))
```

In `AdventureWorks.Infrastructure/Data/EfInventoryRepository.cs`, replace these lines. In TransferAsync.

```csharp
            when (SqliteErrors.Is(ex, SqliteErrors.Check))
```

With these:

```csharp
            when (StoreErrors.IsCheckFailure(ex))
```

### Activity: explicit IDs on SQL Server

The seed sets the product and order IDs that requests.http uses. SQL Server accepts explicit values in an identity column only while IDENTITY_INSERT is on for that table, and the setting belongs to one connection, so the helper opens a transaction to keep all three statements on it.

In `AdventureWorks/StoreSeed.cs`, replace these lines. In AddCatalogAsync.

```csharp
            new() { ProductId = 1, Location = "Overflow" });
        await db.SaveChangesAsync();
```

With these:

```csharp
            new() { ProductId = 1, Location = "Overflow" });
        await SaveWithIdsAsync(db,
            "SET IDENTITY_INSERT Products ON",
            "SET IDENTITY_INSERT Products OFF");
```

In `AdventureWorks/StoreSeed.cs`, replace these lines. In AddOrdersAsync.

```csharp
                quantity: id % 3 + 1));
        await db.SaveChangesAsync();
```

With these:

```csharp
                quantity: id % 3 + 1));
        await SaveWithIdsAsync(db,
            "SET IDENTITY_INSERT Orders ON",
            "SET IDENTITY_INSERT Orders OFF");
```

In `AdventureWorks/StoreSeed.cs`, add these lines above the line `static OrderEntity Order(int id, string ownerId,`.

```csharp
    // The two statements are literals at each call, not built
    // from a table name, so no SQL is ever assembled from text.
    static async Task SaveWithIdsAsync(AdventureWorksDbContext db,
        string identityInsertOn, string identityInsertOff)
    {
        if (!db.Database.IsSqlServer())
        {
            await db.SaveChangesAsync();
            return;
        }
        await using var transaction =
            await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(identityInsertOn);
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync(identityInsertOff);
        await transaction.CommitAsync();
    }
```

### Activity: a launch profile and the SQL Server migration

A second launch profile sets Database__Provider, so Visual Studio's profile list and dotnet run --launch-profile sqlserver both pick SQL Server. The ef tools do not read launch profiles: set the variable in the PowerShell session first. Run the migration command from the new library's folder, so the migration goes there.

Replace the whole of `AdventureWorks/Properties/launchSettings.json` with the following.

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl":
        "https://localhost:7001;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "sqlserver": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl":
        "https://localhost:7001;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "Database__Provider": "SqlServer"
      }
    }
  }
}
```

Run this from the working folder. Build first: dotnet ef needs every project restored, and the new library never was.

```shell
dotnet build
```

Run this from the `AdventureWorks.SqlServerMigrations` folder.

```shell
$env:Database__Provider = "SqlServer"
dotnet ef migrations add InitialCreate -s ..\AdventureWorks
```

It prints:

```text
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
```

Run with the new profile, `dotnet run --project AdventureWorks --launch-profile sqlserver`, or pick sqlserver in Visual Studio's profile list and press F5. In Development, MigrateAsync creates AdventureWorksStore on LocalDB, applies the SQL Server migration and seeds it; requests.http then gives the same results as on SQLite.

### Activity requests and responses

27 of 27 requests in `Chapter10-Activity/requests.http` returned their commented status. They ran on SQL Server LocalDB with the sqlserver profile. The last five, where the constraint errors come from SQL Server (2601 and 547) instead of SQLite:

- `POST /api/v2/products` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Invalid request","status":400,"detail":"SKU HLM-1001 is already in the catalog.","traceId":"00-64d3e8c9ba3f548f9307ab4b6e17485e-1aff04f211929d65-02","...
  ```

- `POST /api/v2/products` returned **201** (Location: https://localhost:7001/api/v2/products/6)

  ```text
  {"id":6,"name":"Trail Bottle","sku":"BTL-5001","price":24.99,"category":"accessories","version":1}
  ```

- `DELETE /api/v2/products/6` returned **204**

- `DELETE /api/v2/products/1` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Invalid request","status":400,"detail":"Product 1 is on orders, so it stays.","traceId":"00-1a99a134d587114c576dfd46da765695-7e5783ff7c06d1f2-02","cor...
  ```

- `POST /dev/sign-out` returned **204**

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| Step 1 DataModels.cs (97 lines, printed twice) | Replaced | `StoreEntities.cs` (the four entity classes and Money) and `StoreConfiguration.cs` (the rules). IDs are ints; owners are Identity users with a foreign key; money is cents in the tables and decimal everywhere else. No OrderNote and no RowVersion. The context is Chapter 8's AdventureWorksDbContext, not a second ApplicationDbContext. |
| Chapter10.csproj (14 lines, printed twice) | Removed | No new project: `dotnet add package` adds the Design package to the web project. The SQL Server package belongs to the Activity. |
| `.config/dotnet-tools.json` (described) | Replaced | `dotnet new tool-manifest` on the .NET 11 SDK writes `dotnet-tools.json` in the working folder itself, not in `.config`. |
| OrderQueries.cs (49 lines, printed twice) | Replaced | `EfOrderRepository` (ReportAsync becomes ListAsync, DetailsAsync becomes GetByIdAsync, AddNoteAsync becomes UpdateNotesAsync with the client's version) and `EfInventoryRepository.TransferAsync`, the explicit transaction. |
| Program.cs (45 lines) | Replaced | Chapter 9's Program.cs plus `app.MapInventoryEndpoints();`, 77 lines. The DbContext registration stays in AddAccountServices; no second cookie scheme, no SQL Server switch (the Activity has it). |
| `/csrf`, `/api/orders/report`, `PUT /api/orders/{id:guid}` | Replaced | `/api/csrf` (Chapter 9), `/api/orders/report` (ten per page), `PUT /api/orders/{id:int}`. |
| DevSignIn.cs, DevSeed.cs (companion) | Replaced | Chapter 8's DevSignIn; DevelopmentSeed (MigrateAsync) and the new StoreSeed. |

## Notes for the writers

- Departure from "the endpoints do not change": true for the catalog, the v1 and v2 product APIs and the pages, which keep their code. The order handlers keep their routes and results but now await the repository, because Chapter 2's IOrderRepository was synchronous and the EF Core one is not. Say so in one sentence.
- Core's records stay the app's model and EF Core maps separate entity classes in Infrastructure, so the decimal-to-cents conversion happens in one place (design decision 3). Money.FromCents multiplies by 0.01m, which keeps two decimal places (1299.00, not 1299). Review (Chapter 10, blocker 1): do not say SQLite cannot sum decimals. EF Core 11 maps a decimal column to TEXT on SQLite and sums it exactly with its own ef_sum function (the Chapter 10 review's run on RC3). The reason for cents is that SQLite has no exact decimal type, so whole cents in INTEGER columns keep money exact and simple to compare in SQL.
- The SQL Server switch is the Activity, verified on LocalDB, not part of the main checkpoint. The main app stays on SQLite, so every later chapter does too.
- OrderNote is gone (C10-3; it is not in the design's domain). The notes PUT carries the concurrency lesson, and TransferAsync carries the transaction lesson. Figure 10.1's redraw (E12) should show TransferAsync: two SaveChanges calls inside one BeginTransactionAsync to CommitAsync bracket.
- Order has a Status column now ("Pending"), so Chapter 11's cancel needs no second migration.
- The report pages hold ten orders, so the seed's eleven orders for casey fill two pages.
- EF Core 11 RC3 names the migration class after its file: `public partial class _20261103153217_InitialCreate : Migration` in `20261103153217_InitialCreate.cs`. Earlier versions used `InitialCreate`. Do not print a class name the reader's timestamp will not match.
- `dotnet ef` on a project that has never been restored fails with "Unable to retrieve project metadata"; Step 1 builds first. A fresh clone of the companion needs `dotnet tool restore` and a build before any ef command.
- Accuracy 10.1: SQLite in WAL mode keeps -wal and -shm files beside the database while connections are open, and `dotnet ef database drop` deletes only the .db file. The leftovers were harmless here (checked), but the text should not say the folder holds only adventureworks.db.
- Accuracy 10.2: nothing here uses lazy loading. GetByIdAsync loads the items with Include; the report is a projection, so SQL computes only the columns it returns.
- SQLite reports a delete refused by ON DELETE RESTRICT as SQLITE_CONSTRAINT_TRIGGER (1811), not SQLITE_CONSTRAINT_FOREIGNKEY (787, which NO ACTION gives). Checked with the app (the 400) and with SQLite directly.
- SQLite ignores HasMaxLength (TEXT has no length); the API's validation enforces the lengths, and SQL Server enforces them in the Activity (nvarchar(8) for Sku).
- Review 11-P10, from Chapter 10 on: the PUT notes update answers 404 when the order is deleted part-way through. Deleted after the endpoint's load, before UpdateNotesAsync's FindAsync: NotFound (as before). Deleted between FindAsync and the save: the UPDATE matches no row, so the catch asks the database whether the order still exists (AnyAsync): gone is NotFound, 404; present with another version stays Conflict, 409. Deleted after the save, before the endpoint reads the order back: 404 instead of 200 with an empty body. The same two blocks carry on unchanged into Chapters 11 and 12. Checked with the scratch delete-race probe (tools/scenarios/DeleteRaceProbe.cs and ch10-delete-race.http; X-Delete-At stage, save or reread on casey's orders 10, 11 and 12): all three PUTs answered 404 with the probe's X-Probe-Deleted header, each order then read 404, and a PUT without the header answered 200, and order 500, Chapter 3's old failure, answered 404: 14 of 14 on SQLite, and 14 of 14 in the Activity on SQL Server LocalDB. The same code before the fix (it was unchanged in Chapter 11 until then) answered 404, 409, and 200 with an empty body.
- Review P13: StockTransfer's Validate rule now has a request. A transfer from Main to Main answers 400 before the endpoint runs, with `"errors":{"To":["Use two different locations."]}` (Chapter 7's AddValidation runs the type's Validate method), on SQLite and on SQL Server.
- EF Core logs each SQL command at Information, so the Development console shows them, and it logs a failed SaveChanges at Error ("An exception occurred in the database while saving changes") even when the code catches the exception and answers 400 or 409. Expect those entries after the constraint requests.
- Program.cs is 77 lines.
