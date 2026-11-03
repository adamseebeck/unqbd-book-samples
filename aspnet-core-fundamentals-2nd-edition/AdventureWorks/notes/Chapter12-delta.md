# Chapter 12 delta: Caching, Rate Limiting, and Resilience

Generated November 3, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter12` and the operations that rebuild it from the Chapter 11 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 12 adds caching, rate limiting, compression and a resilient HTTP dependency to the running app, around the catalog and a new stock route. No separate Chapter12 project: the app keeps every earlier feature.

- `InventoryService/`: a companion tool project beside the app, the warehouse's stock service on 127.0.0.1:42132 (GET /stock/{sku}) with /control routes that make the next calls fail with a status, wait, count attempts or reset. It is in the solution so it builds and starts from Visual Studio.
- `InventoryClient.cs`: the typed HttpClient with the standard resilience handler (1-second attempts, 5-second total, two retries 100 ms apart, a breaker at half of at least five attempts in 30 seconds, open for 15 seconds), and `StockAvailability`, a cache-aside read over IDistributedCache (30 seconds).
- `StorePerformance.cs`: AddStorePerformance registers the "catalog" output-cache policy (one minute, tag "catalog"), the distributed memory cache, the partitioned "search" rate limiter (five per ten seconds per signed-in user or client address, with Retry-After), response compression (Brotli and gzip, HTTPS allowed) and the inventory client.
- `CatalogCacheEviction.cs`: a decorator around EfProductService that evicts the "catalog" tag after every successful product write, whichever API or page made it.
- CatalogEndpoints: CacheOutput on the list and the item, the search limiter, and `GET /api/catalog/{id}/availability` (503 Problem Details when the warehouse cannot answer). Program.cs: AddStorePerformance, and after UseAntiforgery a UseWhen branch that compresses /api/catalog only, then UseRateLimiter and UseOutputCache: 88 lines.
- Package: Microsoft.Extensions.Http.Resilience 10.11.0.

## Files added or changed

| File | Change |
| --- | --- |
| `InventoryService/InventoryService.csproj` | added |
| `InventoryService/Properties/launchSettings.json` | added |
| `InventoryService/Program.cs` | added |
| `AdventureWorks/InventoryClient.cs` | added |
| `AdventureWorks/StorePerformance.cs` | added |
| `AdventureWorks/Program.cs` | changed |
| `AdventureWorks/CatalogCacheEviction.cs` | added |
| `AdventureWorks/CatalogEndpoints.cs` | changed |
| (command) `dotnet sln add InventoryService` | creates or edits project files |
| (command) `dotnet add package Microsoft.Extensions.Http.Resilience@10.11.0` | creates or edits project files |

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 84 of 84 files identical and no differences.

### Step 1: A stand-in for the warehouse's stock service

Resilience is tested against a dependency that fails on purpose. InventoryService is a companion tool project, not part of the store: copy its folder from the companion's Chapter12 checkpoint and add it to the solution. It answers GET /stock/{sku} on 127.0.0.1:42132 only, and its /control routes make the next calls fail with a status, wait, or count attempts. Run it in a second terminal with `dotnet run --project InventoryService`, or make both projects startup projects in Visual Studio.

Create `InventoryService/InventoryService.csproj` (9 lines).

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

Create `InventoryService/Properties/launchSettings.json` (14 lines).

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://127.0.0.1:42132",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

Create `InventoryService/Program.cs` (62 lines).

Program.cs, part 1 of 2 (29 lines):

```csharp
using System.Collections.Frozen;

// A stand-in for the warehouse's stock service, so Chapter 12 can
// test caching and resilience without a real one. It listens on
// 127.0.0.1:42132 only (Properties/launchSettings.json), and its
// /control routes make the next calls fail, wait or answer with
// another status.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var stock = new Dictionary<string, int>
{
    ["HLM-1001"] = 42, ["JRS-2001"] = 17,
    ["BIK-3001"] = 3, ["TNT-4001"] = 8
}.ToFrozenDictionary();
var faults = new Faults();

app.MapGet("/stock/{sku}", async (string sku,
    CancellationToken cancellationToken) =>
{
    Interlocked.Increment(ref faults.Attempts);
    if (faults.DelayMs > 0)
        await Task.Delay(faults.DelayMs, cancellationToken);
    if (Interlocked.Decrement(ref faults.FailuresLeft) >= 0)
        return Results.StatusCode(faults.Status);
    return stock.TryGetValue(sku, out var available)
        ? Results.Ok(new { Sku = sku, Available = available })
        : Results.NotFound();
});
```

Program.cs, part 2 of 2 (32 lines):

```csharp
var control = app.MapGroup("/control");
control.MapPost("/fail/{count:int}", (int count) =>
{
    faults.FailuresLeft = count;
    return Results.NoContent();
});
control.MapPost("/status/{code:int}", (int code) =>
{
    faults.Status = code;
    return Results.NoContent();
});
control.MapPost("/delay/{ms:int}", (int ms) =>
{
    faults.DelayMs = ms;
    return Results.NoContent();
});
control.MapGet("/attempts", () => new { faults.Attempts });
control.MapPost("/reset", () =>
{
    faults = new Faults();
    return Results.NoContent();
});

app.Run();

sealed class Faults
{
    public int Attempts;
    public int FailuresLeft;
    public int Status = StatusCodes.Status503ServiceUnavailable;
    public int DelayMs;
}
```

Run this from the working folder.

```shell
dotnet sln add InventoryService
```

It prints:

```text
Project `InventoryService\InventoryService.csproj` added to the solution.
```

### Step 2: A typed client with the standard resilience handler, behind a cache

The standard resilience handler wraps every call in a total timeout, retries, a circuit breaker and a per-attempt timeout, in that order from the outside in. The defaults suit production traffic (10-second attempts, 30-second totals, a breaker that needs 100 calls); these settings are small enough to see in a few requests. The breaker's sampling duration must be at least twice the attempt timeout, or the options fail validation. The retries apply to transient failures only (5xx, 408, 429, timeouts, connection errors); a 400 is not retried. StockAvailability reads the distributed cache first and calls the service only on a miss.

Run this from the `AdventureWorks` folder. This package ships with the .NET extensions on their own version line; 10.11.0 is the newest and supports .NET 11.

```shell
dotnet add package Microsoft.Extensions.Http.Resilience@10.11.0
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.Extensions.Http.Resilience' into project 'AdventureWorks\AdventureWorks.csproj'.
info : Restoring packages for AdventureWorks\AdventureWorks.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.11.42.14/vulnerability.update.json
info : Package 'Microsoft.Extensions.Http.Resilience' is compatible with all the specified frameworks in project 'AdventureWorks\AdventureWorks.csproj'.
info : PackageReference for package 'Microsoft.Extensions.Http.Resilience' version '10.11.0' added to file 'AdventureWorks\AdventureWorks.csproj'.
info : Generating MSBuild file AdventureWorks\obj\AdventureWorks.csproj.nuget.g.props.
(3 more lines)
```

Create `AdventureWorks/InventoryClient.cs` (84 lines).

InventoryClient.cs, part 1 of 3 (25 lines):

```csharp
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace AdventureWorks;

public sealed record WarehouseStock(string Sku, int Available);

// The warehouse's stock service (InventoryService, beside the
// app), called through a typed HttpClient with the standard
// resilience handler: timeouts, retries, a circuit breaker.
public sealed class InventoryClient(HttpClient http)
{
    public async Task<WarehouseStock?> GetAsync(string sku,
        CancellationToken ct)
    {
        using var response = await http.GetAsync(
            $"stock/{Uri.EscapeDataString(sku)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content
            .ReadFromJsonAsync<WarehouseStock>(ct);
    }
}
```

InventoryClient.cs, part 2 of 3 (27 lines):

```csharp
// Cache-aside: read the shared cache, call the service only on a
// miss, and keep the answer for 30 seconds.
public sealed class StockAvailability(IDistributedCache cache,
    InventoryClient client)
{
    static readonly DistributedCacheEntryOptions Entry = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
    };

    public async Task<WarehouseStock?> GetAsync(string sku,
        CancellationToken ct)
    {
        var key = "stock:v1:" + sku;
        var cached = await cache.GetStringAsync(key, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<WarehouseStock>(
                cached, JsonSerializerOptions.Web);
        var stock = await client.GetAsync(sku, ct);
        if (stock is not null)
            await cache.SetStringAsync(key,
                JsonSerializer.Serialize(
                    stock, JsonSerializerOptions.Web),
                Entry, ct);
        return stock;
    }
}
```

InventoryClient.cs, part 3 of 3 (30 lines):

```csharp
public static class InventoryClientSetup
{
    public static IServiceCollection AddInventoryClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<StockAvailability>();
        services.AddHttpClient<InventoryClient>(http =>
                http.BaseAddress = new Uri(
                    configuration["Inventory:BaseUrl"]
                        ?? "http://127.0.0.1:42132/"))
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout =
                    TimeSpan.FromSeconds(1);
                options.TotalRequestTimeout.Timeout =
                    TimeSpan.FromSeconds(5);
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay =
                    TimeSpan.FromMilliseconds(100);
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.SamplingDuration =
                    TimeSpan.FromSeconds(30);
                options.CircuitBreaker.BreakDuration =
                    TimeSpan.FromSeconds(15);
            });
        return services;
    }
}
```

### Step 3: Register caching, rate limiting and compression

One feature file registers the output cache policy, the distributed cache (in memory here; Redis or SQL Server would share it across instances), the rate limiter and response compression, and the inventory client. The "search" limiter is partitioned: each signed-in user, or each client address when nobody is signed in, gets five searches per ten seconds, and a refusal carries Retry-After. Compression over HTTPS is off by default because compressed responses that mix a secret with attacker-chosen text can leak the secret through their size (BREACH); Step 5 applies it to the public catalog only.

Create `AdventureWorks/StorePerformance.cs` (61 lines).

StorePerformance.cs, part 1 of 2 (34 lines):

```csharp
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;

namespace AdventureWorks;

public static class StorePerformance
{
    public const string CatalogTag = "catalog";

    public static IServiceCollection AddStorePerformance(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Anonymous catalog reads, cached for a minute, tagged so
        // that a product write can evict them all at once.
        services.AddOutputCache(options =>
            options.AddPolicy(CatalogTag, policy => policy
                .Expire(TimeSpan.FromMinutes(1))
                .Tag(CatalogTag)));
        // One process here; Redis or SQL Server would share it.
        services.AddDistributedMemoryCache();
        services.AddRateLimiter(ConfigureRateLimits);
        services.AddResponseCompression(options =>
        {
            // HTTPS too, but Program.cs sends only the public
            // catalog here: it carries no secret (BREACH).
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        services.AddInventoryClient(configuration);
        return services;
    }
```

StorePerformance.cs, part 2 of 2 (26 lines):

```csharp
    // Five searches per ten seconds for each signed-in user, or
    // for each client address when nobody is signed in.
    static void ConfigureRateLimits(RateLimiterOptions options)
    {
        options.RejectionStatusCode =
            StatusCodes.Status429TooManyRequests;
        options.AddPolicy("search", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.User.Identity?.IsAuthenticated == true
                    ? "user:" + context.User.Identity.Name
                    : "ip:" + context.Connection.RemoteIpAddress,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromSeconds(10)
                }));
        options.OnRejected = (rejected, _) =>
        {
            if (rejected.Lease.TryGetMetadata(
                    MetadataName.RetryAfter, out var wait))
                rejected.HttpContext.Response.Headers.RetryAfter =
                    ((int)wait.TotalSeconds).ToString();
            return ValueTask.CompletedTask;
        };
    }
}
```

In `AdventureWorks/Program.cs`, add these lines after the line `builder.Services.AddCatalog();`.

```csharp
builder.Services.AddStorePerformance(builder.Configuration);
```

### Step 4: Cache the catalog, evict it on writes, and read stock

The catalog list and item pages are output-cached for a minute under the "catalog" tag. Output caching skips signed-in requests and responses that set cookies by default, so only anonymous reads are cached. A product can change through the v1 controller, the v2 endpoints or the Razor editor, and all three go through IProductService, so one decorator (Chapter 11's pattern) evicts the tag after every successful write. The new availability route answers 503 when the warehouse cannot answer.

Create `AdventureWorks/CatalogCacheEviction.cs` (61 lines).

CatalogCacheEviction.cs, part 1 of 2 (28 lines):

```csharp
using AdventureWorks.Core;
using Microsoft.AspNetCore.OutputCaching;

namespace AdventureWorks;

// Chapter 11's decorator idea again: every product write that
// succeeds evicts the cached catalog pages, whichever API, page
// or controller made it. Reads pass straight through.
public sealed class CatalogCacheEviction(IProductService inner,
    IOutputCacheStore cache) : IProductService
{
    public Task<Product?> GetByIdAsync(int id) =>
        inner.GetByIdAsync(id);

    public Task<Product[]> GetByIdsAsync(int[] ids,
        CancellationToken cancellationToken = default) =>
        inner.GetByIdsAsync(ids, cancellationToken);

    public Task<Product?> GetBySkuAsync(string sku,
        CancellationToken cancellationToken = default) =>
        inner.GetBySkuAsync(sku, cancellationToken);

    public Task<Product[]> SearchAsync(
        string? query, int page, int pageSize) =>
        inner.SearchAsync(query, page, pageSize);

    public Task<Product[]> ListByCategoryAsync(string category) =>
        inner.ListByCategoryAsync(category);
```

CatalogCacheEviction.cs, part 2 of 2 (32 lines):

```csharp
    public async Task<Product> CreateAsync(
        CreateProductRequest request)
    {
        var product = await inner.CreateAsync(request);
        await EvictAsync();
        return product;
    }

    public async Task<bool> UpdateAsync(
        int id, UpdateProductRequest request) =>
        await inner.UpdateAsync(id, request) &&
        await EvictAsync();

    public async Task<UpdateOutcome> UpdateAsync(int id,
        UpdateProductRequest request, int expectedVersion)
    {
        var outcome = await inner.UpdateAsync(
            id, request, expectedVersion);
        if (outcome == UpdateOutcome.Updated) await EvictAsync();
        return outcome;
    }

    public async Task<bool> DeleteAsync(int id) =>
        await inner.DeleteAsync(id) && await EvictAsync();

    async Task<bool> EvictAsync()
    {
        await cache.EvictByTagAsync(
            StorePerformance.CatalogTag, default);
        return true;
    }
}
```

In `AdventureWorks/CatalogEndpoints.cs`, replace the AddCatalog method with the following.

```csharp
    public static IServiceCollection AddCatalog(
        this IServiceCollection services)
    {
        // The catalog lives in the database now. A DbContext is
        // scoped, so everything that uses one is scoped too.
        // Chapter 12: a decorator around EfProductService evicts
        // the cached catalog after every product write.
        services.AddScoped<EfProductService>();
        services.AddScoped<IProductService>(provider =>
            new CatalogCacheEviction(
                provider.GetRequiredService<EfProductService>(),
                provider
                    .GetRequiredService<IOutputCacheStore>()));
        services.AddScoped<IInventoryRepository,
            EfInventoryRepository>();
        services.AddScoped<CatalogAvailability>();
        services.Configure<RouteOptions>(options =>
            options.ConstraintMap.Add("productCategory",
                typeof(ProductCategoryConstraint)));
        return services;
    }
```

In `AdventureWorks/CatalogEndpoints.cs`, replace the MapCatalogEndpoints method with the following. CacheOutput on the list and the item, the search limiter, and the availability route.

```csharp
    public static IEndpointRouteBuilder MapCatalogEndpoints(
        this IEndpointRouteBuilder app)
    {
        var catalog = app.MapGroup("/api/catalog")
            .WithTags("Catalog").AllowAnonymous();
        catalog.MapGet("/", ListCatalog)
            .CacheOutput(StorePerformance.CatalogTag);
        catalog.MapGet("/{id:int}", GetCatalogItem)
            .WithName("GetCatalogItem")
            .CacheOutput(StorePerformance.CatalogTag);
        catalog.MapGet("/search", SearchCatalog)
            .RequireRateLimiting("search");
        catalog.MapGet("/{id:int}/availability", GetAvailability);
        catalog.MapGet("/category/{category:productCategory}",
            ListCategory).WithName("GetCatalogCategory");
        var reviews = catalog.MapGroup("/{productId:int}/reviews")
            .WithTags("Reviews");
        reviews.MapGet("/", (int productId) => Results.Ok(new
        {
            ProductId = productId,
            Reviews = Array.Empty<string>()
        })).WithName("GetProductReviews");
        return app;
    }
```

In `AdventureWorks/CatalogEndpoints.cs`, add the following above `static async Task<IResult> SearchCatalog(`.

```csharp
    // Warehouse stock from InventoryService, through the cache.
    // 503 when it cannot answer: retries used up, the attempt or
    // total timeout passed, or the circuit breaker is open.
    static async Task<IResult> GetAvailability(int id,
        IProductService products, StockAvailability stock,
        CancellationToken ct)
    {
        var product = await products.GetByIdAsync(id);
        if (product is null) return Results.NotFound();
        try
        {
            var found = await stock.GetAsync(product.Sku, ct);
            return found is null ? Results.NotFound()
                : Results.Ok(new { ProductId = id, found.Sku,
                    found.Available });
        }
        catch (Exception ex) when (ex is HttpRequestException
            or TimeoutRejectedException or BrokenCircuitException)
        {
            return Results.Problem(
                "The warehouse did not answer; try again soon.",
                statusCode:
                    StatusCodes.Status503ServiceUnavailable,
                title: "Stock is unavailable");
        }
    }
```

In `AdventureWorks/CatalogEndpoints.cs`, add these lines after the line `using AdventureWorks.Infrastructure.Data;`.

```csharp
using Microsoft.AspNetCore.OutputCaching;
using Polly.CircuitBreaker;
using Polly.Timeout;
```

### Step 5: Put the middleware in order

After antiforgery, in the order the design fixes for the whole app: compression (for the catalog only, through a UseWhen branch), the rate limiter, then the output cache, so a refused request never reaches the cache and a cached response is still compressed on the way out.

In `AdventureWorks/Program.cs`, add these lines after the line `app.UseAntiforgery();`.

```csharp
// Compress only the public catalog, never a response that carries
// a token or personal data.
app.UseWhen(context =>
    context.Request.Path.StartsWithSegments("/api/catalog"),
    catalog => catalog.UseResponseCompression());
app.UseRateLimiter();
app.UseOutputCache();
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

In Visual Studio 2026, open `AdventureWorks.slnx` and press F5; the https profile starts the same app on https://localhost:7001. A headless Visual Studio 2026 rebuild of this checkpoint (devenv /rebuild Debug) rebuilt 4 project(s) with 0 warning(s) and 0 error(s).

## Requests and real responses

`requests.http` holds these requests in this order; 39 of 39 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `POST http://127.0.0.1:42132/control/reset` returned **204**

- `GET /api/catalog/1` returned **200** (no Age header)

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","reviewsLink":"/api/catalog/1/reviews"}
  ```

- `GET /api/catalog/1` returned **200** (Age: 0)

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","reviewsLink":"/api/catalog/1/reviews"}
  ```

- `GET /api/catalog` returned **200** (Content-Encoding: br)

  ```text
  (244 bytes, Brotli-compressed)
  ```

- `GET /api/status` returned **200** (no Content-Encoding header)

  ```text
  {"status":"Operational","timestamp":"2026-11-03T16:23:37.6603165Z"}
  ```

- `GET /api/catalog/1/availability` returned **200**

  ```text
  {"productId":1,"sku":"HLM-1001","available":42}
  ```

- `GET http://127.0.0.1:42132/control/attempts` returned **200**

  ```text
  {"attempts":1}
  ```

- `GET /api/catalog/1/availability` returned **200**

  ```text
  {"productId":1,"sku":"HLM-1001","available":42}
  ```

- `GET http://127.0.0.1:42132/control/attempts` returned **200**

  ```text
  {"attempts":1}
  ```

- `POST http://127.0.0.1:42132/control/fail/2` returned **204**

- `GET /api/catalog/2/availability` returned **200**

  ```text
  {"productId":2,"sku":"JRS-2001","available":17}
  ```

- `GET http://127.0.0.1:42132/control/attempts` returned **200**

  ```text
  {"attempts":4}
  ```

- `POST http://127.0.0.1:42132/control/status/400` returned **204**

- `POST http://127.0.0.1:42132/control/fail/1` returned **204**

- `GET /api/catalog/3/availability` returned **503**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.6.4","title":"Stock is unavailable","status":503,"detail":"The warehouse did not answer; try again soon.","traceId":"00-9655d86d399dfcb5a512a5f99d83cc6e-dee2e2e92cc...
  ```

- `GET http://127.0.0.1:42132/control/attempts` returned **200**

  ```text
  {"attempts":5}
  ```

- `POST http://127.0.0.1:42132/control/reset` returned **204**

- `POST http://127.0.0.1:42132/control/fail/100` returned **204**

- `GET /api/catalog/4/availability` returned **503**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.6.4","title":"Stock is unavailable","status":503,"detail":"The warehouse did not answer; try again soon.","traceId":"00-201b626c77d27f3867c11f5f858c5608-d475bdbe0f7...
  ```

- `GET http://127.0.0.1:42132/control/attempts` returned **200**

  ```text
  {"attempts":1}
  ```

- `GET /api/catalog/4/availability` returned **503**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.6.4","title":"Stock is unavailable","status":503,"detail":"The warehouse did not answer; try again soon.","traceId":"00-9c6a581053e1d5d5d532733d12359100-06e533ceba2...
  ```

- `GET http://127.0.0.1:42132/control/attempts` returned **200**

  ```text
  {"attempts":1}
  ```

- `POST http://127.0.0.1:42132/control/reset` returned **204**

- `GET /api/catalog/search?q=trail` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1}]
  ```

- `GET /api/catalog/search?q=trail` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1}]
  ```

- `GET /api/catalog/search?q=trail` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1}]
  ```

- `GET /api/catalog/search?q=trail` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1}]
  ```

- `GET /api/catalog/search?q=trail` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1}]
  ```

- `GET /api/catalog/search?q=trail` returned **429** (Retry-After: 9)

  ```text
  {"title":"Too Many Requests","status":429,"traceId":"00-8c1d39b96429e3ba23ffa56f610d7dae-be64ad6193f61883-02","correlationId":"2868ea256846404aabf6397ea394b9f8"}
  ```

- `POST /dev/sign-in/casey` returned **200**

  ```text
  {"userName":"casey@adventure-works.test"}
  ```

- `GET /api/catalog/search?q=trail` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1}]
  ```

- `POST /dev/sign-in/avery` returned **200**

  ```text
  {"userName":"avery@adventure-works.test"}
  ```

- `PUT /api/v2/products/1` returned **204**

- `POST /dev/sign-out` returned **204**

- `GET /api/catalog/1` returned **200** (no Age header)

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":31.99,"category":"accessories","reviewsLink":"/api/catalog/1/reviews"}
  ```

- `POST /dev/sign-in/avery` returned **200**

  ```text
  {"userName":"avery@adventure-works.test"}
  ```

- `PUT /api/v2/products/1` returned **204**

- `POST /dev/sign-out` returned **204**

- `GET /api/catalog/1` returned **200**

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","reviewsLink":"/api/catalog/1/reviews"}
  ```

### Other runs

- Timeout: with every stock call delayed 1.5 seconds, each 1-second attempt times out; three attempts (with the retry delays between them) end in 503 after about 4 seconds (4.0 here), inside the 5-second total. Checked (scenario timeout).
- Stampede: ten requests at once for a SKU nobody has cached made ten calls to InventoryService (cache-aside cannot coalesce concurrent misses); the Activity fixes this. Checked (scenario stampede).
- The breaker's count depends on everything the client did in the last 30 seconds, so requests.http says to send its requests in order, soon after the app starts; a fresh process (the README's curl.exe run) gave the same 39 statuses.

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks/InventoryClient.cs` on line 45:

```csharp
var stock = await client.GetAsync(sku, ct);
```

Press F5 (the https profile), then send GET https://localhost:7001/api/catalog/1/availability with InventoryService running and nothing cached yet. When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
sku = HLM-1001 [String]
key = stock:v1:HLM-1001 [String]
cached =  []
ct.CanBeCanceled = True [Boolean]
```

The distributed cache had nothing under key, so cached is null and the next line calls InventoryService through the resilience handler. Step Over: the call returns HLM-1001 with 42 available, and the next statement stores it for 30 seconds. Send the request again and the breakpoint is not hit: the cache answers.

## What the Activity adds

The original Activity needs a Redis server (not verifiable here), a Redis lease helper, and a webhook. This one replaces the stock cache-aside with HybridCache (Microsoft.Extensions.Caching.Hybrid 10.11.0): `GetOrCreateAsync` lets one caller fill a missing entry while concurrent callers wait for it, and it keeps the entry in memory and in the registered IDistributedCache.

Checked: ten concurrent requests for an uncached SKU made exactly one call to InventoryService and all ten got 200 with 3 available; the timeout behavior is unchanged; requests.http passed with the checkpoint's results.

The reader-path check rebuilt `AdventureWorks/Chapter12-Activity` from the Chapter 12 checkpoint: no differences.

### Activity: one fill for many concurrent misses

Work in a copy of the Chapter 12 checkpoint folder; Chapter 13 starts from the checkpoint. With cache-aside, every request that misses calls the service, so ten requests at once for an uncached SKU make ten calls (a cache stampede). HybridCache (the .NET extensions' two-level cache) lets one caller fill the entry while the others wait for it. It keeps entries in memory and, when an IDistributedCache is registered, as this app's is, in that cache too.

Run this from the `AdventureWorks` folder.

```shell
dotnet add package Microsoft.Extensions.Caching.Hybrid@10.11.0
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.Extensions.Caching.Hybrid' into project 'AdventureWorks\AdventureWorks.csproj'.
info : Restoring packages for AdventureWorks\AdventureWorks.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.11.42.14/vulnerability.update.json
info : Package 'Microsoft.Extensions.Caching.Hybrid' is compatible with all the specified frameworks in project 'AdventureWorks\AdventureWorks.csproj'.
info : PackageReference for package 'Microsoft.Extensions.Caching.Hybrid' version '10.11.0' added to file 'AdventureWorks\AdventureWorks.csproj'.
info : Generating MSBuild file AdventureWorks\obj\AdventureWorks.csproj.nuget.g.props.
(3 more lines)
```

In `AdventureWorks/InventoryClient.cs`, replace the StockAvailability class with the following.

```csharp
// Activity: HybridCache instead of cache-aside. GetOrCreateAsync
// lets one caller fill a missing entry while the others wait for
// it, so ten requests at once make one call. It keeps entries in
// memory and, when one is registered, in IDistributedCache too.
public sealed class StockAvailability(HybridCache cache,
    InventoryClient client)
{
    static readonly HybridCacheEntryOptions Entry = new()
    {
        Expiration = TimeSpan.FromSeconds(30),
        LocalCacheExpiration = TimeSpan.FromSeconds(30)
    };

    public async Task<WarehouseStock?> GetAsync(string sku,
        CancellationToken ct) =>
        await cache.GetOrCreateAsync("stock:v1:" + sku,
            async token => await client.GetAsync(sku, token),
            Entry, cancellationToken: ct);
}
```

In `AdventureWorks/InventoryClient.cs`, replace these lines.

```csharp
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
```

With these:

```csharp
using Microsoft.Extensions.Caching.Hybrid;
```

In `AdventureWorks/InventoryClient.cs`, add these lines above the line `services.AddScoped<StockAvailability>();`. In AddInventoryClient.

```csharp
        services.AddHybridCache();
```

### Activity requests and responses

39 of 39 requests in `Chapter12-Activity/requests.http` returned their commented status. They are the checkpoint's requests, with the same results; the Activity's change shows only under concurrency (below).

- `POST http://127.0.0.1:42132/control/reset` returned **204**

- `GET /api/catalog/1` returned **200** (no Age header)

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","reviewsLink":"/api/catalog/1/reviews"}
  ```

- `GET /api/catalog/1` returned **200** (Age: 0)

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","reviewsLink":"/api/catalog/1/reviews"}
  ```

- `GET /api/catalog` returned **200** (Content-Encoding: br)

  ```text
  (244 bytes, Brotli-compressed)
  ```

- `GET /api/status` returned **200** (no Content-Encoding header)

  ```text
  {"status":"Operational","timestamp":"2026-11-03T16:25:23.408857Z"}
  ```

- `GET /api/catalog/1/availability` returned **200**

  ```text
  {"productId":1,"sku":"HLM-1001","available":42}
  ```

- `GET http://127.0.0.1:42132/control/attempts` returned **200**

  ```text
  {"attempts":1}
  ```

- `GET /api/catalog/1/availability` returned **200**

  ```text
  {"productId":1,"sku":"HLM-1001","available":42}
  ```

- `GET http://127.0.0.1:42132/control/attempts` returned **200**

  ```text
  {"attempts":1}
  ```

- `POST http://127.0.0.1:42132/control/fail/2` returned **204**

- `GET /api/catalog/2/availability` returned **200**

  ```text
  {"productId":2,"sku":"JRS-2001","available":17}
  ```

- `GET http://127.0.0.1:42132/control/attempts` returned **200**

  ```text
  {"attempts":4}
  ```

- `POST http://127.0.0.1:42132/control/status/400` returned **204**

- `POST http://127.0.0.1:42132/control/fail/1` returned **204**

- `GET /api/catalog/3/availability` returned **503**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.6.4","title":"Stock is unavailable","status":503,"detail":"The warehouse did not answer; try again soon.","traceId":"00-0578a89ffd9a717a82e2488dd64519bb-691319d131a...
  ```

- `GET http://127.0.0.1:42132/control/attempts` returned **200**

  ```text
  {"attempts":5}
  ```

- `POST http://127.0.0.1:42132/control/reset` returned **204**

- `POST http://127.0.0.1:42132/control/fail/100` returned **204**

- `GET /api/catalog/4/availability` returned **503**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.6.4","title":"Stock is unavailable","status":503,"detail":"The warehouse did not answer; try again soon.","traceId":"00-9ad9833859cd656060edf9f475a22378-382856a790a...
  ```

- `GET http://127.0.0.1:42132/control/attempts` returned **200**

  ```text
  {"attempts":1}
  ```

- `GET /api/catalog/4/availability` returned **503**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.6.4","title":"Stock is unavailable","status":503,"detail":"The warehouse did not answer; try again soon.","traceId":"00-1ee900644c623492df3a46e159576338-6400c0c5a6a...
  ```

- `GET http://127.0.0.1:42132/control/attempts` returned **200**

  ```text
  {"attempts":1}
  ```

- `POST http://127.0.0.1:42132/control/reset` returned **204**

- `GET /api/catalog/search?q=trail` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1}]
  ```

- `GET /api/catalog/search?q=trail` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1}]
  ```

- `GET /api/catalog/search?q=trail` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1}]
  ```

- `GET /api/catalog/search?q=trail` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1}]
  ```

- `GET /api/catalog/search?q=trail` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1}]
  ```

- `GET /api/catalog/search?q=trail` returned **429** (Retry-After: 9)

  ```text
  {"title":"Too Many Requests","status":429,"traceId":"00-2f11adfa49087d8be7f4d562a40276f5-4ec90329837008f4-02","correlationId":"506d88ed792848feb7aab60faf45af68"}
  ```

- `POST /dev/sign-in/casey` returned **200**

  ```text
  {"userName":"casey@adventure-works.test"}
  ```

- `GET /api/catalog/search?q=trail` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1}]
  ```

- `POST /dev/sign-in/avery` returned **200**

  ```text
  {"userName":"avery@adventure-works.test"}
  ```

- `PUT /api/v2/products/1` returned **204**

- `POST /dev/sign-out` returned **204**

- `GET /api/catalog/1` returned **200** (no Age header)

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":31.99,"category":"accessories","reviewsLink":"/api/catalog/1/reviews"}
  ```

- `POST /dev/sign-in/avery` returned **200**

  ```text
  {"userName":"avery@adventure-works.test"}
  ```

- `PUT /api/v2/products/1` returned **204**

- `POST /dev/sign-out` returned **204**

- `GET /api/catalog/1` returned **200**

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","reviewsLink":"/api/catalog/1/reviews"}
  ```

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| Chapter12.csproj (11 lines) | Removed | No new project for the app: `dotnet add package` adds the resilience package to the web project. No Redis package (no Redis to verify against; the distributed cache is in memory, and the text names the swap). |
| CatalogServices.cs (62 lines, printed twice) | Replaced | `InventoryClient.cs` (typed client, StockAvailability cache-aside, registration) and `CatalogCacheEviction.cs`. ProductSource and its generation counter are gone: the catalog is Chapter 10's database, and the output cache's Age header shows a cached response. |
| Program.cs (101 lines) | Replaced | Chapter 11's Program.cs plus the AddStorePerformance line and, after UseAntiforgery, the commented compression branch, UseRateLimiter and UseOutputCache; the policies live in StorePerformance.cs. 88 lines. |
| `/api/products/{id}`, `/limited`, `/profile`, `/api/stock`, `/api/inventory/webhook` | Replaced | `/api/catalog/{id}` (cached), `/api/catalog/search` (limited), `/api/catalog/{id}/availability` (resilient, cached). The webhook is not carried over: product writes evict the cache through the decorator. |
| InventoryFixture/ (companion) | Replaced | `InventoryService/`, in the solution, same port and control routes, plus /control/reset. |
| DevSignIn.cs (companion) | Replaced | Chapter 8's DevSignIn. |

## Notes for the writers

- Output caching applies to anonymous requests only by default: a signed-in request or a response that sets a cookie is not cached. The Age header marks a response served from the cache (Age: 0 here).
- The compressed catalog arrives as `Content-Encoding: br` or gzip, depending on Accept-Encoding; /api/status with the same header is not compressed, because only the /api/catalog branch runs UseResponseCompression. Tokens (/api/csrf), orders and profiles are never compressed.
- The resilience handler retries transient results only: two 503s were retried (four attempts in all, then 200), a 400 was not (one attempt, then 503 from the app).
- The circuit breaker opened when three of the last six attempts had failed (FailureRatio 0.5, MinimumThroughput 5); after that, the app answered 503 without calling the service (the attempt count stayed at 1). The options must keep SamplingDuration at least twice AttemptTimeout, or validation fails at startup.
- The rate limiter returned 429 with Retry-After: 9 on the sixth anonymous search within ten seconds; casey, signed in on the same machine, had her own partition and got 200.
- The stock route catches HttpRequestException, TimeoutRejectedException and BrokenCircuitException and answers 503 with Problem Details; anything else stays a 500.
- Departure: the design extends the Development seed in Chapter 12; the warehouse stock lives in InventoryService, so the app's seed does not change.
- Departure: no Redis code path, because nothing here can verify it; the text can show the one-line swap (AddStackExchangeRedisCache) as a pointer, not a tested listing.
- Program.cs is 88 lines.
