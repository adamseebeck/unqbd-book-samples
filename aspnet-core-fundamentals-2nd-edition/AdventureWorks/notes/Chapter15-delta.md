# Chapter 15 delta: Testing ASP.NET Core Applications

Generated November 3, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter15` and the operations that rebuild it from the Chapter 14 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 15 adds `AdventureWorks.Tests` to the running app's solution: unit tests for the order rules and integration tests that run the whole app in memory.

- global.json gains `"test": { "runner": "Microsoft.Testing.Platform" }`, so `dotnet test` runs the xUnit v3 project on Microsoft.Testing.Platform (the C# book's verified setup).
- `AdventureWorks.Tests.csproj`, written by hand in an empty folder: OutputType Exe, xunit.v3.mtp-v2 4.0.1, Microsoft.AspNetCore.Mvc.Testing 11.0.0, a reference to the web project. `dotnet sln add` puts it in AdventureWorks.slnx (five projects).
- Unit (Category=Unit): `Fakes.cs` (FakeOrders, one class for IOrderRepository and IOrderWriter, and FakeUnitOfWork), `FakeCatalog.cs`, `OrderServiceTests.cs` (prices from the catalog, one line per product, unknown products, a taken number, another owner's order) and `ArchitectureTests.cs` (Core uses no ASP.NET Core, EF Core or other AdventureWorks assembly).
- Integration (Category=Integration): `StoreFactory` (WebApplicationFactory<Program> in the Testing environment, an in-memory SQLite database built from the migrations, the Test scheme, a stub for InventoryService, seeded catalog and one customer), `TestAuthHandler`, `StubInventory`, `StoreApiTests` (public catalog, 401 for orders, 201 with Location and ETag, stock from the stub) and `StockFailureTests` (503 after one call and two retries, in a factory of its own).
- The app itself does not change: Program.cs is still 88 lines.

## Files added or changed

| File | Change |
| --- | --- |
| `global.json` | changed |
| `AdventureWorks.Tests/AdventureWorks.Tests.csproj` | added |
| `AdventureWorks.Tests/Unit/Fakes.cs` | added |
| `AdventureWorks.Tests/Unit/FakeCatalog.cs` | added |
| `AdventureWorks.Tests/Unit/OrderServiceTests.cs` | added |
| `AdventureWorks.Tests/Unit/ArchitectureTests.cs` | added |
| `AdventureWorks.Tests/Integration/StoreFactory.cs` | added |
| `AdventureWorks.Tests/Integration/TestAuthHandler.cs` | added |
| `AdventureWorks.Tests/Integration/StubInventory.cs` | added |
| `AdventureWorks.Tests/Integration/StoreApiTests.cs` | added |
| `AdventureWorks.Tests/Integration/StockFailureTests.cs` | added |
| (command) `dotnet sln add AdventureWorks.Tests` | creates or edits project files |

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 96 of 96 files identical and no differences.

### Step 1: A test project on Microsoft.Testing.Platform

The working folder's global.json selects Microsoft.Testing.Platform as the test runner, so `dotnet test` runs the xUnit v3 project directly. Create the AdventureWorks.Tests folder empty and write its project file by hand: a test project made from a template and then edited keeps a stale restore, and its first `dotnet test` can report that no test projects were found. The project is an executable (OutputType Exe), references xunit.v3.mtp-v2 and Microsoft.AspNetCore.Mvc.Testing, and references the web project, which brings Core and Infrastructure with it.

In `global.json`, replace these lines. The last two lines of global.json: the sdk section ends, and a test section follows it.

```json
    }
}
```

With these:

```json
    },
    "test": {
        "runner": "Microsoft.Testing.Platform"
    }
}
```

Create `AdventureWorks.Tests/AdventureWorks.Tests.csproj` (21 lines).

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net11.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3.mtp-v2" Version="4.0.1" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing"
      Version="11.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference
      Include="..\AdventureWorks\AdventureWorks.csproj" />
  </ItemGroup>

</Project>
```

Run this from the working folder.

```shell
dotnet sln add AdventureWorks.Tests
```

It prints:

```text
Project `AdventureWorks.Tests\AdventureWorks.Tests.csproj` added to the solution.
```

### Step 2: Unit tests for the order rules

OrderService lives in Core and depends only on Core interfaces (Chapter 11), so its tests need no host, no HTTP and no database: hand-written fakes stand in for the repository, the writer, the catalog and the unit of work. Each test passes TestContext.Current.CancellationToken, which xUnit v3 cancels when a run is stopped. The architecture test checks Chapter 2's rule on the compiled Core assembly.

Create `AdventureWorks.Tests/Unit/Fakes.cs` (66 lines).

Fakes.cs, part 1 of 2 (31 lines):

```csharp
using AdventureWorks.Core;

namespace AdventureWorks.Tests.Unit;

// Hand-written stand-ins for the contracts OrderService uses.
// The writer and the repository share one list, as the real pair
// share one DbContext.
public sealed class FakeOrders : IOrderRepository, IOrderWriter
{
    public List<Order> Saved { get; } = [];
    public string? StagedStatus { get; private set; }

    public Task<Order?> GetByIdAsync(int id) =>
        Task.FromResult(Saved.FirstOrDefault(o => o.Id == id));

    public Task<Order?> GetByNumberAsync(string orderNumber) =>
        Task.FromResult(Saved.FirstOrDefault(o =>
            o.OrderNumber == orderNumber));

    public Task<OrderSummary[]> ListAsync(
        string ownerId, int page, int pageSize) =>
        Task.FromResult(Array.Empty<OrderSummary>());

    public Task<int> CountAsync(string ownerId) =>
        Task.FromResult(Saved.Count(o => o.OwnerId == ownerId));

    public Task<bool> ArchiveAsync(int id) =>
        Task.FromResult(false);

    public Task<bool> DeleteAsync(int id) =>
        Task.FromResult(false);
```

Fakes.cs, part 2 of 2 (34 lines):

```csharp
    public Task<string> NextOrderNumberAsync(
        CancellationToken ct) =>
        Task.FromResult($"AW-{Saved.Count + 101:0000}");

    public void Add(NewOrder order) => Saved.Add(new Order(
        Saved.Count + 101, order.OrderNumber, order.OwnerId,
        order.Items.Sum(i => i.Quantity * i.UnitPrice),
        OrderStatus.Pending, false, null, Guid.NewGuid(),
        order.Items));

    public Task<bool> SetStatusAsync(int id, string status,
        Guid version, CancellationToken ct)
    {
        StagedStatus = status;
        return Task.FromResult(true);
    }

    public Task<bool> SetNotesAsync(int id, string? notes,
        Guid version, CancellationToken ct) =>
        Task.FromResult(true);
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }
    public Exception? Failure { get; set; }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        Saves++;
        return Failure is null
            ? Task.CompletedTask : Task.FromException(Failure);
    }
}
```

Create `AdventureWorks.Tests/Unit/FakeCatalog.cs` (43 lines).

FakeCatalog.cs, part 1 of 2 (20 lines):

```csharp
using AdventureWorks.Core;

namespace AdventureWorks.Tests.Unit;

// The catalog OrderService reads prices from. Only the method
// the service calls is real; the rest fail if a test calls one.
public sealed class FakeCatalog(params Product[] products)
    : IProductService
{
    public Task<Product[]> GetByIdsAsync(int[] ids,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(products
            .Where(p => ids.Contains(p.Id)).ToArray());

    public Task<Product?> GetByIdAsync(int id) =>
        throw new NotSupportedException();

    public Task<Product?> GetBySkuAsync(string sku,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
```

FakeCatalog.cs, part 2 of 2 (22 lines):

```csharp
    public Task<Product[]> SearchAsync(
        string? query, int page, int pageSize) =>
        throw new NotSupportedException();

    public Task<Product[]> ListByCategoryAsync(string category) =>
        throw new NotSupportedException();

    public Task<Product> CreateAsync(
        CreateProductRequest request) =>
        throw new NotSupportedException();

    public Task<bool> UpdateAsync(
        int id, UpdateProductRequest request) =>
        throw new NotSupportedException();

    public Task<UpdateOutcome> UpdateAsync(int id,
        UpdateProductRequest request, int expectedVersion) =>
        throw new NotSupportedException();

    public Task<bool> DeleteAsync(int id) =>
        throw new NotSupportedException();
}
```

Create `AdventureWorks.Tests/Unit/OrderServiceTests.cs` (93 lines).

OrderServiceTests.cs, part 1 of 3 (30 lines):

```csharp
using AdventureWorks.Core;
using Xunit;

namespace AdventureWorks.Tests.Unit;

// Chapter 11's rules, tested without HTTP or a database.
[Trait("Category", "Unit")]
public sealed class OrderServiceTests
{
    static readonly Product Helmet = new(1, "Trail Helmet",
        "HLM-1001", 29.99m, "accessories", 1);
    static readonly Product Tent = new(4, "Two-Person Tent",
        "TNT-4001", 189.00m, "camping", 1);

    readonly FakeOrders _orders = new();
    readonly FakeUnitOfWork _unitOfWork = new();
    readonly OrderService _service;

    public OrderServiceTests() => _service = new OrderService(
        _orders, _orders, new FakeCatalog(Helmet, Tent),
        _unitOfWork);

    static CreateOrderRequest Lines(
        params (int ProductId, int Quantity)[] lines) => new()
    {
        Items = [.. lines.Select(l => new OrderLineRequest
        {
            ProductId = l.ProductId, Quantity = l.Quantity
        })]
    };
```

OrderServiceTests.cs, part 2 of 3 (30 lines):

```csharp
    [Fact]
    public async Task PricesComeFromTheCatalog()
    {
        var result = await _service.CreateAsync(
            "casey", Lines((1, 2), (4, 1)),
            TestContext.Current.CancellationToken);

        Assert.Equal(OrderOutcome.Created, result.Outcome);
        Assert.Equal(248.98m, result.Order!.Total);
        Assert.Equal("casey", result.Order.OwnerId);
        Assert.Equal(1, _unitOfWork.Saves);
    }

    [Fact]
    public async Task OneLinePerProduct()
    {
        var result = await _service.CreateAsync(
            "casey", Lines((1, 1), (1, 2)),
            TestContext.Current.CancellationToken);

        Assert.Equal(OrderOutcome.Invalid, result.Outcome);
        Assert.Equal(0, _unitOfWork.Saves);
    }

    [Fact]
    public async Task UnknownProductsAreRefused()
    {
        var result = await _service.CreateAsync(
            "casey", Lines((99, 1)),
            TestContext.Current.CancellationToken);
```

OrderServiceTests.cs, part 3 of 3 (31 lines):

```csharp
        Assert.Equal(OrderOutcome.Invalid, result.Outcome);
        Assert.Empty(_orders.Saved);
    }

    [Fact]
    public async Task ATakenNumberIsAConflict()
    {
        _unitOfWork.Failure = new OrderNumberConflictException(
            new InvalidOperationException("unique index"));

        var result = await _service.CreateAsync(
            "casey", Lines((1, 1)),
            TestContext.Current.CancellationToken);

        Assert.Equal(OrderOutcome.Conflict, result.Outcome);
    }

    [Fact]
    public async Task AnotherOwnersOrderIsNotFound()
    {
        await _service.CreateAsync("casey", Lines((1, 1)),
            TestContext.Current.CancellationToken);

        var result = await _service.CancelAsync(
            "jordan", _orders.Saved[0].Id,
            TestContext.Current.CancellationToken);

        Assert.Equal(OrderOutcome.NotFound, result.Outcome);
        Assert.Null(_orders.StagedStatus);
    }
}
```

Create `AdventureWorks.Tests/Unit/ArchitectureTests.cs` (23 lines).

```csharp
using AdventureWorks.Core;
using Xunit;

namespace AdventureWorks.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class ArchitectureTests
{
    // Chapter 2's rule: Core depends on nothing but .NET. This
    // reads the assemblies Core's compiled code uses; a package
    // reference whose types are never used does not appear.
    [Fact]
    public void CoreUsesNoWebOrDataTypes()
    {
        var used = typeof(Order).Assembly
            .GetReferencedAssemblies().Select(a => a.Name!);

        Assert.DoesNotContain(used, name =>
            name.StartsWith("Microsoft.AspNetCore") ||
            name.StartsWith("Microsoft.EntityFrameworkCore") ||
            name.StartsWith("AdventureWorks."));
    }
}
```

### Step 3: The whole app in a factory

WebApplicationFactory<Program> starts the real app in memory: its Program.cs, middleware, endpoints and EF Core code. StoreFactory runs it in the Testing environment and replaces three things: the SQLite file with an in-memory SQLite database kept alive by one open connection (built from the same migrations), Identity's cookie with a Test scheme that signs a request in from a header, and InventoryService with a stub message handler. Program needs no `public partial class Program`: since .NET 10 a source generator makes the top-level Program public. The factory finds the web project's folder from the MvcTestingAppManifest.json file that Microsoft.AspNetCore.Mvc.Testing writes beside the tests, not from the solution file.

Create `AdventureWorks.Tests/Integration/StoreFactory.cs` (105 lines).

StoreFactory.cs, part 1 of 4 (29 lines):

```csharp
using System.Data;
using AdventureWorks.Infrastructure;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AdventureWorks.Tests.Integration;

// The whole app in memory, in the Testing environment, with three
// replacements: an in-memory SQLite database, a Test sign-in
// scheme, and a stub for the warehouse's stock service.
public sealed class StoreFactory : WebApplicationFactory<Program>
{
    public const string Customer = "test-customer";
    const string Test = TestAuthHandler.SchemeName;

    // The in-memory database lives as long as this connection.
    private readonly SqliteConnection _connection =
        new("Data Source=:memory:");

    public StubInventory Inventory { get; } = new();
```

StoreFactory.cs, part 2 of 4 (31 lines):

```csharp
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Logging:LogLevel:Default", "Warning");
        builder.ConfigureTestServices(services =>
        {
            // Remove the app's SQLite file registration first.
            services.RemoveAll<DbContextOptions<
                AdventureWorksDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<
                AdventureWorksDbContext>>();
            if (_connection.State != ConnectionState.Open)
                _connection.Open();
            services.AddDbContext<AdventureWorksDbContext>(
                options => options.UseSqlite(_connection));
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions,
                    TestAuthHandler>(Test, null);
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultScheme = Test;
                o.DefaultAuthenticateScheme = Test;
                o.DefaultChallengeScheme = Test;
                o.DefaultForbidScheme = Test;
            });
            services.AddHttpClient<InventoryClient>()
                .ConfigurePrimaryHttpMessageHandler(
                    () => Inventory);
        });
    }
```

StoreFactory.cs, part 3 of 4 (24 lines):

```csharp
    // Build the schema from the migrations and seed what the
    // tests need: the catalog and one customer account. A factory
    // made with WithWebHostBuilder runs this again, so it checks
    // before it adds.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<AdventureWorksDbContext>();
        db.Database.Migrate();
        if (!db.Users.Any(u => u.Id == Customer))
        {
            db.Users.Add(new ApplicationUser
            {
                Id = Customer,
                UserName = "tester@adventure-works.test",
                Email = "tester@adventure-works.test"
            });
            db.SaveChanges();
        }
        StoreSeed.AddCatalogAsync(db).GetAwaiter().GetResult();
        return host;
    }
```

StoreFactory.cs, part 4 of 4 (18 lines):

```csharp
    // A client signed in as the test customer, over HTTPS.
    public HttpClient CreateCustomer()
    {
        var client = CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add(
            TestAuthHandler.Header, Customer);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
```

Create `AdventureWorks.Tests/Integration/TestAuthHandler.cs` (37 lines).

```csharp
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdventureWorks.Tests.Integration;

// Signs a request in as the account named in X-Test-User. Without
// the header the request is anonymous, so the app's fallback
// policy still answers 401. Registered only by StoreFactory.
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(
        options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string Header = "X-Test-User";

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        var id = Request.Headers[Header].ToString();
        if (id.Length == 0)
            return Task.FromResult(AuthenticateResult.NoResult());
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, id),
            new(ClaimTypes.Name, id)
        ];
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, SchemeName)));
    }
}
```

Create `AdventureWorks.Tests/Integration/StubInventory.cs` (31 lines).

```csharp
using System.Net;
using System.Net.Http.Json;

namespace AdventureWorks.Tests.Integration;

// Answers InventoryClient's calls in place of InventoryService:
// seven of every SKU, or the status a test sets.
public sealed class StubInventory : HttpMessageHandler
{
    private int _calls;

    public HttpStatusCode Status { get; set; } =
        HttpStatusCode.OK;
    public int Calls => _calls;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        var sku = request.RequestUri!.Segments[^1];
        var response = Status == HttpStatusCode.OK
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(
                    new { Sku = sku, Available = 7 })
            }
            : new HttpResponseMessage(Status);
        return Task.FromResult(response);
    }
}
```

The integration tests share one factory (one app, one database) through IClassFixture. The failure test gets a factory of its own, because the failing stub trips Chapter 12's circuit breaker, which would leak into other tests.

Create `AdventureWorks.Tests/Integration/StoreApiTests.cs` (73 lines).

StoreApiTests.cs, part 1 of 2 (34 lines):

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AdventureWorks.Tests.Integration;

// The real pipeline, endpoints and EF Core code, over HTTP in
// memory. One factory (one app, one database) for the class.
[Trait("Category", "Integration")]
public sealed class StoreApiTests(StoreFactory factory)
    : IClassFixture<StoreFactory>
{
    static CancellationToken Ct =>
        TestContext.Current.CancellationToken;

    [Fact]
    public async Task TheCatalogIsPublic()
    {
        var client = factory.CreateClient();

        var catalog = await client.GetFromJsonAsync<JsonElement>(
            "/api/catalog", Ct);

        Assert.Equal(4, catalog.GetProperty("items")
            .GetArrayLength());
    }

    [Fact]
    public async Task OrdersNeedASignedInUser()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders", Ct);
```

StoreApiTests.cs, part 2 of 2 (38 lines):

```csharp
        Assert.Equal(HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task APlacedOrderHasALocationAndAnETag()
    {
        var client = factory.CreateCustomer();
        var token = await client.GetFromJsonAsync<JsonElement>(
            "/api/csrf", Ct);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",
            token.GetProperty("token").GetString());

        var response = await client.PostAsJsonAsync("/api/orders",
            new { Items = new[] { new { ProductId = 4,
                Quantity = 1 } } }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var order = await client.GetFromJsonAsync<JsonElement>(
            response.Headers.Location, Ct);
        Assert.Equal(189.00m,
            order.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task StockComesFromTheStub()
    {
        var client = factory.CreateClient();

        var stock = await client.GetFromJsonAsync<JsonElement>(
            "/api/catalog/1/availability", Ct);

        Assert.Equal(7,
            stock.GetProperty("available").GetInt32());
        Assert.True(factory.Inventory.Calls >= 1);
    }
}
```

Create `AdventureWorks.Tests/Integration/StockFailureTests.cs` (31 lines).

```csharp
using System.Net;
using Xunit;

namespace AdventureWorks.Tests.Integration;

// A factory of its own, so the failing stub and the circuit
// breaker it trips cannot affect the other test classes.
[Trait("Category", "Integration")]
public sealed class StockFailureTests : IDisposable
{
    private readonly StoreFactory _factory = new();

    [Fact]
    public async Task AFailingWarehouseIs503()
    {
        _factory.Inventory.Status =
            HttpStatusCode.ServiceUnavailable;
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/catalog/2/availability",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
        // One call and two retries.
        Assert.Equal(3, _factory.Inventory.Calls);
    }

    public void Dispose() => _factory.Dispose();
}
```

### Step 4: Run them

From the working folder. The trait filter runs the unit tests alone, in about a second.

Run this from the working folder.

```shell
dotnet test --project AdventureWorks.Tests
```

It prints:

```text
Running tests from AdventureWorks.Tests\bin\Debug\net11.0\AdventureWorks.Tests.dll (net11.0|x64)
warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]
      Failed to determine the https port for redirect.
warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]
      Failed to determine the https port for redirect.
warn: Polly[3]
      Execution attempt. Source: 'InventoryClient-standard//Standard-Retry', Operation Key: '', Result: '503', Handled: 'True', Attempt: '0', Execution Time: 20.8069ms
warn: Polly[0]
      Resilience event occurred. EventName: 'OnRetry', Source: 'InventoryClient-standard//Standard-Retry', Operation Key: '', Result: '503'
warn: Polly[3]
(12 more lines)
```

Run this from the working folder. The command is longer than the print line; type it as one line.

```shell
dotnet test --project AdventureWorks.Tests --filter-trait "Category=Unit"
```

It prints:

```text
Running tests from AdventureWorks.Tests\bin\Debug\net11.0\AdventureWorks.Tests.dll (net11.0|x64)
AdventureWorks.Tests\bin\Debug\net11.0\AdventureWorks.Tests.dll (net11.0|x64) passed [+6/x0/?0] (647ms)
Test run summary: Passed!
  total: 6
  failed: 0
  succeeded: 6
  skipped: 0
  duration: 1s 050ms
```

## Program.cs at the checkpoint (88 lines)

Program.cs, part 1 of 3 (36 lines):

```csharp
using AdventureWorks;
using AdventureWorks.Authorization;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);
// Lifetime checks in every environment, not only Development.
builder.Host.UseDefaultServiceProvider(options =>
    options.ValidateScopes = options.ValidateOnBuild = true);
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});
builder.Services.AddOrderServices(builder.Configuration);
builder.Services.AddCatalog();
builder.Services.AddStorePerformance(builder.Configuration);
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
builder.Services.AddStoreOpenApi();
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

Program.cs, part 3 of 3 (26 lines):

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
// Compress only the public catalog, never a response that carries
// a token or personal data.
app.UseWhen(context =>
    context.Request.Path.StartsWithSegments("/api/catalog"),
    catalog => catalog.UseResponseCompression());
app.UseRateLimiter();
app.UseOutputCache();
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

In Visual Studio 2026, open `AdventureWorks.slnx` and press F5; the https profile starts the same app on https://localhost:7001. A headless Visual Studio 2026 rebuild of this checkpoint (devenv /rebuild Debug) rebuilt 5 project(s) with 0 warning(s) and 0 error(s).

## Requests and real responses

`requests.http` holds these requests in this order; 6 of 6 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `GET /openapi/store.json` returned **200**

  ```text
  {
    "openapi": "3.2.0",
    "info": {
      "title": "AdventureWorks Store API",
      "description": "Catalog, orders, accounts and Admin routes.",
      "version": "1.0"
    },
    "servers": [
      {
        "url": "https://localhos...
  ```

- `GET /openapi/v1.json` returned **200**

  ```text
  (the OpenAPI JSON document)
  ```

- `GET /openapi/v2.json` returned **200**

  ```text
  {
    "openapi": "3.2.0",
    "info": {
      "title": "AdventureWorks Products API",
      "description": "Minimal APIs, the current version.",
      "version": "2.0"
    },
    "servers": [
      {
        "url": "https://localhost:7001...
  ```

- `GET /openapi/v3.json` returned **404**

  ```text
  No OpenAPI document with the name 'v3' was found.
  ```

- `GET /api/v1/products/1` returned **200**

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1}
  ```

- `GET /api/v2/products/1` returned **200**

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1}
  ```

### Other runs

- `dotnet test --project AdventureWorks.Tests`: total 11, failed 0, succeeded 11, about 9 seconds on RC3; `--filter-trait "Category=Unit"`: total 6, succeeded 6. The Visual Studio 2026 rebuild built all five projects without warnings.
- The tests need neither the app nor InventoryService running; requests.http (the app's) still gave its 6 statuses, and the README's curl.exe run matched.

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks.Tests/Integration/StoreApiTests.cs` on line 53:

```csharp
Assert.Equal(HttpStatusCode.Created, response.StatusCode);
```

Press F5 (the https profile), then send the test APlacedOrderHasALocationAndAnETag (Test Explorer, Debug; or dotnet test from the working folder for the values). When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
response.StatusCode = Created [HttpStatusCode]
response.Headers.Location = https://localhost/api/orders/1 [Uri]
response.Headers.ETag = "6ea6db29-4021-4bb8-9db0-0563a1cfd346" [EntityTagHeaderValue]
factory.Inventory.Calls = 0 [Int32]
```

The whole request ran in memory: the Test scheme signed the request in from its header, the antiforgery check passed with the token from /api/csrf, Chapter 11's service saved the order to the in-memory database, and Chapter 13's endpoint set the Location and ETag. The Location host is localhost, the factory's test server. Step Over the asserts, then into the GET of the Location.

## What the Activity adds

The original Activity's tests carry over to this app (Chapter15-Activity, `ActivityTests.cs`, Moq 4.20.72 added with `dotnet add package`):

- A Moq mock of IProductService: the service calls GetByIdsAsync once, with product 1 and the caller's own cancellation token (Verify with It.Is for the array, because Moq compares arguments with Equals).
- A cancelled request: the mock's callback calls ThrowIfCancellationRequested, the service throws OperationCanceledException, and the unit of work saved nothing.
- WithWebHostBuilder: a second factory with a failing stock stub answers 503 for one test, and the shared factory still answers 200 for the same route. The shared factory's CreateHost runs again for the second factory, so StoreFactory seeds only what is missing and opens the connection only once.

Checked: total 14, succeeded 14; the unit filter 8. `dotnet add package` rewrote the hand-wrapped PackageReference and ProjectReference lines in the test project as single lines; the Activity's project file is the tool's output.

The reader-path check rebuilt `AdventureWorks/Chapter15-Activity` from the Chapter 15 checkpoint: no differences.

### Activity: a mock, a cancelled request and a one-test override

Work in a copy of the Chapter 15 checkpoint folder; Chapter 16 starts from the checkpoint. A Moq mock of IProductService records how the service calls the catalog, so a test can verify the call and the cancellation token it received. A cancelled request must throw and save nothing. WithWebHostBuilder makes a second factory with one more replacement (a failing stock stub) for one test, and the shared factory still answers normally.

Run this from the `AdventureWorks.Tests` folder.

```shell
dotnet add package Moq@4.20.72
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Moq' into project 'AdventureWorks.Tests\AdventureWorks.Tests.csproj'.
info : Restoring packages for AdventureWorks.Tests\AdventureWorks.Tests.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.05.42.04/vulnerability.update.json
info : Package 'Moq' is compatible with all the specified frameworks in project 'AdventureWorks.Tests\AdventureWorks.Tests.csproj'.
info : PackageReference for package 'Moq' version '4.20.72' added to file 'AdventureWorks.Tests\AdventureWorks.Tests.csproj'.
info : Generating MSBuild file AdventureWorks.Tests\obj\AdventureWorks.Tests.csproj.nuget.g.props.
(3 more lines)
```

Create `AdventureWorks.Tests/ActivityTests.cs` (99 lines).

ActivityTests.cs, part 1 of 3 (30 lines):

```csharp
using System.Net;
using AdventureWorks.Core;
using AdventureWorks.Tests.Integration;
using AdventureWorks.Tests.Unit;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AdventureWorks.Tests;

// Activity: a mock that checks how the service calls the catalog,
// and a cancelled request that must save nothing.
[Trait("Category", "Unit")]
public sealed class OrderServiceMockTests
{
    static readonly Product Helmet = new(1, "Trail Helmet",
        "HLM-1001", 29.99m, "accessories", 1);

    readonly Mock<IProductService> _catalog = new();
    readonly FakeOrders _orders = new();
    readonly FakeUnitOfWork _unitOfWork = new();

    OrderService Service() => new(_orders, _orders,
        _catalog.Object, _unitOfWork);

    static CreateOrderRequest OneHelmet() => new()
    {
        Items = [new() { ProductId = 1, Quantity = 1 }]
    };
```

ActivityTests.cs, part 2 of 3 (33 lines):

```csharp
    [Fact]
    public async Task TheCatalogGetsTheCallersToken()
    {
        _catalog.Setup(c => c.GetByIdsAsync(It.IsAny<int[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([Helmet]);
        using var request = new CancellationTokenSource();

        var result = await Service().CreateAsync(
            "casey", OneHelmet(), request.Token);

        Assert.Equal(OrderOutcome.Created, result.Outcome);
        _catalog.Verify(c => c.GetByIdsAsync(
            It.Is<int[]>(ids => ids.SequenceEqual(new[] { 1 })),
            request.Token), Times.Once);
    }

    [Fact]
    public async Task ACancelledRequestSavesNothing()
    {
        _catalog.Setup(c => c.GetByIdsAsync(It.IsAny<int[]>(),
                It.IsAny<CancellationToken>()))
            .Returns((int[] _, CancellationToken token) =>
            {
                token.ThrowIfCancellationRequested();
                return Task.FromResult(new[] { Helmet });
            });
        using var request = new CancellationTokenSource();
        await request.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Service().CreateAsync(
                "casey", OneHelmet(), request.Token));
```

ActivityTests.cs, part 3 of 3 (34 lines):

```csharp
        Assert.Equal(0, _unitOfWork.Saves);
        Assert.Empty(_orders.Saved);
    }
}

// Activity: WithWebHostBuilder changes one test's app only.
[Trait("Category", "Integration")]
public sealed class StockOverrideTests(StoreFactory factory)
    : IClassFixture<StoreFactory>
{
    [Fact]
    public async Task AnOverrideStaysInItsOwnTest()
    {
        using var failing = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddHttpClient<InventoryClient>()
                    .ConfigurePrimaryHttpMessageHandler(() =>
                        new StubInventory
                        {
                            Status =
                                HttpStatusCode.ServiceUnavailable
                        })));
        var ct = TestContext.Current.CancellationToken;

        var changed = await failing.CreateClient().GetAsync(
            "/api/catalog/4/availability", ct);
        var normal = await factory.CreateClient().GetAsync(
            "/api/catalog/4/availability", ct);

        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            changed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, normal.StatusCode);
    }
}
```

Run this from the working folder.

```shell
dotnet test --project AdventureWorks.Tests
```

It prints:

```text
Running tests from AdventureWorks.Tests\bin\Debug\net11.0\AdventureWorks.Tests.dll (net11.0|x64)
warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]
      Failed to determine the https port for redirect.
warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]
      Failed to determine the https port for redirect.
warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]
      Failed to determine the https port for redirect.
warn: Polly[3]
      Execution attempt. Source: 'InventoryClient-standard//Standard-Retry', Operation Key: '', Result: '503', Handled: 'True', Attempt: '0', Execution Time: 28.2723ms
warn: Polly[3]
(26 more lines)
```

### Activity requests and responses

6 of 6 requests in `Chapter15-Activity/requests.http` returned their commented status. They are the checkpoint's requests, with the same results; the Activity adds tests, not routes.

- `GET /openapi/store.json` returned **200**

  ```text
  {
    "openapi": "3.2.0",
    "info": {
      "title": "AdventureWorks Store API",
      "description": "Catalog, orders, accounts and Admin routes.",
      "version": "1.0"
    },
    "servers": [
      {
        "url": "https://localhos...
  ```

- `GET /openapi/v1.json` returned **200**

  ```text
  (the OpenAPI JSON document)
  ```

- `GET /openapi/v2.json` returned **200**

  ```text
  {
    "openapi": "3.2.0",
    "info": {
      "title": "AdventureWorks Products API",
      "description": "Minimal APIs, the current version.",
      "version": "2.0"
    },
    "servers": [
      {
        "url": "https://localhost:7001...
  ```

- `GET /openapi/v3.json` returned **404**

  ```text
  No OpenAPI document with the name 'v3' was found.
  ```

- `GET /api/v1/products/1` returned **200**

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1}
  ```

- `GET /api/v2/products/1` returned **200**

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1}
  ```

## The debugging lab (Chapter15-DebugLab)

A separate copy of the Chapter 15 checkpoint, built like an Activity folder, for the chapter's Visual Studio debugging lesson. It adds one feature with one deliberate bug, and the tests that catch it. Chapter 16 starts from Chapter15, not from this folder.

The feature: ten or more of one product cost 10% less each, rounded to the cent. In `AdventureWorks.Core/OrderService.cs`, CreateAsync now prices each line with `UnitPrice(products[line.ProductId].Price, line.Quantity)` instead of the catalog price, and the class gains:

```csharp
    static decimal UnitPrice(decimal price, int quantity)
    {
        var discounted = quantity > DiscountQuantity;
        if (!discounted) return price;
        return decimal.Round(price * 0.9m, 2);
    }
```

`AdventureWorks.Tests/Unit/VolumeDiscountTests.cs`:

```csharp
using AdventureWorks.Core;
using Xunit;

namespace AdventureWorks.Tests.Unit;

// Debugging lab: the volume discount starts at ten of a product.
[Trait("Category", "Unit")]
public sealed class VolumeDiscountTests
{
    static readonly Product Helmet = new(1, "Trail Helmet",
        "HLM-1001", 29.99m, "accessories", 1);

    readonly FakeOrders _orders = new();

    OrderService Service() => new(_orders, _orders,
        new FakeCatalog(Helmet), new FakeUnitOfWork());

    static CreateOrderRequest Helmets(int quantity) => new()
    {
        Items = [new() { ProductId = 1, Quantity = quantity }]
    };

    [Fact]
    public async Task NineHelmetsPayTheListPrice()
    {
        var result = await Service().CreateAsync("casey",
            Helmets(9), TestContext.Current.CancellationToken);

        Assert.Equal(29.99m, result.Order!.Items[0].UnitPrice);
    }

    [Fact]
    public async Task TenHelmetsCostTenPercentLess()
    {
        var result = await Service().CreateAsync("casey",
            Helmets(10), TestContext.Current.CancellationToken);

        Assert.Equal(26.99m, result.Order!.Items[0].UnitPrice);
    }
}
```

Run the tests from the working folder. On RC3, the full suite reports total 13, failed 1, succeeded 12, and exits with code 2; the discount tests alone:

```powershell
dotnet test --project AdventureWorks.Tests --filter-class `
    AdventureWorks.Tests.Unit.VolumeDiscountTests
```

```text
failed AdventureWorks.Tests.Unit.VolumeDiscountTests.TenHelmetsCostTenPercentLess (6ms)
  Assert.Equal() Failure: Values differ
  Expected: 26.99
  Actual:   29.99
Test run summary: Failed!
  total: 2
  failed: 1
  succeeded: 1
```

Set a breakpoint in `AdventureWorks.Core/OrderService.cs` on line 100:

```csharp
        if (!discounted) return price;
```

In Test Explorer, right-click TenHelmetsCostTenPercentLess and choose Debug. Visual Studio stops twice if you debug the whole class (NineHelmetsPayTheListPrice first) and once for the one test. At the stop for the failing test, the Locals window shows (recorded on RC3 by logging each value just above that line in a scratch copy; the runtime type is in brackets):

```text
price = 29.99 [Decimal]
quantity = 10 [Int32]
discounted = False [Boolean]
```

quantity is 10, the discount quantity, yet discounted is false: the comparison on the line above, `quantity > DiscountQuantity`, excludes ten itself. (For NineHelmetsPayTheListPrice, Locals shows quantity = 9 and discounted = False, which is correct.) DiscountQuantity is a constant, so it is not in Locals; hover over it, or add it to Watch, to see 10.

The fix is one character: change `>` to `>=`.

```csharp
        var discounted = quantity >= DiscountQuantity;
```

With the fix, the full suite on RC3: total 13, failed 0, succeeded 13. The lab folder keeps the bug, so readers can find it themselves; the fixed variant was built and run in a scratch copy only.

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| AdventureWorks.slnx with Domain, Api, Tests (new solution) | Replaced | The running app's AdventureWorks.slnx gains AdventureWorks.Tests; Core is the domain project (C15 inventory: no second Order type). |
| Domain Order(Guid, ProductId, Quantity) | Removed | The tests use the app's Core types and Chapter 11's OrderService. |
| Tests csproj | Replaced | Hand-written in an empty folder (the C# book's MTP finding), xunit.v3.mtp-v2 4.0.1 and Mvc.Testing; no Moq in the checkpoint (the Activity adds it). |
| The test factory | Replaced | StoreFactory: Testing environment, in-memory SQLite from the migrations, the Test scheme, the inventory stub. Role "Administrator" is gone (the app's role is "Admin", Chapter 9). |
| `public partial class Program { }` | Removed | Not needed since .NET 10 (accuracy 15.3). |

## Notes for the writers

- Accuracy 15.1: the solution file is not the content-root marker. Microsoft.AspNetCore.Mvc.Testing writes MvcTestingAppManifest.json beside the test assembly, mapping the AdventureWorks assembly to its project folder, and WebApplicationFactory reads that; it searches for a solution file only when the manifest has no entry.
- Accuracy 15.2: ArchitectureTests reads the assemblies Core's compiled code uses (GetReferencedAssemblies). A package reference whose types are never used is not recorded there, so this test does not catch it; say so beside the test.
- Accuracy 15.3: no `public partial class Program { }` anywhere; .NET 10's source generator makes the top-level Program public.
- EF Core 9 and later register a DbContext's options through IDbContextOptionsConfiguration<TContext>; the factory removes it as well as DbContextOptions<TContext>, or the app's SQLite file registration would still apply.
- The Test scheme replaces Identity's cookie as the default authenticate, challenge and forbid scheme (PostConfigure on AuthenticationOptions), so the fallback policy and Chapter 9's resource rules work unchanged; a client without the X-Test-User header is anonymous and gets 401.
- The factory sets Logging:LogLevel:Default to Warning, so test output is not buried in EF Core's SQL commands.
- xUnit v3 needs `using Xunit;` in each test file here; the project does not add it as a global using.
- The TestAuthHandler's constant is SchemeName, not Scheme: AuthenticationHandler already has a Scheme property, and hiding it is warning CS0108 (an error with warnings as errors).
- `dotnet test --project AdventureWorks.Tests --filter-trait "Category=Unit"` is the MTP form of the old `--filter Category=Unit`.
- Program.cs is unchanged (88 lines).
