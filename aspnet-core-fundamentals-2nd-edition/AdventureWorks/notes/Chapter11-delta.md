# Chapter 11 delta: Application Services and Architectural Patterns

Generated November 3, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter11` and the operations that rebuild it from the Chapter 10 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 11 puts order placement and cancellation behind an application service in the same app, on Chapter 10's model. No second copy of the data model and no second project.

- Core `OrderContracts.cs`: OrderStatus, `CreateOrderRequest` (1 to 20 `OrderLineRequest` lines of ProductId and Quantity 1 to 20; no owner, no price), NewOrder, OrderOutcome, OrderResult, `IOrderService` (CreateAsync, CancelAsync), the narrow `IOrderWriter` (stages only) and `IUnitOfWork` (one save), and three Core exceptions for the expected database failures (a duplicate number, a changed order, an order deleted since it was read). IProductService gains GetByIdsAsync; IOrderRepository gains GetByNumberAsync.
- Core `OrderService.cs`: the rules: one line per product, every product sold, prices from the catalog, the next order number, ownership and the version check on cancel. It depends only on Core interfaces, so Chapter 15 can unit-test it.
- Infrastructure: `EfOrderWriter` (stages a new order or a status change in the request's DbContext), `EfUnitOfWork` (one SaveChangesAsync; translates DbUpdateConcurrencyException into OrderDeletedException when the row is gone and OrderChangedException when it exists with another version, and the unique-index violation on Orders.OrderNumber into OrderNumberConflictException, and lets anything else through), and the two new query methods. Chapter 10's self-saving notes update (PUT) already tells a deleted order from a changed one (Chapter 10, review 11-P10), so this chapter does not change it.
- Web: `LoggingOrderService`, a decorator registered by hand with a factory; AddOrderServices registers the writer, the unit of work, the service and the decorator, all scoped; `POST /api/orders` (201 with a Location) and `POST /api/orders/{id}/cancel` (Chapter 9's owner rule, then the service); Program.cs turns on ValidateScopes and ValidateOnBuild in every environment: 80 lines.

## Files added or changed

| File | Change |
| --- | --- |
| `AdventureWorks.Core/OrderContracts.cs` | added |
| `AdventureWorks.Core/Products.cs` | changed |
| `AdventureWorks.Core/Orders.cs` | changed |
| `AdventureWorks.Core/OrderService.cs` | added |
| `AdventureWorks.Infrastructure/Data/EfOrderWriter.cs` | added |
| `AdventureWorks.Infrastructure/Data/EfUnitOfWork.cs` | added |
| `AdventureWorks.Infrastructure/Data/EfProductService.cs` | changed |
| `AdventureWorks.Infrastructure/Data/EfOrderRepository.cs` | changed |
| `AdventureWorks/LoggingOrderService.cs` | added |
| `AdventureWorks/OrderEndpoints.cs` | changed |
| `AdventureWorks/Program.cs` | changed |

The reader types 371 lines of code: 259 in 5 new files and 112 new or changed in existing files, plus 0 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 78 of 78 files identical and no differences.

### Step 1: The contracts the service needs

An application service owns a use case: here, placing and cancelling an order. Its inputs are a CreateOrderRequest that holds only products and quantities (the owner is the signed-in user, and prices come from the catalog) and the owner's ID; its output is an OrderResult with an outcome the endpoint turns into a status code. Two narrow contracts serve it: IOrderWriter stages changes and IUnitOfWork saves them, once, for the whole request. The two exceptions are the expected database failures, in Core terms, so the service never sees EF Core.

Create `AdventureWorks.Core/OrderContracts.cs` (68 lines).

OrderContracts.cs, part 1 of 2 (35 lines):

```csharp
using System.ComponentModel.DataAnnotations;

namespace AdventureWorks.Core;

public static class OrderStatus
{
    public const string Pending = "Pending";
    public const string Cancelled = "Cancelled";
}

// What a caller may send: products and quantities. The owner is
// the signed-in user, and prices come from the catalog.
public sealed class OrderLineRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 20)]
    public int Quantity { get; set; }
}

public sealed class CreateOrderRequest
{
    [Required, MinLength(1), MaxLength(20)]
    public List<OrderLineRequest> Items { get; set; } = [];
}

public sealed record NewOrder(string OwnerId, string OrderNumber,
    IReadOnlyList<OrderItem> Items);

public enum OrderOutcome { Created, Cancelled, NotFound, Invalid,
    Conflict }

public sealed record OrderResult(OrderOutcome Outcome,
    Order? Order = null, string? Error = null);
```

OrderContracts.cs, part 2 of 2 (32 lines):

```csharp
public interface IOrderService
{
    Task<OrderResult> CreateAsync(string ownerId,
        CreateOrderRequest request,
        CancellationToken ct = default);
    Task<OrderResult> CancelAsync(string ownerId, int id,
        CancellationToken ct = default);
}

// Writes that wait for the unit of work: nothing here saves.
public interface IOrderWriter
{
    Task<string> NextOrderNumberAsync(CancellationToken ct);
    void Add(NewOrder order);
    Task<bool> SetStatusAsync(int id, string status,
        Guid version, CancellationToken ct);
}

// One save for everything the request staged.
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}

public sealed class OrderNumberConflictException(Exception inner)
    : Exception("Another order took that number.", inner);

public sealed class OrderChangedException(Exception inner)
    : Exception("The order changed after it was read.", inner);

public sealed class OrderDeletedException(Exception inner)
    : Exception("The order was deleted after the read.", inner);
```

The service reads the catalog through Chapter 4's IProductService, which gets a method that loads several products in one query, and finds a new order by its number after the save.

In `AdventureWorks.Core/Products.cs`, replace these lines. In IProductService.

```csharp
    Task<Product?> GetByIdAsync(int id);
```

With these:

```csharp
    Task<Product?> GetByIdAsync(int id);
    Task<Product[]> GetByIdsAsync(int[] ids,
        CancellationToken cancellationToken = default);
```

In `AdventureWorks.Core/Orders.cs`, add these lines after the line `Task<Order?> GetByIdAsync(int id);`. In IOrderRepository.

```csharp
    Task<Order?> GetByNumberAsync(string orderNumber);
```

### Step 2: The service

OrderService holds the rules: one line per product, every product sold, server-side prices, the order number, ownership on cancel and the version check. It depends only on Core interfaces, so it lives in Core and Chapter 15 can test it without HTTP or a database. Validation of the request's shape (1 to 20 lines, quantities 1 to 20) stays on the request type, where Chapter 7's AddValidation checks it before the endpoint runs.

Create `AdventureWorks.Core/OrderService.cs` (79 lines).

OrderService.cs, part 1 of 2 (38 lines):

```csharp
namespace AdventureWorks.Core;

// The order rules in one place, over Core contracts only, so the
// endpoints stay small and the rules can be tested without HTTP.
public sealed class OrderService(IOrderRepository orders,
    IOrderWriter writer, IProductService catalog,
    IUnitOfWork unitOfWork) : IOrderService
{
    public async Task<OrderResult> CreateAsync(string ownerId,
        CreateOrderRequest request,
        CancellationToken ct = default)
    {
        int[] ids = [.. request.Items.Select(i => i.ProductId)
            .Distinct()];
        if (ids.Length != request.Items.Count)
            return Invalid("Use one line per product.");
        var products = (await catalog.GetByIdsAsync(ids, ct))
            .ToDictionary(p => p.Id);
        if (products.Count != ids.Length)
            return Invalid("A product on the order is not sold.");
        // The catalog sets each price, whatever the caller sent.
        OrderItem[] items = [.. request.Items.Select(line =>
            new OrderItem(line.ProductId, line.Quantity,
                products[line.ProductId].Price))];
        var number = await writer.NextOrderNumberAsync(ct);
        writer.Add(new NewOrder(ownerId, number, items));
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (OrderNumberConflictException)
        {
            return new(OrderOutcome.Conflict, Error:
                "Another order took that number; send it again.");
        }
        return Saved(OrderOutcome.Created,
            await orders.GetByNumberAsync(number));
    }
```

OrderService.cs, part 2 of 2 (40 lines):

```csharp
    public async Task<OrderResult> CancelAsync(string ownerId,
        int id, CancellationToken ct = default)
    {
        var order = await orders.GetByIdAsync(id);
        if (order is null || order.OwnerId != ownerId)
            return new(OrderOutcome.NotFound);
        if (order.Status == OrderStatus.Cancelled)
            return Invalid("The order is already cancelled.");
        // Staged with the version just read; the save checks it.
        // False: the order was deleted after the read above.
        if (!await writer.SetStatusAsync(
                id, OrderStatus.Cancelled, order.Version, ct))
            return new(OrderOutcome.NotFound);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (OrderDeletedException)
        {
            return new(OrderOutcome.NotFound);
        }
        catch (OrderChangedException)
        {
            return new(OrderOutcome.Conflict, Error:
                "The order changed; read it again.");
        }
        return Saved(OrderOutcome.Cancelled,
            await orders.GetByIdAsync(id));
    }

    static OrderResult Invalid(string error) =>
        new(OrderOutcome.Invalid, Error: error);

    // The order read back after a save, for the response. Null:
    // it was deleted since, so the caller gets NotFound.
    static OrderResult Saved(OrderOutcome outcome,
        Order? order) =>
        order is null
            ? new(OrderOutcome.NotFound) : new(outcome, order);
}
```

### Step 3: The EF Core side

EfOrderWriter only stages: Add puts a new order in the change tracker, and SetStatusAsync loads a tracked order and sets its status with the version the service read. EfUnitOfWork calls SaveChangesAsync once and translates the failures the service expects: a concurrency failure, which is OrderDeletedException when the order no longer exists (deleted since the service read it) and OrderChangedException when it exists with another version, and a duplicate order number from the unique index, which happens when two requests pick the same next number at once. Any other database error stays an exception. All of them share the request's one DbContext, so one SaveChanges commits everything the request staged, in one transaction.

Create `AdventureWorks.Infrastructure/Data/EfOrderWriter.cs` (42 lines).

EfOrderWriter.cs, part 1 of 2 (18 lines):

```csharp
using AdventureWorks.Core;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.Infrastructure.Data;

// Stages order changes in the request's DbContext. EfUnitOfWork,
// which shares that context, saves them in one SaveChanges.
public sealed class EfOrderWriter(AdventureWorksDbContext db)
    : IOrderWriter
{
    // The next number after the newest order. Two requests can
    // pick the same one; the unique index refuses the second.
    public async Task<string> NextOrderNumberAsync(
        CancellationToken ct)
    {
        var last = await db.Orders.MaxAsync(o => (int?)o.Id, ct);
        return $"AW-{(last ?? 0) + 1:0000}";
    }
```

EfOrderWriter.cs, part 2 of 2 (23 lines):

```csharp
    public void Add(NewOrder order) => db.Orders.Add(new()
    {
        OwnerId = order.OwnerId,
        OrderNumber = order.OrderNumber,
        Items = [.. order.Items.Select(item => new OrderItemEntity
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPriceCents = Money.ToCents(item.UnitPrice)
        })]
    });

    public async Task<bool> SetStatusAsync(int id, string status,
        Guid version, CancellationToken ct)
    {
        var row = await db.Orders.FindAsync([id], ct);
        if (row is null) return false;
        db.Entry(row).Property(o => o.Version).OriginalValue =
            version;
        row.Status = status;
        return true;
    }
}
```

Create `AdventureWorks.Infrastructure/Data/EfUnitOfWork.cs` (38 lines).

```csharp
using AdventureWorks.Core;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.Infrastructure.Data;

// One SaveChanges for the request, translating the expected
// database failures into Core exceptions. Anything else is a real
// failure and stays an exception.
public sealed class EfUnitOfWork(AdventureWorksDbContext db)
    : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // No row left to compare with: deleted, not changed.
            foreach (var entry in ex.Entries)
            {
                var row = await entry.GetDatabaseValuesAsync(ct);
                if (row is null)
                    throw new OrderDeletedException(ex);
            }
            throw new OrderChangedException(ex);
        }
        catch (DbUpdateException ex)
            when (SqliteErrors.Is(ex, SqliteErrors.Unique) &&
                ex.InnerException!.Message.Contains(
                    "Orders.OrderNumber",
                    StringComparison.Ordinal))
        {
            throw new OrderNumberConflictException(ex);
        }
    }
}
```

In `AdventureWorks.Infrastructure/Data/EfProductService.cs`, add the following above `public async Task<Product?> GetBySkuAsync(string sku,`. One query: WHERE Id IN (...).

```csharp
    public async Task<Product[]> GetByIdsAsync(int[] ids,
        CancellationToken cancellationToken = default) =>
        Map(await db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToArrayAsync(cancellationToken));
```

In `AdventureWorks.Infrastructure/Data/EfOrderRepository.cs`, add the following below `public async Task<Order?> GetByIdAsync(int id)`.

```csharp
    public async Task<Order?> GetByNumberAsync(
        string orderNumber)
    {
        var row = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o =>
                o.OrderNumber == orderNumber);
        return row is null ? null : ToOrder(row);
    }
```

### Step 4: A logging decorator, registered by hand

A decorator implements the same interface and wraps the real service, so logging (or metrics, or caching) is added without touching the rules. The built-in container has no decorator method, and none is needed: register the concrete OrderService, then register IOrderService with a factory that wraps it. Everything is scoped, because everything below it uses the scoped DbContext.

Create `AdventureWorks/LoggingOrderService.cs` (32 lines).

```csharp
using System.Diagnostics;
using AdventureWorks.Core;

namespace AdventureWorks;

// A decorator: the same interface, wrapped around OrderService,
// adds a log line for each outcome without touching the rules.
public sealed class LoggingOrderService(IOrderService inner,
    ILogger<LoggingOrderService> logger) : IOrderService
{
    public Task<OrderResult> CreateAsync(string ownerId,
        CreateOrderRequest request,
        CancellationToken ct = default) =>
        LogAsync("Create", () =>
            inner.CreateAsync(ownerId, request, ct));

    public Task<OrderResult> CancelAsync(string ownerId, int id,
        CancellationToken ct = default) =>
        LogAsync("Cancel", () =>
            inner.CancelAsync(ownerId, id, ct));

    async Task<OrderResult> LogAsync(string operation,
        Func<Task<OrderResult>> call)
    {
        var timer = Stopwatch.StartNew();
        var result = await call();
        logger.LogInformation(
            "{Operation} order: {Outcome} in {ElapsedMs} ms",
            operation, result.Outcome, timer.ElapsedMilliseconds);
        return result;
    }
}
```

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
        // Repositories and the unit of work share the request's
        // DbContext, so all three are scoped like it.
        services.AddScoped<IOrderWriter, EfOrderWriter>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        // The service, wrapped by its logging decorator.
        services.AddScoped<OrderService>();
        services.AddScoped<IOrderService>(provider =>
            new LoggingOrderService(
                provider.GetRequiredService<OrderService>(),
                provider.GetRequiredService<
                    ILogger<LoggingOrderService>>()));
        return services;
    }
```

### Step 5: A small HTTP adapter

The endpoints do HTTP work only: check the antiforgery header, take the owner from the sign-in, call the service, and map its outcome to 201 with a Location, 200, 400, 403, 404 or 409. Cancel keeps Chapter 9's pattern: load the order, authorize OrderOwner, then act.

In `AdventureWorks/OrderEndpoints.cs`, replace the MapOrderEndpoints method with the following. Two new routes: POST /api/orders and POST /api/orders/{id}/cancel.

```csharp
    public static IEndpointRouteBuilder MapOrderEndpoints(
        this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/api/orders")
            .WithTags("Orders");
        orders.MapGet("/{id:int}", GetOrder)
            .WithName("GetOrderById");
        orders.MapPost("/", CreateOrder);
        orders.MapPost("/{id:int}/cancel", CancelOrder);
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

In `AdventureWorks/OrderEndpoints.cs`, add the following below `static async Task<IResult> GetOrder(int id,`.

```csharp
    // The endpoint adapts HTTP to the service: the owner comes
    // from the sign-in, and the outcome becomes a status code.
    static async Task<IResult> CreateOrder(
        CreateOrderRequest request, IOrderService service,
        ClaimsPrincipal user, HttpContext context,
        IAntiforgery antiforgery, CancellationToken ct)
    {
        if (!await HasValidTokenAsync(context, antiforgery))
            return Results.BadRequest();
        var owner = user.FindFirstValue(
            ClaimTypes.NameIdentifier)!;
        var result =
            await service.CreateAsync(owner, request, ct);
        return result.Outcome == OrderOutcome.Created
            ? Results.CreatedAtRoute("GetOrderById",
                new { id = result.Order!.Id }, result.Order)
            : ToProblem(result);
    }
```

In `AdventureWorks/OrderEndpoints.cs`, add the following below `static async Task<IResult> CreateOrder(`.

```csharp
    // Chapter 9's owner rule first, then the service.
    static async Task<IResult> CancelOrder(int id,
        IOrderRepository orders, IOrderService service,
        IAuthorizationService authorization,
        ClaimsPrincipal user, HttpContext context,
        IAntiforgery antiforgery, CancellationToken ct)
    {
        if (!await HasValidTokenAsync(context, antiforgery))
            return Results.BadRequest();
        var order = await orders.GetByIdAsync(id);
        if (order is null) return Results.NotFound();
        var owner = await authorization.AuthorizeAsync(
            user, order, StorePolicies.OrderOwner);
        if (!owner.Succeeded) return Results.Forbid();
        var result = await service.CancelAsync(
            order.OwnerId, id, ct);
        return result.Outcome == OrderOutcome.Cancelled
            ? Results.Ok(result.Order) : ToProblem(result);
    }
```

In `AdventureWorks/OrderEndpoints.cs`, add the following below `static async Task<IResult> CancelOrder(int id,`.

```csharp
    static IResult ToProblem(OrderResult result) =>
        result.Outcome switch
        {
            OrderOutcome.NotFound => Results.NotFound(),
            OrderOutcome.Conflict => Results.Problem(result.Error,
                statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(result.Error,
                statusCode: StatusCodes.Status400BadRequest)
        };
```

### Step 6: Validate lifetimes in every environment

Development already validates scopes and the whole container at startup. These two options make Production do the same, so a singleton that captures a scoped service (a DbContext, the unit of work, this service) stops the app at startup instead of sharing one context across requests.

In `AdventureWorks/Program.cs`, add these lines after the line `var builder = WebApplication.CreateBuilder(args);`.

```csharp
// Lifetime checks in every environment, not only Development.
builder.Host.UseDefaultServiceProvider(options =>
    options.ValidateScopes = options.ValidateOnBuild = true);
```

## Program.cs at the checkpoint (80 lines)

Program.cs, part 1 of 3 (35 lines):

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

`requests.http` holds these requests in this order; 18 of 18 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `POST /api/orders` returned **401**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"traceId":"00-259cac7c501e303568d7df9a6c8d0186-61506fce46a3d457-02","correlationId":"f10369833bc244578de7666d026ea183"}
  ```

- `POST /dev/sign-in/casey` returned **200**

  ```text
  {"userName":"casey@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqYa5URoWzuy_hSRI-f_8ezdPQqHo4JkyJsk1dxMniWLMvao7zmlk-xBOs7TZvFYG2rJoPOSlt7EJdt16okMxyt8iNW329O6JGpNAc9B4FwrbVgqzJStZGH9wSzY0nYgsv8lkMbf_yzA9LwI3rTJqACSyvXL80b7PMwMtieSnnP5Zg"}
  ```

- `POST /api/orders` returned **201** (Location: https://localhost:7001/api/orders/13)

  ```text
  {"id":13,"orderNumber":"AW-0013","ownerId":"e72a5673-9526-43a5-a99c-b1ed24b84878","total":248.98,"status":"Pending","archived":false,"notes":null,"version":"fa72fc07-001d-4484-8278-c961fe6ac875","items":[{"productId":1,"...
  ```

- `GET /api/orders/13` returned **200**

  ```text
  {"id":13,"orderNumber":"AW-0013","ownerId":"e72a5673-9526-43a5-a99c-b1ed24b84878","total":248.98,"status":"Pending","archived":false,"notes":null,"version":"fa72fc07-001d-4484-8278-c961fe6ac875","items":[{"productId":1,"...
  ```

- `POST /api/orders` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Items":["The field Items must be a string or array type with a minimum length of '1'....
  ```

- `POST /api/orders` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Items[0].Quantity":["The field Quantity must be between 1 and 20."]},"traceId":"00-c7...
  ```

- `POST /api/orders` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,"detail":"Use one line per product.","traceId":"00-f9827f0e48985ec2897a7507a0bf9142-746695e5eb07a046-02","correlationId":"6d...
  ```

- `POST /api/orders` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,"detail":"A product on the order is not sold.","traceId":"00-842609180d7778445dbb8c14abac9435-0e1d9de06d059000-02","correlat...
  ```

- `POST /api/orders` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,"traceId":"00-2993e1c31d4bc49eb93a94a154f9a92d-573e5b90d5a5a747-02","correlationId":"40dcbda00c8d4f778113e7640366cddc"}
  ```

- `POST /dev/sign-in/jordan` returned **200**

  ```text
  {"userName":"jordan@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqbXetjzgvCF6YQVDp4xeN8500yT5re1ueuBF6eiaMKj-fAgW74_NQvRuKzXL6eamUjjhsgolt2VQTsoqTvMybammM8KyyT35_znR6AQfHL4u-H32G_9e7E7V-w-I0BG79HbBucmhc6yfJdbHigmzNSZM2RDFRMQSdwYu8w23aClAQ"}
  ```

- `POST /api/orders/13/cancel` returned **403**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.4","title":"Forbidden","status":403,"traceId":"00-e5427becdd718531af884c1526329f11-a010a4d5585153f2-02","correlationId":"651e0d798ffa475b82e58889ad9d856d"}
  ```

- `POST /dev/sign-in/casey` returned **200**

  ```text
  {"userName":"casey@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqZrF9FYVRrzdfh-xfc0ZojsyYn9JgWuahnyIlyLZ3AhrzLIEkfs6Qn0UueFODqNEYsdUqjvH_IZlNtncDt3whR3tbVSLa7eRXXl6vfuN0C4YCWc6fn_q3ynFEky5zHZg4Szr9papEWWC5VEtRqtMyve3mZI5cEh-lNgbaRH-C8gBg"}
  ```

- `POST /api/orders/13/cancel` returned **200**

  ```text
  {"id":13,"orderNumber":"AW-0013","ownerId":"e72a5673-9526-43a5-a99c-b1ed24b84878","total":248.98,"status":"Cancelled","archived":false,"notes":null,"version":"d5c086ad-3cb8-4cd8-8b11-1a7ccc7faaf2","items":[{"productId":1...
  ```

- `POST /api/orders/13/cancel` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,"detail":"The order is already cancelled.","traceId":"00-9da05085ef0ef3181dac32f5737667bd-ae20f074ae48049b-02","correlationI...
  ```

- `POST /dev/sign-out` returned **204**

### Other runs

- The app log has one decorator line per service call, for example "Create order: Created in 181 ms" (the first order, with EF Core warming up) and "Cancel order: Cancelled in 29 ms". Checked.
- Eight orders placed at the same moment (eight sessions released by a barrier): one 201 (AW-0015) and seven 409s with "Another order took that number; send it again.", no 500, no duplicate number; the created order was cancelled afterward. All eight read the same newest order and picked the same number, and the unique index refused seven inserts. EF Core logged each refused insert at Error. Checked.
- Production starts with both validation options and serves the catalog (200) and refuses an anonymous order (401). Checked.
- A captive dependency, registered on purpose in a scratch copy (`AddSingleton<IOrderWriter, EfOrderWriter>`, which holds the scoped DbContext), stops the app at startup in Production: "Cannot consume scoped service 'AdventureWorks.Infrastructure.AdventureWorksDbContext' from singleton 'AdventureWorks.Core.IOrderWriter'." Without the new options, Production would have started and shared one context across requests. Checked.
- requests.http ran twice against the same database: 18 of 18 both times; each run places one order and cancels it.
- Delete race (review 11-P10): a scratch copy registers a probe (tools/scenarios/DeleteRaceProbe.cs, never part of the checkpoint) that deletes the order through a second connection, as the Admin delete would, at the point an X-Delete-At header names: after the load and before the staging FindAsync, after staging and before SaveChanges, or after SaveChanges and before the read-back. PUT notes, cancel and create each answered 404 at every point, with the probe's X-Probe-Deleted header showing the row was deleted, and the order read 404 afterward; without the header the same PUT and cancel answered 200: 24 of 24 (`tools/scenarios/ch11-delete-race.http`). The same file against the printed (pre-fix) code: PUT 404, 409, 200 with an empty body; cancel 200 with an empty body, 409, 200 with an empty body; create 500 (NullReferenceException in CreateOrder). Checked; Chapter 12 passes the same file.

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks.Core/OrderService.cs` on line 25:

```csharp
var number = await writer.NextOrderNumberAsync(ct);
```

Press F5 (the https profile), then send POST https://localhost:7001/api/orders as casey with two helmets (sent with "unitPrice": 0.01) and a tent. When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
ownerId = 6f5a3a60-fb73-4cc2-ab7f-d4cb672281e0 [String]
request.Items.Count = 2 [Int32]
ids.Length = 2 [Int32]
products.Count = 2 [Int32]
items[0] = OrderItem { ProductId = 1, Quantity = 2, UnitPrice = 29.99 } [OrderItem]
items[1] = OrderItem { ProductId = 4, Quantity = 1, UnitPrice = 189.00 } [OrderItem]
```

AddValidation has already checked the request's shape, and the endpoint passed the signed-in user's ID as ownerId. products holds the catalog rows for both IDs, loaded in one query, and items carries their prices: the helmet line shows 29.99, not the 0.01 the request sent, because OrderLineRequest has no price to bind. Step Over to see the next order number; the order is staged by writer.Add and saved by the unit of work two lines later.

## What the Activity adds

The original Activity's items mostly exercise the checkpoint (empty lines, zero quantity, duplicate lines, unknown product, server-side prices, another owner), and requests.http now covers each of them. This Activity adds new code: a second decorator, `AuditingOrderService`, around the logging one, and `OrderAudit`, a singleton that keeps the last 20 outcomes, which the Admin reads at `GET /api/admin/orders/audit`. The decorator is scoped because the service it wraps is; the singleton is safe to use from it, never the other way round.

Checked: casey gets 403 on the audit; avery gets the outcomes newest first, Cancel Invalid, Cancel Cancelled, Create Invalid twice, Create Created (the 400s that validation stopped never reached the service). And the mistake the Activity warns about, in a scratch copy: registering the decorator chain with AddSingleton and a factory. A factory hides its dependencies from ValidateOnBuild, so the app starts; ValidateScopes refuses on the first order instead, a 500 with "Cannot resolve scoped service 'AdventureWorks.Core.OrderService' from root provider." Say so: validation at build sees constructor dependencies, not what a factory resolves.

The reader-path check rebuilt `AdventureWorks/Chapter11-Activity` from the Chapter 11 checkpoint: no differences.
The Activity adds 75 lines of code: 63 in 1 new file and 12 new or changed in existing files, plus 0 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

### Activity: a second decorator and a singleton it may use

Work in a copy of the Chapter 11 checkpoint folder; Chapter 12 starts from the checkpoint. The Activity keeps the last 20 order outcomes for the Admin. The store of outcomes lives as long as the app, so it is a singleton; the decorator that writes to it wraps a scoped service, so it is scoped too. A singleton may be used by a scoped service, never the other way round.

Create `AdventureWorks/OrderAudit.cs` (63 lines).

OrderAudit.cs, part 1 of 2 (26 lines):

```csharp
using System.Collections.Concurrent;
using AdventureWorks.Authorization;
using AdventureWorks.Core;

namespace AdventureWorks;

public sealed record OrderAuditEntry(DateTimeOffset At,
    string Operation, string Outcome, string? OrderNumber);

// The last 20 order outcomes, kept for the life of the app: a
// singleton, safe for many requests at once.
public sealed class OrderAudit(TimeProvider clock)
{
    private readonly ConcurrentQueue<OrderAuditEntry> _entries
        = new();

    public void Record(string operation, OrderResult result)
    {
        _entries.Enqueue(new(clock.GetUtcNow(), operation,
            result.Outcome.ToString(),
            result.Order?.OrderNumber));
        while (_entries.Count > 20) _entries.TryDequeue(out _);
    }

    public OrderAuditEntry[] Recent() => [.. _entries.Reverse()];
}
```

OrderAudit.cs, part 2 of 2 (36 lines):

```csharp
// A second decorator. It wraps a scoped service, so it is scoped;
// it may use the singleton OrderAudit.
public sealed class AuditingOrderService(IOrderService inner,
    OrderAudit audit) : IOrderService
{
    public async Task<OrderResult> CreateAsync(string ownerId,
        CreateOrderRequest request,
        CancellationToken ct = default)
    {
        var result =
            await inner.CreateAsync(ownerId, request, ct);
        audit.Record("Create", result);
        return result;
    }

    public async Task<OrderResult> CancelAsync(string ownerId,
        int id, CancellationToken ct = default)
    {
        var result = await inner.CancelAsync(ownerId, id, ct);
        audit.Record("Cancel", result);
        return result;
    }
}

public static class OrderAuditEndpoints
{
    public static IEndpointRouteBuilder MapOrderAudit(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/orders/audit",
                (OrderAudit audit) => audit.Recent())
            .WithTags("Orders")
            .RequireAuthorization(StorePolicies.AdminOnly);
        return app;
    }
}
```

In `AdventureWorks/OrderEndpoints.cs`, replace these lines. In AddOrderServices.

```csharp
        // The service, wrapped by its logging decorator.
        services.AddScoped<OrderService>();
        services.AddScoped<IOrderService>(provider =>
            new LoggingOrderService(
                provider.GetRequiredService<OrderService>(),
                provider.GetRequiredService<
                    ILogger<LoggingOrderService>>()));
```

With these:

```csharp
        // The service, wrapped by the logging decorator, wrapped
        // by the auditing decorator (Activity).
        services.AddScoped<OrderService>();
        services.AddSingleton<OrderAudit>();
        services.AddScoped<IOrderService>(provider =>
            new AuditingOrderService(
                new LoggingOrderService(
                    provider.GetRequiredService<OrderService>(),
                    provider.GetRequiredService<
                        ILogger<LoggingOrderService>>()),
                provider.GetRequiredService<OrderAudit>()));
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.MapInventoryEndpoints();`.

```csharp
app.MapOrderAudit();
```

### Activity requests and responses

21 of 21 requests in `Chapter11-Activity/requests.http` returned their commented status. The responses for the requests the Activity adds:

- `GET /api/admin/orders/audit` returned **403**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.4","title":"Forbidden","status":403,"traceId":"00-f08315c06f7fdc482f283eff891b1b3d-99fe402f5bfcce4c-02","correlationId":"3ccec4da062a49b4837290fc2d1b105f"}
  ```

- `POST /dev/sign-in/avery` returned **200**

  ```text
  {"userName":"avery@adventure-works.test"}
  ```

- `GET /api/admin/orders/audit` returned **200**

  ```text
  [{"at":"2026-11-03T17:52:15.8888922+00:00","operation":"Cancel","outcome":"Invalid","orderNumber":null},{"at":"2026-11-03T17:52:15.8829132+00:00","operation":"Cancel","outcome":"Cancelled","orderNumber":"AW-0013"},{"at":...
  ```

- `POST /dev/sign-out` returned **204**

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| Step 1 DataModels.cs (97 lines, printed twice in this chapter) | Removed | Chapter 10's model is used unchanged; nothing is copied. |
| OrderServices.cs (112 lines, printed twice) | Replaced | `OrderContracts.cs` and `OrderService.cs` in Core, `EfOrderWriter.cs` and `EfUnitOfWork.cs` in Infrastructure, `LoggingOrderService.cs` in the web project. RequestedItem becomes OrderLineRequest; CreateOrderRequest has no OrderNumber (the service numbers orders) and validates its own shape; OrderResult carries an outcome instead of a bool; ICatalogRepository is Chapter 4's IProductService with GetByIdsAsync. |
| Chapter11.csproj (14 lines) | Removed | No new project and no new package. |
| Program.cs (44 lines) | Replaced | Chapter 10's Program.cs plus the UseDefaultServiceProvider lines; the registrations live in AddOrderServices. 80 lines. |
| `/csrf`, `POST /api/orders`, `GET /api/orders/{id:guid}` | Replaced | `/api/csrf` (Chapter 9), `POST /api/orders`, `GET /api/orders/{id:int}` (Chapter 9's owner-or-Finance read), and the new `POST /api/orders/{id}/cancel`. |
| DevSignIn.cs, DevSeed.cs (companion) | Replaced | Chapter 8's DevSignIn and Chapter 10's seeds. |

## Notes for the writers

- The service lives in Core, not in the web project or Infrastructure, because it depends only on Core interfaces; that is what lets Chapter 15 unit-test it with fakes.
- Narrow repositories: the service reads through Chapter 10's IOrderRepository and writes through IOrderWriter, which only stages. Chapter 10's own write methods (the notes update, archive and delete) still save at once; they are single-purpose and keep working. Say that the unit of work applies to what the service stages.
- The order number is the next one after the newest order ID. Two requests can pick the same number at once; the unique index from Chapter 10 refuses the second, and the client sends the order again (the race scenario shows it). Deleting the newest order (Chapter 9's Admin delete) lets its number be issued again; real stores number orders from a sequence.
- After a save, the service reads the new order back by its number to return it with its ID and Location: one extra query, and no EF Core type leaks into Core.
- Departure: the design lists GET for the service; this chapter's service has CreateAsync and CancelAsync, and GET /api/orders/{id} keeps Chapter 9's resource-based rule. Chapter 13 adds the owner's list.
- Cancel loads the order, applies Chapter 9's OrderOwner policy (403 for jordan), then calls the service, which checks the owner again and stages the status with the version it read.
- Review 11-P10, an order deleted part-way through a write: every write now answers 404 at each point. Deleted after the load, before staging: SetStatusAsync (and Chapter 10's notes update) finds no row, NotFound. Deleted after staging, before the save: the UPDATE matches no row, and EfUnitOfWork asks the database for the row's current values (GetDatabaseValuesAsync); none means OrderDeletedException, NotFound, while a row with another version stays OrderChangedException, 409. Deleted after the save, before the read-back: OrderService.Saved turns the missing order into NotFound instead of an outcome with no order. Chapter 10's PUT notes path has had the same rule since Chapter 10. Before the fix the same runs gave a 409 instead of 404, 200 with an empty body (PUT and cancel), and a 500 (NullReferenceException) for a create whose order was deleted before the read-back.
- AddValidation validates the nested lines: a zero quantity returns 400 with the key `Items[0].Quantity`; an empty list returns 400 for `Items`.
- The decorator needs no package: register OrderService, then IOrderService with a factory that wraps it.
- Program.cs is 80 lines.
