# Chapter 4 delta: Routing and Endpoint Design

Generated October 30, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter04` and the operations that rebuild it from the Chapter 3 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 4 adds the storefront catalog and organizes registration by feature. Everything from Chapters 1 to 3 still answers on the same paths.

- The OpenAPI package (`Microsoft.AspNetCore.OpenApi` 11.0.0), `AddOpenApi()`, and `MapOpenApi()` in Development (`/openapi/v1.json`).
- `EndpointLoggingMiddleware`, placed right after `UseRouting()`, logs `Selected endpoint {Endpoint}` at Information level.
- Core: `Product(int Id, string Name, string Sku, decimal Price, string Category, int Version)`, `ProductCategories.All` (bikes, clothing, accessories, camping) and `IProductService` (GetByIdAsync, SearchAsync, ListByCategoryAsync).
- Infrastructure: `InMemoryProductService`, a singleton seeded with four products (1 Trail Helmet HLM-1001 29.99, 2 Trail Jersey JRS-2001 49.99, 3 Mountain-200 Bike BIK-3001 1299.00, 4 Two-Person Tent TNT-4001 189.00). This is the one product data path: Chapters 5 and 6 use the same store, and Chapter 10 swaps in EF Core behind the same interface.
- Web: `ProductCategoryConstraint` (reads Core's category list) and `CatalogEndpoints` with `AddCatalog` and `MapCatalogEndpoints`: the `/api/catalog` group with the list (and a Featured link to product 3), `/{id:int}` named GetCatalogItem (with a ReviewsLink), the literal `/search`, `/category/{category:productCategory}` and a nested `/{productId:int}/reviews` group named GetProductReviews.
- `DevelopmentEndpoints.MapDevelopmentEndpoints` holds Chapter 2's `/api/config` and `/api/email-check` and the route-template examples under `/examples`, all Development only.
- Step 8 composition: `StatusEndpoints.cs` (`/`, `/api/status`, `/health`) and `OrderEndpoints.cs` (`AddOrderServices` with Chapter 2's registrations unchanged, and the `/api/orders` group with GetOrderById). Program.cs keeps the pipeline and one line per feature: 51 lines.

## Files added or changed

| File | Change |
| --- | --- |
| `AdventureWorks/Program.cs` | changed |
| `AdventureWorks/EndpointLoggingMiddleware.cs` | added |
| `AdventureWorks/DevelopmentEndpoints.cs` | added |
| `AdventureWorks.Core/Products.cs` | added |
| `AdventureWorks.Infrastructure/InMemoryProductService.cs` | added |
| `AdventureWorks/ProductCategoryConstraint.cs` | added |
| `AdventureWorks/CatalogEndpoints.cs` | added |
| `AdventureWorks/StatusEndpoints.cs` | added |
| `AdventureWorks/OrderEndpoints.cs` | added |
| (command) `dotnet add package Microsoft.AspNetCore.OpenApi@11.0.0` | creates or edits project files |

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 24 of 24 files identical and no differences.

### Step 1: Configure endpoint selection

Add the OpenAPI package from the web project folder, register the generator, and log the selected endpoint in a small middleware placed directly after `app.UseRouting();`, where GetEndpoint() already returns the match.

Run this from the `AdventureWorks` folder.

```shell
dotnet add package Microsoft.AspNetCore.OpenApi@11.0.0
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.AspNetCore.OpenApi' into project 'AdventureWorks\AdventureWorks.csproj'.
info : Restoring packages for AdventureWorks\AdventureWorks.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.05.42.04/vulnerability.update.json
info : Package 'Microsoft.AspNetCore.OpenApi' is compatible with all the specified frameworks in project 'AdventureWorks\AdventureWorks.csproj'.
info : PackageReference for package 'Microsoft.AspNetCore.OpenApi' version '11.0.0' added to file 'AdventureWorks\AdventureWorks.csproj'.
info : Generating MSBuild file AdventureWorks\obj\AdventureWorks.csproj.nuget.g.props.
(3 more lines)
```

In `AdventureWorks/Program.cs`, add these lines after the line `builder.Services.AddTransient<DomainExceptionMiddleware>();`.

```csharp
builder.Services.AddOpenApi();
```

Create `AdventureWorks/EndpointLoggingMiddleware.cs` (13 lines).

```csharp
namespace AdventureWorks;

public sealed class EndpointLoggingMiddleware(
    RequestDelegate next,
    ILogger<EndpointLoggingMiddleware> logger)
{
    public Task InvokeAsync(HttpContext context)
    {
        logger.LogInformation("Selected endpoint {Endpoint}",
            context.GetEndpoint()?.DisplayName);
        return next(context);
    }
}
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.UseRouting();`. Before the UseWhen branch from Chapter 3.

```csharp
app.UseMiddleware<EndpointLoggingMiddleware>();
```

### Steps 2 and 3: Route-template examples (Development only)

The optional, default, catch-all, unconstrained and stacked-constraint templates are demonstrations, so they live in one Development-only file with Chapter 2's two diagnostics, under the /examples prefix. Replace Chapter 2's Development block in Program.cs with the one below, which also serves the OpenAPI document.

Create `AdventureWorks/DevelopmentEndpoints.cs` (38 lines).

```csharp
using AdventureWorks.Core;
using Microsoft.Extensions.Options;

namespace AdventureWorks;

public static class DevelopmentEndpoints
{
    public static IEndpointRouteBuilder MapDevelopmentEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/config",
            (IOptions<FeatureFlags> flags) =>
                Results.Ok(flags.Value));
        app.MapGet("/api/email-check", (string address,
            IEmailValidator validator) => Results.Ok(
                new { IsValid = validator.IsValid(address) }));
        MapRouteExamples(
            app.MapGroup("/examples").WithTags("Examples"));
        return app;
    }

    static void MapRouteExamples(RouteGroupBuilder group)
    {
        group.MapGet("/orders/{pageNumber:int?}",
            (int? pageNumber) => Results.Ok(
                new { Page = pageNumber ?? 1 }));
        group.MapGet("/status/{status:alpha=active}",
            (string status) => Results.Ok(
                new { Status = status }));
        group.MapGet("/files/{**path}",
            (string? path) => Results.Ok(new { Path = path }));
        group.MapGet("/unconstrained/{id}",
            (int id) => Results.Ok(new { Id = id }));
        group.MapGet("/reviews/{id:int:min(1)}/{reviewId:guid}",
            (int id, Guid reviewId) => Results.Ok(
                new { Id = id, ReviewId = reviewId }));
    }
}
```

In `AdventureWorks/Program.cs`, replace these lines.

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/config", (IOptions<FeatureFlags> flags) =>
        Results.Ok(flags.Value));
    app.MapGet("/api/email-check",
        (string address, IEmailValidator validator) => Results.Ok(
            new { IsValid = validator.IsValid(address) }));
}
```

With these:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapDevelopmentEndpoints();
}
```

### Step 5: Define the catalog and a stateless custom constraint

The catalog's data type, its category list and the service contract are framework-free, so they go in Core. The in-memory store goes in Infrastructure; Chapters 5, 6 and later use the same singleton, and Chapter 10 replaces it with an EF Core implementation of the same interface. The route constraint depends on ASP.NET Core routing, so it stays in Web and reads Core's category list.

Create `AdventureWorks.Core/Products.cs` (21 lines).

```csharp
namespace AdventureWorks.Core;

public sealed record Product(int Id, string Name, string Sku,
    decimal Price, string Category, int Version);

public static class ProductCategories
{
    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bikes", "clothing", "accessories", "camping"
        };
}

public interface IProductService
{
    Task<Product?> GetByIdAsync(int id);
    Task<Product[]> SearchAsync(
        string? query, int page, int pageSize);
    Task<Product[]> ListByCategoryAsync(string category);
}
```

Create `AdventureWorks.Infrastructure/InMemoryProductService.cs` (49 lines).

InMemoryProductService.cs, part 1 of 2 (21 lines):

```csharp
using System.Collections.Concurrent;
using AdventureWorks.Core;

namespace AdventureWorks.Infrastructure;

public sealed class InMemoryProductService : IProductService
{
    private readonly ConcurrentDictionary<int, Product> _products
        = new();
    private int _lastId;

    public InMemoryProductService()
    {
        Add("Trail Helmet", "HLM-1001", 29.99m, "accessories");
        Add("Trail Jersey", "JRS-2001", 49.99m, "clothing");
        Add("Mountain-200 Bike", "BIK-3001", 1299.00m, "bikes");
        Add("Two-Person Tent", "TNT-4001", 189.00m, "camping");
    }

    public Task<Product?> GetByIdAsync(int id) =>
        Task.FromResult(_products.GetValueOrDefault(id));
```

InMemoryProductService.cs, part 2 of 2 (27 lines):

```csharp
    public Task<Product[]> SearchAsync(
        string? query, int page, int pageSize) =>
        Task.FromResult(_products.Values
            .Where(p => string.IsNullOrWhiteSpace(query) ||
                p.Name.Contains(query,
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToArray());

    public Task<Product[]> ListByCategoryAsync(string category) =>
        Task.FromResult(_products.Values
            .Where(p => p.Category.Equals(category,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Id)
            .ToArray());

    private Product Add(string name, string sku,
        decimal price, string category)
    {
        var id = Interlocked.Increment(ref _lastId);
        var product = new Product(
            id, name, sku, price, category, Version: 1);
        _products[id] = product;
        return product;
    }
}
```

Create `AdventureWorks/ProductCategoryConstraint.cs` (13 lines).

```csharp
using AdventureWorks.Core;

namespace AdventureWorks;

public sealed class ProductCategoryConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route,
        string routeKey, RouteValueDictionary values,
        RouteDirection routeDirection) =>
        values.TryGetValue(routeKey, out var value) &&
        value is string category &&
        ProductCategories.All.Contains(category);
}
```

### Step 6: Group the catalog routes and name the link targets

One file holds the catalog feature: AddCatalog registers the store and the constraint, and MapCatalogEndpoints maps the /api/catalog group. The handlers are named static methods, so the route table reads as a list.

Create `AdventureWorks/CatalogEndpoints.cs` (74 lines).

CatalogEndpoints.cs, part 1 of 2 (38 lines):

```csharp
using AdventureWorks.Core;
using AdventureWorks.Infrastructure;

namespace AdventureWorks;

public static class CatalogEndpoints
{
    public static IServiceCollection AddCatalog(
        this IServiceCollection services)
    {
        services.AddSingleton<IProductService,
            InMemoryProductService>();
        services.Configure<RouteOptions>(options =>
            options.ConstraintMap.Add("productCategory",
                typeof(ProductCategoryConstraint)));
        return services;
    }

    public static IEndpointRouteBuilder MapCatalogEndpoints(
        this IEndpointRouteBuilder app)
    {
        var catalog = app.MapGroup("/api/catalog")
            .WithTags("Catalog");
        catalog.MapGet("/", ListCatalog);
        catalog.MapGet("/{id:int}", GetCatalogItem)
            .WithName("GetCatalogItem");
        catalog.MapGet("/search", SearchCatalog);
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

CatalogEndpoints.cs, part 2 of 2 (35 lines):

```csharp
    static async Task<IResult> ListCatalog(
        IProductService products, LinkGenerator links,
        HttpContext context) => Results.Ok(new
        {
            Items = await products.SearchAsync(null, 1, 20),
            Featured = links.GetPathByName(
                context, "GetCatalogItem", new { id = 3 })
        });

    static async Task<IResult> GetCatalogItem(int id,
        IProductService products, LinkGenerator links,
        HttpContext context)
    {
        if (id <= 0)
            return Results.BadRequest(
                new { Error = "ID must be positive." });
        var product = await products.GetByIdAsync(id);
        if (product is null) return Results.NotFound();
        return Results.Ok(new
        {
            product.Id, product.Name, product.Sku,
            product.Price, product.Category,
            ReviewsLink = links.GetPathByName(context,
                "GetProductReviews", new { productId = id })
        });
    }

    static async Task<IResult> SearchCatalog(
        string? q, IProductService products) =>
        Results.Ok(await products.SearchAsync(q, 1, 20));

    static async Task<IResult> ListCategory(
        string category, IProductService products) =>
        Results.Ok(await products.ListByCategoryAsync(category));
}
```

In `AdventureWorks/Program.cs`, add these lines above the line `builder.Services.AddHealthChecks();`.

```csharp
builder.Services.AddCatalog();
```

In `AdventureWorks/Program.cs`, add these lines above the line `if (app.Environment.IsDevelopment())`. After the order route, before the Development block.

```csharp
app.MapCatalogEndpoints();
```

### Step 8: Compose feature registration

Move the Chapter 1 and 2 routes into feature files the same way, so Program.cs keeps the pipeline and one line per feature. Nothing about the routes changes: the same paths answer, and the order route is now a named endpoint in an /api/orders group.

Create `AdventureWorks/StatusEndpoints.cs` (17 lines).

```csharp
namespace AdventureWorks;

public static class StatusEndpoints
{
    public static IEndpointRouteBuilder MapStatusEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health");
        app.MapGet("/", () => Results.Redirect("/api/status"));
        app.MapGet("/api/status", () => Results.Ok(new
        {
            Status = "Operational",
            Timestamp = DateTime.UtcNow
        }));
        return app;
    }
}
```

Create `AdventureWorks/OrderEndpoints.cs` (47 lines).

OrderEndpoints.cs, part 1 of 2 (22 lines):

```csharp
using AdventureWorks.Core;
using AdventureWorks.Infrastructure;
using Microsoft.Extensions.Options;

namespace AdventureWorks;

public static class OrderEndpoints
{
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
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddTransient<IEmailValidator, EmailValidator>();
        return services;
    }
```

OrderEndpoints.cs, part 2 of 2 (24 lines):

```csharp
    public static IEndpointRouteBuilder MapOrderEndpoints(
        this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/api/orders")
            .WithTags("Orders");
        orders.MapGet("/{id:int}", GetOrder)
            .WithName("GetOrderById");
        return app;
    }

    static IResult GetOrder(int id, IOrderRepository orders,
        IOptions<FeatureFlags> flags)
    {
        if (!flags.Value.EnableNewCheckout)
            return Results.StatusCode(
                StatusCodes.Status503ServiceUnavailable);
        if (id <= 0)
            throw new DomainValidationException(
                "Order ID must be positive.");
        return Results.Ok(orders.GetById(id) ??
            throw new ResourceNotFoundException(
                $"Order {id} was not found."));
    }
}
```

In `AdventureWorks/Program.cs`, replace these lines. The Chapter 2 registrations move into AddOrderServices unchanged.

```csharp
builder.Services.AddOptions<FeatureFlags>()
    .Bind(builder.Configuration.GetSection("FeatureFlags"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<ICacheService,
    MemoryCacheService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddTransient<IEmailValidator, EmailValidator>();
```

With these:

```csharp
builder.Services.AddOrderServices(builder.Configuration);
```

In `AdventureWorks/Program.cs`, replace these lines.

```csharp
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/api/status"));
app.MapGet("/api/status", () => Results.Ok(new
{
    Status = "Operational",
    Timestamp = DateTime.UtcNow
}));
app.MapGet("/api/orders/{id:int}", (int id,
    IOrderRepository orders, IOptions<FeatureFlags> flags) =>
{
    if (!flags.Value.EnableNewCheckout)
        return Results.StatusCode(
            StatusCodes.Status503ServiceUnavailable);
    if (id <= 0)
        throw new DomainValidationException(
            "Order ID must be positive.");
    return Results.Ok(orders.GetById(id) ??
        throw new ResourceNotFoundException(
            $"Order {id} was not found."));
});
```

With these:

```csharp
app.MapStatusEndpoints();
app.MapOrderEndpoints();
```

In `AdventureWorks/Program.cs`, delete these lines. Program.cs no longer names Core or Infrastructure types.

```csharp
using AdventureWorks.Core;
using AdventureWorks.Infrastructure;
using Microsoft.Extensions.Options;
```

## Program.cs at the checkpoint (51 lines)

Program.cs, part 1 of 2 (18 lines):

```csharp
using AdventureWorks;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});
builder.Services.AddOrderServices(builder.Configuration);
builder.Services.AddCatalog();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["correlationId"] =
            context.HttpContext.Items["CorrelationId"]);
builder.Services.AddTransient<CorrelationIdMiddleware>();
builder.Services.AddTransient<DomainExceptionMiddleware>();
builder.Services.AddOpenApi();
```

Program.cs, part 2 of 2 (32 lines):

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
app.UseRouting();
app.UseMiddleware<EndpointLoggingMiddleware>();
app.UseWhen(context =>
    context.Request.Path.StartsWithSegments("/api"),
    api => api.Use(async (context, next) =>
    {
        context.Response.Headers["X-API-Pipeline"] = "true";
        await next();
    }));
app.MapStatusEndpoints();
app.MapOrderEndpoints();
app.MapCatalogEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapDevelopmentEndpoints();
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
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: <working folder>\AdventureWorks
```

In Visual Studio 2026, open `AdventureWorks.slnx` and press F5; the https profile starts the same app on https://localhost:7001. A headless Visual Studio 2026 rebuild of this checkpoint (devenv /rebuild Debug) rebuilt 3 project(s) with 0 warning(s) and 0 error(s).

## Requests and real responses

`requests.http` holds these requests in this order; 24 of 24 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `GET /api/catalog` returned **200**

  ```text
  {"items":[{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":...
  ```

- `GET /api/catalog/1` returned **200**

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","reviewsLink":"/api/catalog/1/reviews"}
  ```

- `GET /api/catalog/1/reviews` returned **200**

  ```text
  {"productId":1,"reviews":[]}
  ```

- `GET /api/catalog/0` returned **400**

  ```text
  {"error":"ID must be positive."}
  ```

- `GET /api/catalog/99` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-52fe5e5349258bedb336a2cc21f96d43-a0b2dde0a0432427-02","correlationId":"009566a541f24650a8766c6db1828b77"}
  ```

- `GET /api/catalog/abc` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-d48f365e78d88ec37650bb99757af6da-8f1cb2c7bc517e1a-02","correlationId":"e8b868c850564a55b5056f9718c6a111"}
  ```

- `GET /api/catalog/search?q=trail` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1}]
  ```

- `GET /api/catalog/category/bikes` returned **200**

  ```text
  [{"id":3,"name":"Mountain-200 Bike","sku":"BIK-3001","price":1299.00,"category":"bikes","version":1}]
  ```

- `GET /api/catalog/category/BIKES` returned **200**

  ```text
  [{"id":3,"name":"Mountain-200 Bike","sku":"BIK-3001","price":1299.00,"category":"bikes","version":1}]
  ```

- `GET /api/catalog/category/toys` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-0cfb15c52c1a808d91c86f8f89c3d9b3-7203af57d500a9db-02","correlationId":"4ae4a9d03ade4e6ebe392bc0ce4ec91c"}
  ```

- `GET /examples/orders` returned **200**

  ```text
  {"page":1}
  ```

- `GET /examples/orders/3` returned **200**

  ```text
  {"page":3}
  ```

- `GET /examples/status` returned **200**

  ```text
  {"status":"active"}
  ```

- `GET /examples/status/123` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-6b7198accea449a53e78dfdb185f919e-c5873fbe40e832f9-02","correlationId":"14a8f1b203b04c4b8f51cab8a2ead864"}
  ```

- `GET /examples/files/manuals/tent.pdf` returned **200**

  ```text
  {"path":"manuals/tent.pdf"}
  ```

- `GET /examples/unconstrained/abc` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Microsoft.AspNetCore.Http.BadHttpRequestException","status":400,"detail":"Failed to bind parameter \"int id\" from \"abc\".","exception":{"details":"M...
  ```

- `GET /examples/reviews/0/3f2504e0-4f89-11d3-9a0c-0305e82c3301` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-8e1823145b845bd67f09d5c1ba686a25-3f4feb17f927e124-02","correlationId":"b1097677dd6f4fd2851387a1c3bda32e"}
  ```

- `GET /examples/reviews/1/3f2504e0-4f89-11d3-9a0c-0305e82c3301` returned **200**

  ```text
  {"id":1,"reviewId":"3f2504e0-4f89-11d3-9a0c-0305e82c3301"}
  ```

- `GET /openapi/v1.json` returned **200**

  ```text
  (the OpenAPI JSON document)
  ```

- `GET /ping` returned **200**

  ```text
  pong
  ```

- `GET /api/orders/1` returned **200**

  ```text
  {"id":1,"orderNumber":"AW-0001","total":49.00}
  ```

- `GET /api/orders/0` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Invalid request","status":400,"detail":"Order ID must be positive.","traceId":"00-3ebfd9cb465cbb9696e2e88a84d47b0e-586d8032e648bdc5-02","correlationId...
  ```

- `GET /health` returned **200**

  ```text
  Healthy
  ```

- `GET /api/config` returned **200**

  ```text
  {"enableNewCheckout":true,"maxCartItems":30,"allowedRegions":["US"]}
  ```

### Other runs

- Production: `/openapi/v1.json` and `/examples/orders` return 404; `/api/catalog` returns 200. Checked.
- The console shows lines such as `Selected endpoint HTTP: GET /api/catalog/{id:int} => GetCatalogItem`, because the handlers are named methods. Checked.

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks/CatalogEndpoints.cs` on line 57:

```csharp
if (product is null) return Results.NotFound();
```

Press F5 (the https profile), then send GET https://localhost:7001/api/catalog/1. When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
id = 1 [Int32]
product = Product { Id = 1, Name = Trail Helmet, Sku = HLM-1001, Price = 29.99, Category = accessories, Version = 1 } [Product]
context.GetEndpoint()?.DisplayName = HTTP: GET /api/catalog/{id:int} => GetCatalogItem [String]
```

The handler runs only after routing selected GetCatalogItem, so id is already an int and the endpoint is known. product is the record the in-memory store returned.

## What the Activity adds

`SkuFormatConstraint` (exactly three ASCII uppercase letters, a hyphen and four ASCII digits, checked ordinally, so lowercase fails) registered as `skuFormat`; `GET /inventory/{sku:skuFormat}` echoes the SKU; and an `/api/products` group tagged Product Management (ActivityEndpoints.cs) with uniquely named GET endpoints (ListManagedProducts, GetManagedProduct) over the same store, a mock POST that returns 201 with a Location for product 3, and `GET /{id:int}/related`, which generates two relative links with GetPathByName and returns a Problem if any link is null. Chapter 5 uses `/api/v1/products` and `/api/v2/products`, so this group stays in the Activity copy. The SKU format is the one Chapter 5 validates.

The reader-path check rebuilt `AdventureWorks/Chapter04-Activity` from the Chapter 4 checkpoint: no differences.

### Activity: a SKU constraint and a product-management group

Work in a copy of the Chapter 4 checkpoint folder. Chapter 5 starts from the checkpoint.

Create `AdventureWorks/SkuFormatConstraint.cs` (17 lines).

```csharp
namespace AdventureWorks;

public sealed class SkuFormatConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route,
        string routeKey, RouteValueDictionary values,
        RouteDirection routeDirection) =>
        values.TryGetValue(routeKey, out var value) &&
        value is string sku && IsSku(sku);

    // Three ASCII uppercase letters, a hyphen, four ASCII digits.
    static bool IsSku(string sku) =>
        sku.Length == 8 &&
        sku.Take(3).All(char.IsAsciiLetterUpper) &&
        sku[3] == '-' &&
        sku.Skip(4).All(char.IsAsciiDigit);
}
```

Create `AdventureWorks/ActivityEndpoints.cs` (44 lines).

ActivityEndpoints.cs, part 1 of 2 (22 lines):

```csharp
using AdventureWorks.Core;

namespace AdventureWorks;

public static class ActivityEndpoints
{
    public static IEndpointRouteBuilder MapActivityEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/inventory/{sku:skuFormat}",
            (string sku) => Results.Ok(new { Sku = sku }));
        var products = app.MapGroup("/api/products")
            .WithTags("Product Management");
        products.MapGet("/", ListIds)
            .WithName("ListManagedProducts");
        products.MapGet("/{id:int}", GetById)
            .WithName("GetManagedProduct");
        products.MapPost("/", () => Results.CreatedAtRoute(
            "GetManagedProduct", new { id = 3 }, new { Id = 3 }));
        products.MapGet("/{id:int}/related", Related);
        return app;
    }
```

ActivityEndpoints.cs, part 2 of 2 (21 lines):

```csharp
    static async Task<IResult> ListIds(IProductService catalog) =>
        Results.Ok((await catalog.SearchAsync(null, 1, 20))
            .Select(product => product.Id));

    static async Task<IResult> GetById(
        int id, IProductService catalog) =>
        await catalog.GetByIdAsync(id) is { } product
            ? Results.Ok(product) : Results.NotFound();

    static IResult Related(
        int id, LinkGenerator links, HttpContext context)
    {
        var paths = new[] { id + 1, id + 2 }
            .Select(related => links.GetPathByName(context,
                "GetManagedProduct", new { id = related }))
            .ToArray();
        return paths.Any(path => path is null)
            ? Results.Problem("Link generation failed.")
            : Results.Ok(paths);
    }
}
```

In `AdventureWorks/Program.cs`, add these lines after the line `builder.Services.AddCatalog();`.

```csharp
builder.Services.Configure<RouteOptions>(options =>
    options.ConstraintMap.Add("skuFormat",
        typeof(SkuFormatConstraint)));
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.MapCatalogEndpoints();`.

```csharp
app.MapActivityEndpoints();
```

### Activity requests and responses

32 of 32 requests in `Chapter04-Activity/requests.http` returned their commented status. The responses for the requests the Activity adds:

- `GET /inventory/ABC-1234` returned **200**

  ```text
  {"sku":"ABC-1234"}
  ```

- `GET /inventory/abc-1234` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-3ea18c3a863dd3eed0d8788cf581d180-b53781b9aa889958-02","correlationId":"6aaace34cd684c5ea2c2c4adeb2cf974"}
  ```

- `GET /inventory/invalid` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-22687589ecaf172139a8df0adf662914-50e8a945fcc894c0-02","correlationId":"5a449dc0a1164020b2911606f6fe0472"}
  ```

- `GET /api/products` returned **200**

  ```text
  [1,2,3,4]
  ```

- `GET /api/products/1` returned **200**

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1}
  ```

- `POST /api/products` returned **201** (Location: https://localhost:7001/api/products/3)

  ```text
  {"id":3}
  ```

- `GET /api/products/1/related` returned **200**

  ```text
  ["/api/products/2","/api/products/3"]
  ```

- `GET /api/catalog/search?q=tent` returned **200**

  ```text
  [{"id":4,"name":"Two-Person Tent","sku":"TNT-4001","price":189.00,"category":"camping","version":1}]
  ```

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| Step 1 `dotnet add package ... --version 11.0.0` | Replaced | `dotnet add package Microsoft.AspNetCore.OpenApi@11.0.0`, run in the AdventureWorks folder. |
| Step 2 and Step 3 /examples excerpts | Replaced | The same five templates, now group-relative inside DevelopmentEndpoints.MapRouteExamples. |
| Step 5 ProductCategoryConstraint.cs | Replaced | Store categories from Core's ProductCategories.All; `using Microsoft.AspNetCore.Routing;` dropped (an implicit using in the Web SDK). |
| Step 5 Configure&lt;RouteOptions&gt; excerpt | Replaced | Now inside AddCatalog. |
| Step 6 ProductEndpoints.cs | Replaced | CatalogEndpoints.cs (74 lines, printed in two parts): `/api/catalog` instead of `/api/v1/products`, data from IProductService, named static handlers. |
| Step 8 text | Replaced | Becomes the composition step that creates StatusEndpoints.cs and OrderEndpoints.cs (47 lines, two parts). |
| Full Example repeats of the constraint and endpoint files | Removed | Printed once. |
| Full Example Program.cs | Replaced | 51 lines; the delta lists every line added, moved or deleted. |
| Full Example run text | Replaced | Request `/api/catalog`, `/api/catalog/1`, `/api/catalog/search?q=trail`, `/api/catalog/category/bikes`, `/openapi/v1.json`; follow ReviewsLink. |

## Notes for the writers

- Line 152 ("This Program.cs replaces Chapter 3's; Chapter 3's middleware file can stay but is not registered") is wrong now: Chapter 3's middleware stays registered and in order.
- Step 8's "while the reference application is small" goes; the step now shows the composition every later chapter follows (an AddX and a MapX per feature file).
- The endpoint-logging lesson moved from an inline `app.Use` to a five-line middleware class with the same behavior, placed directly after `app.UseRouting();`, so Program.cs stays short. The lesson (GetEndpoint returns the match only after routing) is unchanged.
- The catalog handlers are named static methods. Their names (ListCatalog, GetCatalogItem, SearchCatalog, ListCategory) are unique in the app; later chapters should keep handler method names unique as well.
- `/api/catalog/0` returns 400 `{"error":"ID must be positive."}`; `/api/catalog/99` returns 404 with a Problem Details body from Chapter 3's status-code pages.
- Quiz question 6 (the literal /search route) still applies to the catalog group.
- The Program.cs `using` lines for Core, Infrastructure and Options are deleted in Step 8 because Program.cs no longer names those types; the feature files import them.
