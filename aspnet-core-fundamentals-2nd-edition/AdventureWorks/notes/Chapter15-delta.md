# Chapter 15 delta: Testing ASP.NET Core Applications

Generated November 3, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter15` and the operations that rebuild it from the Chapter 14 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 15 adds `AdventureWorks.Tests` to the running app's solution: a few focused unit tests for the order rules and integration tests that run the whole app in memory through one small factory.

- global.json gains `"test": { "runner": "Microsoft.Testing.Platform" }`, so `dotnet test` runs the xUnit v3 project on Microsoft.Testing.Platform (the C# book's verified setup).
- `AdventureWorks.Tests.csproj`, written by hand in an empty folder: OutputType Exe, xunit.v3.mtp-v2 4.0.1, Microsoft.AspNetCore.Mvc.Testing 11.0.0, Moq 4.21.0 (the chapter's pinned version and the newest on nuget.org on November 3), a reference to the web project. `dotnet sln add` puts it in AdventureWorks.slnx (five projects).
- Unit (Category=Unit): `OrderServiceTests.cs`, two tests with Moq standing in for the four contracts (prices come from the catalog; an order deleted before the save is NotFound), and `ArchitectureTests.cs` (Core uses no ASP.NET Core, EF Core or other AdventureWorks assembly).
- Integration (Category=Integration): `StoreFactory` (WebApplicationFactory<Program> in the Testing environment, an in-memory SQLite database built from the migrations, the Test scheme, the seeded catalog and one customer, and CreateCustomerAsync for a signed-in client with the antiforgery header), `TestAuthHandler`, and `StoreApiTests` (401 for orders without a user; 201 with Location and ETag, then the order's total).
- The app itself does not change: Program.cs is still 88 lines.
- Lean pass (coordinator, November 3): the checkpoint dropped the hand-written fakes (FakeOrders, FakeUnitOfWork, FakeCatalog, 109 lines), three of the five service tests, the public-catalog test, and the inventory stub with its two tests (moved to the Activity, where the one-test override needs it). Test code went from 523 to 292 lines.

## Files added or changed

| File | Change |
| --- | --- |
| `global.json` | changed |
| `AdventureWorks.Tests/AdventureWorks.Tests.csproj` | added |
| `AdventureWorks.Tests/Unit/OrderServiceTests.cs` | added |
| `AdventureWorks.Tests/Unit/ArchitectureTests.cs` | added |
| `AdventureWorks.Tests/Integration/StoreFactory.cs` | added |
| `AdventureWorks.Tests/Integration/TestAuthHandler.cs` | added |
| `AdventureWorks.Tests/Integration/StoreApiTests.cs` | added |
| (command) `dotnet sln add AdventureWorks.Tests` | creates or edits project files |

The reader types 297 lines of code: 292 in 6 new files and 5 new or changed in existing files, plus 4 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 92 of 92 files identical and no differences.

### Step 1: A test project on Microsoft.Testing.Platform

The working folder's global.json selects Microsoft.Testing.Platform as the test runner, so `dotnet test` runs the xUnit v3 project directly. Create the AdventureWorks.Tests folder empty and write its project file by hand: a test project made from a template and then edited keeps a stale restore, and its first `dotnet test` can report that no test projects were found. The project is an executable (OutputType Exe), references xunit.v3.mtp-v2, Microsoft.AspNetCore.Mvc.Testing and Moq, and references the web project, which brings Core and Infrastructure with it.

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

Create `AdventureWorks.Tests/AdventureWorks.Tests.csproj` (22 lines).

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
    <PackageReference Include="Moq" Version="4.21.0" />
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

OrderService lives in Core and depends only on Core interfaces (Chapter 11), so its tests need no host, no HTTP and no database: Moq stands in for the repository, the writer, the catalog and the unit of work, and each test sets up only the calls its rule needs. One test checks that prices come from the catalog; the other that an order deleted before the save answers NotFound (Chapter 11). Each passes TestContext.Current.CancellationToken, which xUnit v3 cancels when a run is stopped. The architecture test checks Chapter 2's rule on the compiled Core assembly.

Create `AdventureWorks.Tests/Unit/OrderServiceTests.cs` (60 lines).

OrderServiceTests.cs, part 1 of 2 (29 lines):

```csharp
using AdventureWorks.Core;
using Moq;
using Xunit;

namespace AdventureWorks.Tests.Unit;

// Chapter 11's rules without HTTP or a database. Moq stands in
// for the four contracts OrderService depends on.
[Trait("Category", "Unit")]
public sealed class OrderServiceTests
{
    readonly Mock<IOrderRepository> _orders = new();
    readonly Mock<IOrderWriter> _writer = new();
    readonly Mock<IProductService> _catalog = new();
    readonly Mock<IUnitOfWork> _unitOfWork = new();

    OrderService Service() => new(_orders.Object, _writer.Object,
        _catalog.Object, _unitOfWork.Object);

    [Fact]
    public async Task PricesComeFromTheCatalog()
    {
        _catalog.Setup(c => c.GetByIdsAsync(It.IsAny<int[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Product(4, "Two-Person Tent",
                "TNT-4001", 189.00m, "camping", 1)]);
        NewOrder? staged = null;
        _writer.Setup(w => w.Add(It.IsAny<NewOrder>()))
            .Callback((NewOrder order) => staged = order);
```

OrderServiceTests.cs, part 2 of 2 (30 lines):

```csharp
        await Service().CreateAsync("casey", new()
        {
            Items = [new() { ProductId = 4, Quantity = 2 }]
        }, TestContext.Current.CancellationToken);

        Assert.Equal(189.00m, staged!.Items[0].UnitPrice);
        Assert.Equal("casey", staged.OwnerId);
    }

    // Deleted after the service read it: the save finds no row.
    [Fact]
    public async Task AnOrderDeletedBeforeTheSaveIsNotFound()
    {
        _orders.Setup(o => o.GetByIdAsync(7)).ReturnsAsync(
            new Order(7, "AW-0007", "casey", 29.99m,
                OrderStatus.Pending, false, null, Guid.NewGuid(),
                []));
        _writer.Setup(w => w.SetStatusAsync(7, It.IsAny<string>(),
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _unitOfWork.Setup(u => u.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OrderDeletedException(new()));

        var result = await Service().CancelAsync("casey", 7,
            TestContext.Current.CancellationToken);

        Assert.Equal(OrderOutcome.NotFound, result.Outcome);
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

WebApplicationFactory<Program> starts the real app in memory: its Program.cs, middleware, endpoints and EF Core code. StoreFactory runs it in the Testing environment and replaces two things: the SQLite file with an in-memory SQLite database kept alive by one open connection (built from the same migrations), and Identity's cookie with a Test scheme that signs a request in from a header. CreateCustomerAsync returns a client signed in as the seeded customer, with the antiforgery header order changes need, so each test starts in one line. Program needs no `public partial class Program`: since .NET 10 a source generator makes the top-level Program public. The factory finds the web project's folder from the MvcTestingAppManifest.json file that Microsoft.AspNetCore.Mvc.Testing writes beside the tests, not from the solution file.

Create `AdventureWorks.Tests/Integration/StoreFactory.cs` (107 lines).

StoreFactory.cs, part 1 of 4 (28 lines):

```csharp
using System.Data;
using System.Net.Http.Json;
using System.Text.Json;
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

// The whole app in memory, in the Testing environment, with two
// replacements: an in-memory SQLite database and a Test sign-in.
public sealed class StoreFactory : WebApplicationFactory<Program>
{
    public const string Customer = "test-customer";
    const string Test = TestAuthHandler.SchemeName;

    // The in-memory database lives as long as this connection.
    private readonly SqliteConnection _connection =
        new("Data Source=:memory:");
```

StoreFactory.cs, part 2 of 4 (28 lines):

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

StoreFactory.cs, part 4 of 4 (24 lines):

```csharp
    // A client signed in as the test customer, over HTTPS, with
    // the antiforgery header that order changes require.
    public async Task<HttpClient> CreateCustomerAsync(
        CancellationToken ct)
    {
        var client = CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add(
            TestAuthHandler.Header, Customer);
        var csrf = await client.GetFromJsonAsync<JsonElement>(
            "/api/csrf", ct);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",
            csrf.GetProperty("token").GetString());
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

The integration tests share one factory (one app, one database) through IClassFixture.

Create `AdventureWorks.Tests/Integration/StoreApiTests.cs` (43 lines).

StoreApiTests.cs, part 1 of 2 (20 lines):

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
    [Fact]
    public async Task OrdersNeedASignedInUser()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders",
            TestContext.Current.CancellationToken);
```

StoreApiTests.cs, part 2 of 2 (22 lines):

```csharp
        Assert.Equal(HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task APlacedOrderHasALocationAndAnETag()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateCustomerAsync(ct);

        var response = await client.PostAsJsonAsync("/api/orders",
            new { Items = new[] { new { ProductId = 4,
                Quantity = 1 } } }, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var order = await client.GetFromJsonAsync<JsonElement>(
            response.Headers.Location, ct);
        Assert.Equal(189.00m,
            order.GetProperty("total").GetDecimal());
    }
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
AdventureWorks.Tests\bin\Debug\net11.0\AdventureWorks.Tests.dll (net11.0|x64) passed [+5/x0/?0] (4s 578ms)
Test run summary: Passed!
  total: 5
  failed: 0
  succeeded: 5
  skipped: 0
  duration: 5s 737ms
```

Run this from the working folder. The backtick at the end of the first line continues the command on the next line in PowerShell.

```powershell
dotnet test --project AdventureWorks.Tests `
    --filter-trait "Category=Unit"
```

It prints:

```text
Running tests from AdventureWorks.Tests\bin\Debug\net11.0\AdventureWorks.Tests.dll (net11.0|x64)
AdventureWorks.Tests\bin\Debug\net11.0\AdventureWorks.Tests.dll (net11.0|x64) passed [+3/x0/?0] (683ms)
Test run summary: Passed!
  total: 3
  failed: 0
  succeeded: 3
  skipped: 0
  duration: 1s 036ms
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

- `dotnet test --project AdventureWorks.Tests`: total 5, failed 0, succeeded 5 on RC3; `--filter-trait "Category=Unit"`: total 3, succeeded 3. The Visual Studio 2026 rebuild built all five projects without warnings.
- The tests need neither the app nor InventoryService running; requests.http (the app's) still gave its 6 statuses, and the README's curl.exe run matched.
- The delete-race probe from Chapter 11 (`tools/scenarios/ch13-delete-race.http`) passed against this checkpoint too: 24 of 24.

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks.Tests/Integration/StoreApiTests.cs` on line 36:

```csharp
Assert.Equal(HttpStatusCode.Created, response.StatusCode);
```

Press F5 (the https profile), then send the test APlacedOrderHasALocationAndAnETag (Test Explorer, Debug; or dotnet test from the working folder for the values). When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
response.StatusCode = Created [HttpStatusCode]
response.Headers.Location = https://localhost/api/orders/1 [Uri]
response.Headers.ETag = "a46fc093-3a50-40a4-ad20-0ea2dfad7c1d" [EntityTagHeaderValue]
client.DefaultRequestHeaders.Count() = 2 [Int32]
```

The whole request ran in memory: the Test scheme signed the request in from its header, the antiforgery check passed with the token CreateCustomerAsync fetched from /api/csrf, Chapter 11's service saved the order to the in-memory database, and Chapter 13's endpoint set the Location and ETag. The Location host is localhost, the factory's test server. Step Over the asserts, then into the GET of the Location.

## What the Activity adds

The original Activity's ideas carry over to this app (Chapter15-Activity, `ActivityTests.cs`; Moq is already in the checkpoint, so no package command):

- Moq's Verify: the service calls GetByIdsAsync once, with product 1 and the caller's own cancellation token (It.Is for the array, because Moq compares arguments with Equals).
- A cancelled request: the catalog mock throws OperationCanceledException, the service lets it through, and the unit of work's SaveChangesAsync was never called.
- WithWebHostBuilder: a second factory whose InventoryClient uses StubInventory, a handler that answers every call with 503 and counts them, for one test only. The stock route answers 503 after three calls, one and Chapter 12's two retries. The shared factory's CreateHost runs again for the second factory, so StoreFactory seeds only what is missing and opens the connection only once.

Checked: total 8, succeeded 8; the unit filter 5.

The reader-path check rebuilt `AdventureWorks/Chapter15-Activity` from the Chapter 15 checkpoint: no differences.
The Activity adds 103 lines of code: 103 in 1 new file and 0 new or changed in existing files, plus 1 command line. Blank lines in listings are counted; deleted lines and files that a command writes are not.

### Activity: a verified call, a cancelled request and a one-test override

Work in a copy of the Chapter 15 checkpoint folder; Chapter 16 starts from the checkpoint. Moq's Verify checks that the service passed the caller's cancellation token to the catalog. A cancelled request must throw and save nothing. WithWebHostBuilder makes a second factory with one more replacement for one test: a stub that answers every stock call with 503, so the endpoint answers 503 after one call and Chapter 12's two retries.

Create `AdventureWorks.Tests/ActivityTests.cs` (103 lines).

ActivityTests.cs, part 1 of 3 (34 lines):

```csharp
using System.Net;
using AdventureWorks.Core;
using AdventureWorks.Tests.Integration;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AdventureWorks.Tests;

// Activity: what the service passes to the catalog, and a
// cancelled request that must save nothing.
[Trait("Category", "Unit")]
public sealed class OrderServiceMockTests
{
    readonly Mock<IProductService> _catalog = new();
    readonly Mock<IUnitOfWork> _unitOfWork = new();

    OrderService Service() => new(Mock.Of<IOrderRepository>(),
        Mock.Of<IOrderWriter>(), _catalog.Object,
        _unitOfWork.Object);

    static CreateOrderRequest OneHelmet() => new()
    {
        Items = [new() { ProductId = 1, Quantity = 1 }]
    };

    [Fact]
    public async Task TheCatalogGetsTheCallersToken()
    {
        using var request = new CancellationTokenSource();

        await Service().CreateAsync("casey", OneHelmet(),
            request.Token);
```

ActivityTests.cs, part 2 of 3 (39 lines):

```csharp
        _catalog.Verify(c => c.GetByIdsAsync(
            It.Is<int[]>(ids => ids.SequenceEqual(new[] { 1 })),
            request.Token), Times.Once);
    }

    [Fact]
    public async Task ACancelledRequestSavesNothing()
    {
        _catalog.Setup(c => c.GetByIdsAsync(It.IsAny<int[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        using var request = new CancellationTokenSource();
        await request.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Service().CreateAsync(
                "casey", OneHelmet(), request.Token));

        _unitOfWork.Verify(u => u.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }
}

// Activity: WithWebHostBuilder changes one test's app only; the
// class's shared factory keeps the real InventoryClient.
[Trait("Category", "Integration")]
public sealed class StockOverrideTests(StoreFactory factory)
    : IClassFixture<StoreFactory>
{
    [Fact]
    public async Task AFailingWarehouseIs503()
    {
        var stub = new StubInventory(
            HttpStatusCode.ServiceUnavailable);
        using var failing = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddHttpClient<InventoryClient>()
                    .ConfigurePrimaryHttpMessageHandler(
                        () => stub)));
```

ActivityTests.cs, part 3 of 3 (28 lines):

```csharp
        var response = await failing.CreateClient().GetAsync(
            "/api/catalog/4/availability",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
        // One call and Chapter 12's two retries.
        Assert.Equal(3, stub.Calls);
    }
}

// Answers InventoryClient's calls in place of InventoryService
// with one status, and counts them.
public sealed class StubInventory(HttpStatusCode status)
    : HttpMessageHandler
{
    private int _calls;

    public int Calls => _calls;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        return Task.FromResult(new HttpResponseMessage(status));
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
warn: Polly[3]
      Execution attempt. Source: 'InventoryClient-standard//Standard-Retry', Operation Key: '', Result: '503', Handled: 'True', Attempt: '0', Execution Time: 23.4749ms
warn: Polly[0]
      Resilience event occurred. EventName: 'OnRetry', Source: 'InventoryClient-standard//Standard-Retry', Operation Key: '', Result: '503'
warn: Polly[3]
      Execution attempt. Source: 'InventoryClient-standard//Standard-Retry', Operation Key: '', Result: '503', Handled: 'True', Attempt: '1', Execution Time: 0.1343ms
warn: Polly[0]
(12 more lines)
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

`AdventureWorks.Tests/Unit/VolumeDiscountTests.cs` (Moq, like the checkpoint's unit tests; each test reads the unit price the service staged through IOrderWriter.Add):

```csharp
using AdventureWorks.Core;
using Moq;
using Xunit;

namespace AdventureWorks.Tests.Unit;

// Debugging lab: the volume discount starts at ten of a product.
[Trait("Category", "Unit")]
public sealed class VolumeDiscountTests
{
    readonly Mock<IProductService> _catalog = new();
    readonly Mock<IOrderWriter> _writer = new();
    NewOrder? _staged;

    public VolumeDiscountTests()
    {
        _catalog.Setup(c => c.GetByIdsAsync(It.IsAny<int[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Product(1, "Trail Helmet",
                "HLM-1001", 29.99m, "accessories", 1)]);
        _writer.Setup(w => w.Add(It.IsAny<NewOrder>()))
            .Callback((NewOrder order) => _staged = order);
    }

    // The unit price OrderService staged for that many helmets.
    async Task<decimal> UnitPriceFor(int helmets)
    {
        var service = new OrderService(
            Mock.Of<IOrderRepository>(), _writer.Object,
            _catalog.Object, Mock.Of<IUnitOfWork>());
        await service.CreateAsync("casey", new()
        {
            Items = [new() { ProductId = 1, Quantity = helmets }]
        }, TestContext.Current.CancellationToken);
        return _staged!.Items[0].UnitPrice;
    }

    [Fact]
    public async Task NineHelmetsPayTheListPrice() =>
        Assert.Equal(29.99m, await UnitPriceFor(9));

    [Fact]
    public async Task TenHelmetsCostTenPercentLess() =>
        Assert.Equal(26.99m, await UnitPriceFor(10));
}
```

Run the tests from the working folder. On RC3, the full suite reports total 7, failed 1, succeeded 6, and exits with code 2; the discount tests alone:

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

Set a breakpoint in `AdventureWorks.Core/OrderService.cs` on line 111:

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

With the fix, the full suite on RC3: total 7, failed 0, succeeded 7. The lab folder keeps the bug, so readers can find it themselves; the fixed variant was built and run in a scratch copy only.

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| AdventureWorks.slnx with Domain, Api, Tests (new solution) | Replaced | The running app's AdventureWorks.slnx gains AdventureWorks.Tests; Core is the domain project (C15 inventory: no second Order type). |
| Domain Order(Guid, ProductId, Quantity) | Removed | The tests use the app's Core types and Chapter 11's OrderService. |
| Tests csproj | Replaced | Hand-written in an empty folder (the C# book's MTP finding), xunit.v3.mtp-v2 4.0.1, Mvc.Testing and Moq. |
| The test factory | Replaced | StoreFactory: Testing environment, in-memory SQLite from the migrations, the Test scheme, CreateCustomerAsync. Role "Administrator" is gone (the app's role is "Admin", Chapter 9). |
| `public partial class Program { }` | Removed | Not needed since .NET 10 (accuracy 15.3). |

## Notes for the writers

- Moq, not hand-written fakes (coordinator, November 3): one line of setup per call a test needs, instead of 109 lines of fakes that implement every member of IOrderRepository, IOrderWriter, IUnitOfWork and IProductService. The pinned version, 4.21.0, is on nuget.org (the newest on November 3, 2026, and the version the chapter already pins) and restored cleanly with warnings as errors (no vulnerability warnings). It is a lib-only package: the nupkg holds lib/net462, net6.0, netstandard2.0 and netstandard2.1, a readme, an icon and its signature, and no analyzers, build or buildTransitive folder, so nothing runs at build time; its one dependency for net6.0, Castle.Core 5.2.1, is lib-only too. Moq.dll contains no SponsorLink type or string; its one mention of Devlooped is the AssemblyCompany attribute. License BSD-3-Clause.
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
