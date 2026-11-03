# Chapter 13 delta: Designing RESTful APIs

Generated November 3, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter13` and the operations that rebuild it from the Chapter 12 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 13 reshapes the order API as REST resources over Chapter 11's OrderService, in the same app. No second service and no second store.

- `OrderDtos.cs`: OrderDto and OrderLineDto (no owner ID; the Version moves to the ETag header), `PagedResponse<T>` (items, page, page size, total, total pages), OrderNotesPatch (the one patchable field, validated), and `StoreJsonContext`, the System.Text.Json source-generated context for these types. AddOrderServices inserts it first in the HTTP JSON resolver chain; reflection stays in the chain for every other type (design decision 10).
- Core: IOrderService gains ListAsync (a page and the owner's total) and UpdateNotesAsync (staged through IOrderWriter.SetNotesAsync, saved by the unit of work, version-checked); OrderPage; OrderOutcome.Updated. IOrderRepository's ListAsync takes a page size and gains CountAsync; Chapter 10's self-saving UpdateNotesAsync is gone.
- Routes: `GET /api/orders` (the signed-in owner's orders, page 1 to 1000, 1 to 50 per page, Link header with prev and next), `GET /api/orders/{id}` (OrderDto and ETag), `POST /api/orders` (201, Location, ETag), `PATCH /api/orders/{id}` (If-Match required: 428 without it, 412 when stale, 200 with the new ETag), `DELETE /api/orders/{id}` (cancels: 204, or 409 when already cancelled). Finance's archive and the validation route stay. The Admin's permanent delete moves to `DELETE /api/admin/orders/{id}`.
- Removed: Chapter 10's `GET /api/orders/report` and `PUT /api/orders/{id}` and Chapter 11's `POST /api/orders/{id}/cancel`, which the collection, PATCH and DELETE replace; OrderNotesInput.
- Program.cs is unchanged: 88 lines.

## Files added or changed

| File | Change |
| --- | --- |
| `AdventureWorks/OrderDtos.cs` | added |
| `AdventureWorks/OrderEndpoints.cs` | changed |
| `AdventureWorks.Core/OrderContracts.cs` | changed |
| `AdventureWorks.Core/Orders.cs` | changed |
| `AdventureWorks.Core/OrderService.cs` | changed |
| `AdventureWorks/LoggingOrderService.cs` | changed |
| `AdventureWorks.Infrastructure/Data/EfOrderRepository.cs` | changed |
| `AdventureWorks.Infrastructure/Data/EfOrderWriter.cs` | changed |
| `AdventureWorks/OrderInputs.cs` | changed |

The reader types 347 lines of code: 49 in 1 new file and 298 new or changed in existing files, plus 0 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 85 of 85 files identical and no differences.

### Step 1: Response contracts and generated JSON

The API now returns OrderDto, not Core's Order: a contract can drop what clients must not see (the owner's account ID) and move what belongs in HTTP (the Version becomes the ETag header) without changing the domain. PagedResponse carries one page and the totals. StoreJsonContext is the System.Text.Json source generator: serialization code for these types is generated at build time instead of discovered by reflection at run time. It goes first in the resolver chain, and reflection stays in the chain for every other type.

Create `AdventureWorks/OrderDtos.cs` (49 lines).

OrderDtos.cs, part 1 of 2 (22 lines):

```csharp
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdventureWorks.Core;

namespace AdventureWorks;

// What the order API returns: no owner ID, and the Version
// travels in the ETag header instead of the body.
public sealed record OrderLineDto(
    int ProductId, int Quantity, decimal UnitPrice);

public sealed record OrderDto(int Id, string OrderNumber,
    string Status, bool Archived, decimal Total, string? Notes,
    IReadOnlyList<OrderLineDto> Items)
{
    public static OrderDto From(Order order) => new(order.Id,
        order.OrderNumber, order.Status, order.Archived,
        order.Total, order.Notes,
        [.. order.Items.Select(i => new OrderLineDto(
            i.ProductId, i.Quantity, i.UnitPrice))]);
}
```

OrderDtos.cs, part 2 of 2 (26 lines):

```csharp
public sealed record PagedResponse<T>(IReadOnlyList<T> Items,
    int Page, int PageSize, int TotalCount)
{
    public int TotalPages =>
        (int)Math.Ceiling(TotalCount / (double)PageSize);
}

// PATCH body: the one field a customer may change.
public sealed class OrderNotesPatch
{
    [StringLength(500)]
    public string? Notes { get; set; }
}

// Serialization code generated at build time for the order API's
// types. AddOrderServices puts it first in the resolver chain;
// reflection still serves every other type.
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(OrderDto))]
[JsonSerializable(typeof(PagedResponse<OrderSummary>))]
[JsonSerializable(typeof(CreateOrderRequest))]
[JsonSerializable(typeof(OrderNotesPatch))]
public sealed partial class StoreJsonContext
    : JsonSerializerContext
{
}
```

In `AdventureWorks/OrderEndpoints.cs`, replace the AddOrderServices method with the following. One addition at the end: ConfigureHttpJsonOptions inserts StoreJsonContext first.

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
        // Chapter 13: generated JSON code for the order API
        // first, reflection for everything else.
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain
                .Insert(0, StoreJsonContext.Default));
        return services;
    }
```

### Step 2: The service owns the list and the notes change too

Chapter 11's service gains the two operations the REST endpoints need, so the endpoints keep calling one service: ListAsync (a page and the owner's total) and UpdateNotesAsync, which stages the change through IOrderWriter with the client's version and saves through the unit of work, like CancelAsync. Chapter 10's repository method that saved notes itself is gone, and ListAsync takes a page size.

In `AdventureWorks.Core/OrderContracts.cs`, replace these lines.

```csharp
public enum OrderOutcome { Created, Cancelled, NotFound, Invalid,
    Conflict }
```

With these:

```csharp
public enum OrderOutcome { Created, Updated, Cancelled, NotFound,
    Invalid, Conflict }
```

In `AdventureWorks.Core/OrderContracts.cs`, replace the IOrderService interface with the following.

```csharp
public interface IOrderService
{
    Task<OrderPage> ListAsync(string ownerId, int page,
        int pageSize, CancellationToken ct = default);
    Task<OrderResult> CreateAsync(string ownerId,
        CreateOrderRequest request,
        CancellationToken ct = default);
    Task<OrderResult> CancelAsync(string ownerId, int id,
        CancellationToken ct = default);
    Task<OrderResult> UpdateNotesAsync(string ownerId, int id,
        string? notes, Guid version,
        CancellationToken ct = default);
}
```

In `AdventureWorks.Core/OrderContracts.cs`, add these lines above the line `public interface IOrderService`.

```csharp
public sealed record OrderPage(OrderSummary[] Items, int Page,
    int PageSize, int TotalCount);
```

In `AdventureWorks.Core/OrderContracts.cs`, replace the IOrderWriter interface with the following.

```csharp
// Writes that wait for the unit of work: nothing here saves.
public interface IOrderWriter
{
    Task<string> NextOrderNumberAsync(CancellationToken ct);
    void Add(NewOrder order);
    Task<bool> SetStatusAsync(int id, string status,
        Guid version, CancellationToken ct);
    Task<bool> SetNotesAsync(int id, string? notes,
        Guid version, CancellationToken ct);
}
```

In `AdventureWorks.Core/Orders.cs`, replace the IOrderRepository interface with the following.

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<Order?> GetByNumberAsync(string orderNumber);
    Task<OrderSummary[]> ListAsync(
        string ownerId, int page, int pageSize);
    Task<int> CountAsync(string ownerId);
    Task<bool> ArchiveAsync(int id);
    Task<bool> DeleteAsync(int id);
}
```

In `AdventureWorks.Core/OrderService.cs`, add the following above `static OrderResult Invalid(string error) =>`.

```csharp
    public async Task<OrderPage> ListAsync(string ownerId,
        int page, int pageSize, CancellationToken ct = default) =>
        new(await orders.ListAsync(ownerId, page, pageSize), page,
            pageSize, await orders.CountAsync(ownerId));
```

In `AdventureWorks.Core/OrderService.cs`, add the following above `static OrderResult Invalid(string error) =>`.

```csharp
    // The same staging and version check as CancelAsync, for the
    // notes the customer may change.
    public async Task<OrderResult> UpdateNotesAsync(
        string ownerId, int id, string? notes, Guid version,
        CancellationToken ct = default)
    {
        var order = await orders.GetByIdAsync(id);
        if (order is null || order.OwnerId != ownerId)
            return new(OrderOutcome.NotFound);
        if (!await writer.SetNotesAsync(id, notes, version, ct))
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
        return Saved(OrderOutcome.Updated,
            await orders.GetByIdAsync(id));
    }
```

In `AdventureWorks/LoggingOrderService.cs`, add the following above `async Task<OrderResult> LogAsync(string operation,`.

```csharp
    public Task<OrderPage> ListAsync(string ownerId, int page,
        int pageSize, CancellationToken ct = default) =>
        inner.ListAsync(ownerId, page, pageSize, ct);
```

In `AdventureWorks/LoggingOrderService.cs`, add the following above `async Task<OrderResult> LogAsync(string operation,`.

```csharp
    public Task<OrderResult> UpdateNotesAsync(string ownerId,
        int id, string? notes, Guid version,
        CancellationToken ct = default) =>
        LogAsync("Update", () => inner.UpdateNotesAsync(
            ownerId, id, notes, version, ct));
```

In `AdventureWorks.Infrastructure/Data/EfOrderRepository.cs`, delete these lines.

```csharp
    public const int PageSize = 10;
```

In `AdventureWorks.Infrastructure/Data/EfOrderRepository.cs`, replace the ListAsync method with the following. The page size is a parameter now.

```csharp
    // One bounded page, newest first. SQL sums the cents.
    public async Task<OrderSummary[]> ListAsync(
        string ownerId, int page, int pageSize)
    {
        var rows = await db.Orders.AsNoTracking()
            .Where(o => o.OwnerId == ownerId)
            .OrderByDescending(o => o.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
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

In `AdventureWorks.Infrastructure/Data/EfOrderRepository.cs`, delete the UpdateNotesAsync method.

In `AdventureWorks.Infrastructure/Data/EfOrderRepository.cs`, add the following below `public async Task<OrderSummary[]> ListAsync(`.

```csharp
    public Task<int> CountAsync(string ownerId) =>
        db.Orders.CountAsync(o => o.OwnerId == ownerId);
```

In `AdventureWorks.Infrastructure/Data/EfOrderWriter.cs`, add the following below `public async Task<bool> SetStatusAsync(int id, string status,`.

```csharp
    // Chapter 10's notes update, staged. The client's version
    // goes in the UPDATE's WHERE clause at the save.
    public async Task<bool> SetNotesAsync(int id, string? notes,
        Guid version, CancellationToken ct)
    {
        var row = await db.Orders.FindAsync([id], ct);
        if (row is null) return false;
        db.Entry(row).Property(o => o.Version).OriginalValue =
            version;
        row.Notes = notes;
        return true;
    }
```

### Step 3: Orders as resources

The order API becomes one collection, /api/orders, of the signed-in customer's orders. GET on the collection returns one bounded page and puts links to the neighboring pages in the Link header (RFC 8288). GET on an order returns its OrderDto with an ETag; POST answers 201 with a Location and an ETag; PATCH changes the notes only if If-Match carries the current ETag (none is 428 Precondition Required, a stale one 412 Precondition Failed); DELETE cancels (204), and the order stays readable. Chapter 10's report and PUT, and Chapter 11's POST /cancel, become these. The Admin's permanent delete moves to /api/admin/orders/{id}, so DELETE on a customer's resource means one thing.

In `AdventureWorks/OrderEndpoints.cs`, replace the MapOrderEndpoints method with the following.

```csharp
    // The orders collection of the signed-in customer. Each order
    // is a resource: GET reads it, PATCH changes it, DELETE
    // cancels it. Finance archives; the Admin deletes.
    public static IEndpointRouteBuilder MapOrderEndpoints(
        this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/api/orders")
            .WithTags("Orders");
        orders.MapGet("/", ListOrders).WithName("ListOrders");
        orders.MapGet("/{id:int}", GetOrder)
            .WithName("GetOrderById");
        orders.MapPost("/", CreateOrder);
        orders.MapPatch("/{id:int}", PatchOrder);
        orders.MapDelete("/{id:int}", CancelOrder);
        orders.MapPost("/{id:int}/archive", ArchiveOrder)
            .RequireAuthorization(
                StorePolicies.FinanceDepartment);
        orders.MapPost("/validate", (OrderApiInput input) =>
            TypedResults.Ok(new { input.Sku, input.Quantity }));
        app.MapDelete("/api/admin/orders/{id:int}", DeleteOrder)
            .WithTags("Orders")
            .RequireAuthorization(StorePolicies.AdminOnly);
        return app;
    }
```

In `AdventureWorks/OrderEndpoints.cs`, add the following below `public static IEndpointRouteBuilder MapOrderEndpoints(`.

```csharp
    // One bounded page, with links to its neighbors in the Link
    // header (RFC 8288) and the total in the body.
    static async Task<IResult> ListOrders(IOrderService service,
        ClaimsPrincipal user, HttpContext context,
        LinkGenerator links, CancellationToken ct,
        [Range(1, 1000)] int page = 1,
        [Range(1, 50)] int pageSize = 10)
    {
        var owner = user.FindFirstValue(
            ClaimTypes.NameIdentifier)!;
        var result =
            await service.ListAsync(owner, page, pageSize, ct);
        var body = new PagedResponse<OrderSummary>(result.Items,
            page, pageSize, result.TotalCount);
        List<string> rels = [];
        if (page > 1) rels.Add(Link(page - 1, "prev"));
        if (page < body.TotalPages)
            rels.Add(Link(page + 1, "next"));
        if (rels.Count > 0)
            context.Response.Headers.Link =
                string.Join(", ", rels);
        return Results.Ok(body);

        string Link(int target, string rel)
        {
            var uri = links.GetUriByName(context, "ListOrders",
                new { page = target, pageSize });
            return $"<{uri}>; rel=\"{rel}\"";
        }
    }
```

In `AdventureWorks/OrderEndpoints.cs`, replace the GetOrder method with the following. It returns the representation with its ETag now.

```csharp
    static async Task<IResult> GetOrder(int id,
        IOrderRepository orders, IOptions<FeatureFlags> flags,
        IAuthorizationService authorization, ClaimsPrincipal user,
        HttpContext context)
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
        if (!access.Succeeded) return Results.Forbid();
        return Represent(context, order);
    }
```

In `AdventureWorks/OrderEndpoints.cs`, replace the CreateOrder method with the following.

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
        if (result.Outcome != OrderOutcome.Created)
            return ToProblem(result);
        SetETag(context, result.Order!);
        return Results.CreatedAtRoute("GetOrderById",
            new { id = result.Order!.Id },
            OrderDto.From(result.Order));
    }
```

In `AdventureWorks/OrderEndpoints.cs`, add the following below `static async Task<IResult> CreateOrder(`.

```csharp
    // A conditional update: If-Match must carry the ETag the
    // client read. None is 428; a stale one is 412.
    static async Task<IResult> PatchOrder(int id,
        OrderNotesPatch patch, IOrderService service,
        ClaimsPrincipal user, HttpContext context,
        IAntiforgery antiforgery, CancellationToken ct)
    {
        if (!await HasValidTokenAsync(context, antiforgery))
            return Results.BadRequest();
        var ifMatch = context.Request.Headers.IfMatch.ToString();
        if (ifMatch.Length == 0)
            return Results.Problem("Send If-Match with the ETag.",
                statusCode:
                    StatusCodes.Status428PreconditionRequired);
        if (!Guid.TryParse(ifMatch.Trim('"'), out var version))
            return Results.Problem("If-Match is not an ETag.",
                statusCode:
                    StatusCodes.Status412PreconditionFailed);
        var owner = user.FindFirstValue(
            ClaimTypes.NameIdentifier)!;
        var result = await service.UpdateNotesAsync(
            owner, id, patch.Notes, version, ct);
        return result.Outcome switch
        {
            OrderOutcome.Updated =>
                Represent(context, result.Order!),
            OrderOutcome.Conflict => Results.Problem(result.Error,
                statusCode:
                    StatusCodes.Status412PreconditionFailed),
            _ => ToProblem(result)
        };
    }
```

In `AdventureWorks/OrderEndpoints.cs`, replace the CancelOrder method with the following. DELETE answers 204, or 409 when the order is already cancelled.

```csharp
    // DELETE cancels: the order stays, with status Cancelled.
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
        return result.Outcome switch
        {
            OrderOutcome.Cancelled => Results.NoContent(),
            OrderOutcome.Invalid => Results.Problem(result.Error,
                statusCode: StatusCodes.Status409Conflict),
            _ => ToProblem(result)
        };
    }
```

In `AdventureWorks/OrderEndpoints.cs`, delete the GetReport method.

In `AdventureWorks/OrderEndpoints.cs`, delete the UpdateNotes method.

In `AdventureWorks/OrderEndpoints.cs`, add the following below `static IResult ToProblem(OrderResult result) =>`.

```csharp
    static IResult Represent(HttpContext context, Order order)
    {
        SetETag(context, order);
        return Results.Ok(OrderDto.From(order));
    }
```

In `AdventureWorks/OrderEndpoints.cs`, add the following below `static IResult Represent(HttpContext context, Order order)`.

```csharp
    // The Version concurrency token from Chapter 10, as an ETag.
    static void SetETag(HttpContext context, Order order) =>
        context.Response.Headers.ETag = $"\"{order.Version}\"";
```

In `AdventureWorks/OrderInputs.cs`, delete the OrderNotesInput class (the PUT body; OrderNotesPatch replaces it).

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

`requests.http` holds these requests in this order; 21 of 21 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `GET /api/orders` returned **401**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"traceId":"00-c800fa8c970a95fbd8f87b66f8ffc8b2-0031e50fd972d4cd-02","correlationId":"10fc9c0507a64ed9a6a06371751dbe59"}
  ```

- `POST /dev/sign-in/casey` returned **200**

  ```text
  {"userName":"casey@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqY2jAnKgeIlaw16ntkvJJ1U2ULpgGfK3ckqztVU_uKW1Ob2lF-vs02GZ8Ijf6xo9D2GnVlsa15_SpeMePquBu0GfvbquC6cvepfmoCf_Ge2ZGfqR3YFHkF45VSouI5aZSUeOo4WeZyIxFPlLr-Pb0vsZKq9lfvL5TeM6sLbaUUVAQ"}
  ```

- `GET /api/orders?page=1&pageSize=5` returned **200** (Link: <https://localhost:7001/api/orders?page=2&pageSize=5>; rel="next")

  ```text
  {"items":[{"id":12,"orderNumber":"AW-0012","total":29.99,"status":"Pending","archived":false},{"id":11,"orderNumber":"AW-0011","total":89.97,"status":"Pending","archived":false},{"id":10,"orderNumber":"AW-0010","total":5...
  ```

- `GET /api/orders?page=3&pageSize=5` returned **200** (Link: <https://localhost:7001/api/orders?page=2&pageSize=5>; rel="prev")

  ```text
  {"items":[{"id":1,"orderNumber":"AW-0001","total":49.99,"status":"Pending","archived":false}],"page":3,"pageSize":5,"totalCount":11,"totalPages":3}
  ```

- `GET /api/orders?pageSize=500` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"pageSize":["The field pageSize must be between 1 and 50."]},"traceId":"00-6151b3a724f...
  ```

- `GET /api/orders/1` returned **200** (ETag: "84ca142b-8794-4d4c-98ef-8338f25c6975")

  ```text
  {"id":1,"orderNumber":"AW-0001","status":"Pending","archived":false,"total":49.99,"notes":null,"items":[{"productId":2,"quantity":1,"unitPrice":49.99}]}
  ```

- `PATCH /api/orders/1` returned **428**

  ```text
  {"title":"Precondition Required","status":428,"detail":"Send If-Match with the ETag.","traceId":"00-fe7431242fcceb9f23f308d5ab8d616a-7d74eddd4626f12b-02","correlationId":"bcd4712970934a66b5e7064d17931834"}
  ```

- `PATCH /api/orders/1` returned **200**

  ```text
  {"id":1,"orderNumber":"AW-0001","status":"Pending","archived":false,"total":49.99,"notes":"Leave it at the side door.","items":[{"productId":2,"quantity":1,"unitPrice":49.99}]}
  ```

- `PATCH /api/orders/1` returned **412**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.13","title":"Precondition Failed","status":412,"detail":"The order changed; read it again.","traceId":"00-6c743f43183fae0582cfa4b3bcfbf961-1f253731c05e0772-02","c...
  ```

- `PATCH /api/orders/1` returned **200**

  ```text
  {"id":1,"orderNumber":"AW-0001","status":"Pending","archived":false,"total":49.99,"notes":null,"items":[{"productId":2,"quantity":1,"unitPrice":49.99}]}
  ```

- `POST /api/orders` returned **201** (Location: https://localhost:7001/api/orders/13, Location: https://localhost:7001/api/orders/13)

  ```text
  {"id":13,"orderNumber":"AW-0013","status":"Pending","archived":false,"total":49.99,"notes":null,"items":[{"productId":2,"quantity":1,"unitPrice":49.99}]}
  ```

- `DELETE /api/orders/13` returned **204**

- `DELETE /api/orders/13` returned **409**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.10","title":"Conflict","status":409,"detail":"The order is already cancelled.","traceId":"00-3b864cf87ba296982e2a7a1bf840e884-9684a61f9f1c02a5-02","correlationId"...
  ```

- `GET /api/orders/13` returned **200**

  ```text
  {"id":13,"orderNumber":"AW-0013","status":"Cancelled","archived":false,"total":49.99,"notes":null,"items":[{"productId":2,"quantity":1,"unitPrice":49.99}]}
  ```

- `GET /api/nope` returned **404** (Content-Type: application/problem+json)

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-a0cbb39441006c94d006be30e66991b9-f05045e9b2c1931b-02","correlationId":"8cf63ac4b2de4bd98158a918276eb1c3"}
  ```

- `GET /api/nope` returned **404** (Content-Type: text/plain)

  ```text
  Status Code: 404; Not Found
  ```

- `POST /dev/sign-in/avery` returned **200**

  ```text
  {"userName":"avery@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqb8rHzKc_wkrf3T9hraDs1JsPNlsdkPtemXG5LYDfheYslbxGxlnyv9IBMKpU8D919SyIDscreZx8_o9ds6g_VdB9mKby-urdpowmsrkldty0v9FtG3t0LZVigInsMuTW9c6oLdepfExWuOZWHqNFmWXttftMtBzw0MP244t7GnbA"}
  ```

- `DELETE /api/admin/orders/13` returned **204**

- `POST /dev/sign-out` returned **204**

### Other runs

- requests.http ran twice against the same database (the requests run, then the README's curl.exe run): 21 of 21 both times. The order it places is cancelled and then deleted by the Admin.
- InventoryService may stay stopped for this chapter's requests (review P11), and the requests.http header now says so: with nothing listening on 127.0.0.1:42132, requests.http gave 21 of 21 and the README's curl.exe run 21 of 21 (the Activity: 28 of 28 both ways), and the app log never mentions port 42132. No request here reads stock.
- Delete race (review 11-P10): the scratch probe from Chapter 11 (tools/scenarios/DeleteRaceProbe.cs, X-Delete-At: stage, save or reread) deleted the order at each point of PATCH, DELETE (cancel) and POST. Each answered 404, with X-Probe-Deleted showing the deletion, and the order read 404 afterward; without the header PATCH answered 200 and DELETE 204: 24 of 24 (`tools/scenarios/ch13-delete-race.http`). Against the printed (pre-fix) code: PATCH 500, 412, 500; DELETE 204, 409, 204; POST 500. Checked; Chapters 14 and 15 pass the same file.
- The README's curl.exe commands keep the ETag from a saved header file (`curl.exe -D order.txt`, then `-match '^ETag:'`) and send it back in If-Match. The app reads the version inside If-Match with or without its quotes, so the way Windows PowerShell 5.1 passes quotes to curl.exe does not matter; the If-Match checks passed (428, 200, 412, 200).

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks/OrderEndpoints.cs` on line 164:

```csharp
var result = await service.UpdateNotesAsync(
```

Press F5 (the https profile), then send PATCH https://localhost:7001/api/orders/1 as casey, with If-Match set to the ETag that GET /api/orders/1 returned. When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
id = 1 [Int32]
ifMatch = "277816d3-5894-4b7e-9cbe-9e5e15fa7600" [String]
version = 277816d3-5894-4b7e-9cbe-9e5e15fa7600 [Guid]
patch.Notes = Leave it at the side door. [String]
```

The endpoint has read If-Match as text, quotes and all, and parsed the GUID inside into version: the value Chapter 10's concurrency check needs. patch.Notes is the only field the body may set. Step Into the service call to see it stage the change with that version; the unit of work's save is where a stale version turns into 412.

## What the Activity adds

The original Activity's sub-collection (the order's line items) carries over, on this app: `OrderItemEndpoints.cs` maps `GET /api/orders/{id}/items` with the same bounds and Link header as the collection, after Chapter 9's read rule (owner or Finance), and StoreJsonContext gains `PagedResponse<OrderLineDto>`. A missing order is 404; pageSize outside 1 to 50 is 400. Checked: an order with three lines, pageSize=2: two lines, totalCount 3 and a next link; page 2: the third line (productId 4) and no Link header.

The reader-path check rebuilt `AdventureWorks/Chapter13-Activity` from the Chapter 13 checkpoint: no differences.
The Activity adds 53 lines of code: 51 in 1 new file and 2 new or changed in existing files, plus 0 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

### Activity: an order's lines as a paged sub-collection

Work in a copy of the Chapter 13 checkpoint folder; Chapter 14 starts from the checkpoint. GET /api/orders/{id}/items returns one page of an order's lines with the same bounds and Link header as the orders collection, after Chapter 9's read rule (owner or Finance); a missing order is 404.

Create `AdventureWorks/OrderItemEndpoints.cs` (51 lines).

OrderItemEndpoints.cs, part 1 of 2 (19 lines):

```csharp
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AdventureWorks.Authorization;
using AdventureWorks.Core;
using Microsoft.AspNetCore.Authorization;

namespace AdventureWorks;

// Activity: an order's lines as a paged sub-collection, with the
// same Link header and bounds as the orders collection.
public static class OrderItemEndpoints
{
    public static IEndpointRouteBuilder MapOrderItemEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/orders/{id:int}/items", ListItems)
            .WithTags("Orders").WithName("ListOrderItems");
        return app;
    }
```

OrderItemEndpoints.cs, part 2 of 2 (31 lines):

```csharp
    static async Task<IResult> ListItems(int id,
        IOrderRepository orders,
        IAuthorizationService authorization,
        ClaimsPrincipal user, HttpContext context,
        LinkGenerator links,
        [Range(1, 1000)] int page = 1,
        [Range(1, 50)] int pageSize = 10)
    {
        var order = await orders.GetByIdAsync(id);
        if (order is null) return Results.NotFound();
        var access = await authorization.AuthorizeAsync(
            user, order, StorePolicies.OrderAccess);
        if (!access.Succeeded) return Results.Forbid();
        OrderLineDto[] items = [.. order.Items
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new OrderLineDto(
                i.ProductId, i.Quantity, i.UnitPrice))];
        var body = new PagedResponse<OrderLineDto>(items, page,
            pageSize, order.Items.Count);
        if (page < body.TotalPages)
        {
            // The link keeps the order ID and the page size.
            var next = links.GetUriByName(context,
                "ListOrderItems",
                new { id, page = page + 1, pageSize });
            context.Response.Headers.Link =
                $"<{next}>; rel=\"next\"";
        }
        return Results.Ok(body);
    }
}
```

In `AdventureWorks/OrderDtos.cs`, add these lines after the line `[JsonSerializable(typeof(PagedResponse<OrderSummary>))]`.

```csharp
[JsonSerializable(typeof(PagedResponse<OrderLineDto>))]
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.MapOrderEndpoints();`.

```csharp
app.MapOrderItemEndpoints();
```

### Activity requests and responses

28 of 28 requests in `Chapter13-Activity/requests.http` returned their commented status. The responses for the requests the Activity adds:

- `DELETE /api/orders/14` returned **204**

- `POST /dev/sign-in/avery` returned **200**

  ```text
  {"userName":"avery@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqaUNdLm3Ggm6RXs9HsEfWCZKGBIIzqjYFZjehf8VGIT4JZTCgxFPDlCds8bHZQyTKyUuoxitYIyC-fd3MxfpugGAvyhyGXBsBDLADFFO-W-BZ6K-sMTNQcIAMRIvIhkMhhWe1AGhDwkQFIOiljW24oyB8un6n9viu18x0JL_YAPWA"}
  ```

- `DELETE /api/admin/orders/13` returned **204**

- `DELETE /api/admin/orders/14` returned **204**

- `POST /dev/sign-out` returned **204**

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| AdventureWorks.Rest.csproj | Removed | No new project. |
| Contracts.cs (26 lines) | Replaced | `OrderDtos.cs`: OrderDto has an int ID, the order number, status, total and items, and no customer name (the owner is the signed-in user, never a request field: C13-1, C13-2). UpdateOrderRequest becomes OrderNotesPatch. PaginationRequest becomes two validated query parameters. The JSON context keeps its role. |
| OrderStore.cs (57 lines) | Replaced | Chapter 11's OrderService, with ListAsync and UpdateNotesAsync added. |
| OrderEndpoints.cs (58 lines) | Replaced | The app's OrderEndpoints.cs: the collection, the item with ETag, POST, PATCH with If-Match, DELETE as cancel. |
| Program.cs (14 lines) | Replaced | Unchanged from Chapter 12 (88 lines); ConfigureHttpJsonOptions is in AddOrderServices. ProblemDetails and status-code pages are Chapter 3's. |
| Activity/ (companion) | Replaced | Chapter13-Activity, the line-items sub-collection on this app. |

## Notes for the writers

- Accuracy 13.1: status-code pages fill an otherwise empty 400 to 599 response whatever the Accept header; only the format depends on it. Checked here (signed in, on an unmapped /api path): `Accept: application/json` gave 404 `application/problem+json`; `Accept: text/html` gave 404 `text/plain` with the body "Status Code: 404; Not Found". Anonymous requests to unmapped paths get 401 instead, because of Chapter 9's fallback policy.
- DELETE cancels instead of deleting, so a customer's order history survives and the order stays readable (status Cancelled). Say why the Admin's permanent delete moved to /api/admin/orders/{id}: one method, one meaning, on the customer's resource.
- Review 11-P10: UpdateNotesAsync follows CancelAsync, so a PATCH whose order is deleted part-way through answers 404 at each point: SetNotesAsync finds no row (NotFound), the UPDATE matches no row and the database has none (EfUnitOfWork's OrderDeletedException, NotFound; a row with another version stays 412), or the read-back after the save finds nothing (OrderService.Saved, NotFound). Before the fix the printed code answered 500 (NullReferenceException in SetETag, called from Represent) when the order was deleted before staging or before the read-back, and 412 when it was deleted before the save.
- The ETag is Chapter 10's Version token, quoted. PATCH compares it through the unit of work's concurrency check: the If-Match value goes into the UPDATE's WHERE clause, so a lost race also answers 412.
- 428 Problem Details has no "type" link (the framework has none for 428); 412 links to RFC 9110 section 15.5.13.
- The Link header uses absolute URIs (LinkGenerator.GetUriByName), for example `<https://localhost:7001/api/orders?page=2&pageSize=5>; rel="next"`.
- Program.cs is unchanged (88 lines).
