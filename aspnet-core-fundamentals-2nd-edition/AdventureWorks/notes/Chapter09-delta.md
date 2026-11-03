# Chapter 9 delta: Authorization and Security Fundamentals

Generated November 3, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter09` and the operations that rebuild it from the Chapter 8 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 9 protects the running app with policies instead of a separate Chapter09 project. Documents become orders (inventory option A), and the four personas map to Chapter 8's accounts.

- Core: `Order(int Id, string OrderNumber, string OwnerId, decimal Total, bool Archived = false)`; IOrderRepository gains Add, Delete and Archive. Infrastructure's in-memory repository keeps its orders in the instance and is now a singleton. Order 500 still simulates a storage failure.
- `Authorization/Requirements.cs`: `MinimumAgeRequirement` and its handler (DateOfBirth claim, TimeProvider), and the resource-based `OrderAccessRequirement(bool AllowFinance)` with `OrderAccessHandler`: the owner passes, and Finance staff pass when AllowFinance is true.
- `Authorization/DepartmentPolicies.cs`: `DepartmentRequirement`, its handler, and `DepartmentPolicyProvider`, which builds Department:Finance and Department:Shipping on demand and returns no policy for any other name.
- `Authorization/StorePolicies.cs`: the fallback policy (RequireAuthenticatedUser), the named policies AdminOnly (role "Admin", the app's only role), AtLeast21, OrderAccess (owner or Finance) and OrderOwner (owner only), and the handler and provider registrations.
- The antiforgery header becomes X-CSRF-TOKEN, and `GET /api/csrf` returns a signed-in caller's token.
- Every public endpoint now says so with AllowAnonymous: `/`, `/api/status`, `/health`, the catalog group, the v1 and v2 product reads, `/Products`, the sign-in page, `/api/auth/login` and `/refresh`, the test sign-in and OpenAPI in Development. The `/ping` branch and static files run before authorization and stay public unchanged. Everything else (orders, the order form and its validation API, the profile, the Development diagnostics) needs a signed-in user.
- Chapter 5's product writes (v1 controller and v2 endpoints) and the product editor page need the Admin role; the `/Admin` area is protected; a new `/Products/Restricted` page needs AtLeast21; `/Account/AccessDenied` is where a signed-in user who fails a page policy lands.
- Orders: `GET /api/orders/{id}` loads the order, then authorizes OrderAccess (owner or Finance). `DELETE /api/orders/{id}` needs AdminOnly and the antiforgery header. `POST /api/orders/{id}/archive` needs Department:Finance on the endpoint, the antiforgery header, and then OrderOwner on the loaded order.
- The Development seed adds the Admin role for avery and three orders: AW-0001 and AW-0003 for casey, AW-0002 for jordan.

## Files added or changed

| File | Change |
| --- | --- |
| `AdventureWorks.Core/Orders.cs` | changed |
| `AdventureWorks.Infrastructure/Services.cs` | changed |
| `AdventureWorks/OrderEndpoints.cs` | changed |
| `AdventureWorks/Authorization/Requirements.cs` | added |
| `AdventureWorks/Authorization/DepartmentPolicies.cs` | added |
| `AdventureWorks/Authorization/StorePolicies.cs` | added |
| `AdventureWorks/Program.cs` | changed |
| `AdventureWorks/StatusEndpoints.cs` | changed |
| `AdventureWorks/CatalogEndpoints.cs` | changed |
| `AdventureWorks/ProductEndpointsV2.cs` | changed |
| `AdventureWorks/Controllers/ProductsController.cs` | changed |
| `AdventureWorks/AccountEndpoints.cs` | changed |
| `AdventureWorks/DevSignIn.cs` | changed |
| `AdventureWorks/Pages/_ViewImports.cshtml` | changed |
| `AdventureWorks/Pages/Products/Index.cshtml` | changed |
| `AdventureWorks/Pages/Products/Edit.cshtml.cs` | changed |
| `AdventureWorks/Pages/Account/Login.cshtml.cs` | changed |
| `AdventureWorks/Pages/Account/AccessDenied.cshtml` | added |
| `AdventureWorks/Pages/Products/Restricted.cshtml` | added |
| `AdventureWorks/Areas/Admin/Pages/Index.cshtml` | replaced |
| `AdventureWorks/DevelopmentSeed.cs` | changed |

The reader types 387 lines of code: 158 in 5 new files and 229 new or changed in existing files, plus 0 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 61 of 61 files identical and no differences.

### Step 1: Give orders an owner and the operations to protect

Authorization needs a resource with an owner. Order gains OwnerId and Archived, and the repository gains Add, Delete and Archive. The in-memory repository now holds its orders in the instance, so it becomes a singleton (Chapter 10's EF Core repository goes back to scoped). Order 500 still simulates a storage failure.

In `AdventureWorks.Core/Orders.cs`, replace these lines.

```csharp
public sealed record Order(
    int Id, string OrderNumber, decimal Total);

public interface IOrderRepository
{
    Order? GetById(int id);
}
```

With these:

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

In `AdventureWorks.Infrastructure/Services.cs`, replace these lines.

```csharp
public sealed class OrderRepository : IOrderRepository
{
    public Order? GetById(int id) => id switch
    {
        1 => new Order(1, "AW-0001", 49.00m),
        500 => throw new InvalidOperationException(
            "Simulated storage failure."),
        _ => null
    };
}
```

With these:

```csharp
public sealed class OrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<int, Order> _orders
        = new();
    private int _lastId;

    public Order? GetById(int id) => id == 500
        ? throw new InvalidOperationException(
            "Simulated storage failure.")
        : _orders.GetValueOrDefault(id);

    public Order Add(string ownerId, decimal total)
    {
        var id = Interlocked.Increment(ref _lastId);
        var order = new Order(
            id, $"AW-{id:0000}", ownerId, total);
        _orders[id] = order;
        return order;
    }

    public bool Delete(int id) => _orders.TryRemove(id, out _);

    public bool Archive(int id)
    {
        while (_orders.TryGetValue(id, out var current))
        {
            var archived = current with { Archived = true };
            if (_orders.TryUpdate(id, archived, current))
                return true;
        }
        return false;
    }
}
```

In `AdventureWorks/OrderEndpoints.cs`, replace these lines. Inside AddOrderServices.

```csharp
        services.AddScoped<IOrderRepository, OrderRepository>();
```

With these:

```csharp
        services.AddSingleton<IOrderRepository,
            OrderRepository>();
```

### Step 2: Requirements, handlers and named policies

Three files in a new Authorization folder. MinimumAgeHandler reads the DateOfBirth claim through TimeProvider. OrderAccessHandler is resource-based: it runs only after an endpoint has loaded the order, and lets the owner in, or Finance staff when the requirement allows it. DepartmentPolicyProvider builds Department:Finance and Department:Shipping on demand and returns no policy for any other name. StorePolicies registers the fallback policy, the named policies and the handlers.

Create `AdventureWorks/Authorization/Requirements.cs` (52 lines).

Requirements.cs, part 1 of 2 (30 lines):

```csharp
using System.Globalization;
using System.Security.Claims;
using AdventureWorks.Core;
using Microsoft.AspNetCore.Authorization;

namespace AdventureWorks.Authorization;

public sealed record MinimumAgeRequirement(int MinimumAge)
    : IAuthorizationRequirement;

public sealed class MinimumAgeHandler(TimeProvider clock)
    : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumAgeRequirement requirement)
    {
        var value = context.User.FindFirstValue("DateOfBirth");
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None,
            out var born))
        {
            var today = DateOnly.FromDateTime(
                clock.GetUtcNow().UtcDateTime);
            if (born.AddYears(requirement.MinimumAge) <= today)
                context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
```

Requirements.cs, part 2 of 2 (21 lines):

```csharp
// Resource-based: decided only after the order has been loaded.
public sealed record OrderAccessRequirement(bool AllowFinance)
    : IAuthorizationRequirement;

public sealed class OrderAccessHandler
    : AuthorizationHandler<OrderAccessRequirement, Order>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrderAccessRequirement requirement, Order order)
    {
        var user = context.User;
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is not null && id == order.OwnerId)
            context.Succeed(requirement);
        else if (requirement.AllowFinance &&
            user.HasClaim("Department", "Finance"))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
```

Create `AdventureWorks/Authorization/DepartmentPolicies.cs` (53 lines).

DepartmentPolicies.cs, part 1 of 2 (31 lines):

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AdventureWorks.Authorization;

public sealed record DepartmentRequirement(string Name)
    : IAuthorizationRequirement;

public sealed class DepartmentHandler
    : AuthorizationHandler<DepartmentRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DepartmentRequirement requirement)
    {
        if (context.User.HasClaim("Department", requirement.Name))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

// Builds the Department:Finance and Department:Shipping policies
// on demand. Any other department is unknown: no policy at all.
public sealed class DepartmentPolicyProvider(
    IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    const string Prefix = "Department:";
    static readonly string[] Known = ["Finance", "Shipping"];
    readonly DefaultAuthorizationPolicyProvider fallback =
        new(options);
```

DepartmentPolicies.cs, part 2 of 2 (21 lines):

```csharp
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string name)
    {
        if (!name.StartsWith(Prefix, StringComparison.Ordinal))
            return fallback.GetPolicyAsync(name);
        var department = name[Prefix.Length..];
        if (!Known.Contains(department))
            return Task.FromResult<AuthorizationPolicy?>(null);
        var requirement = new DepartmentRequirement(department);
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(requirement)
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
```

Create `AdventureWorks/Authorization/StorePolicies.cs` (38 lines).

```csharp
using Microsoft.AspNetCore.Authorization;

namespace AdventureWorks.Authorization;

public static class StorePolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string AtLeast21 = "AtLeast21";
    public const string OrderAccess = "OrderAccess";
    public const string OrderOwner = "OrderOwner";
    public const string FinanceDepartment = "Department:Finance";

    public static IServiceCollection AddStorePolicies(
        this IServiceCollection services)
    {
        // Every endpoint needs a signed-in user unless it says
        // otherwise: public endpoints call AllowAnonymous.
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().Build())
            .AddPolicy(AdminOnly, p => p.RequireRole("Admin"))
            .AddPolicy(AtLeast21, p => p.AddRequirements(
                new MinimumAgeRequirement(21)))
            .AddPolicy(OrderAccess, p => p.AddRequirements(
                new OrderAccessRequirement(AllowFinance: true)))
            .AddPolicy(OrderOwner, p => p.AddRequirements(
                new OrderAccessRequirement(AllowFinance: false)));
        services.AddSingleton<IAuthorizationHandler,
            MinimumAgeHandler>();
        services.AddSingleton<IAuthorizationHandler,
            OrderAccessHandler>();
        services.AddScoped<IAuthorizationHandler,
            DepartmentHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider,
            DepartmentPolicyProvider>();
        return services;
    }
}
```

In `AdventureWorks/Program.cs`, add these lines after the line `builder.Configuration, builder.Environment);`. After the AddAccountServices call from Chapter 8.

```csharp
builder.Services.AddStorePolicies();
```

In `AdventureWorks/Program.cs`, add these lines after the line `using AdventureWorks;`.

```csharp
using AdventureWorks.Authorization;
```

In `AdventureWorks/Program.cs`, replace these lines. Cookie-authenticated API writes send their antiforgery token in this header.

```csharp
builder.Services.AddAntiforgery();
```

With these:

```csharp
builder.Services.AddAntiforgery(options =>
    options.HeaderName = "X-CSRF-TOKEN");
```

### Step 3: The fallback policy and the public endpoints

With RequireAuthenticatedUser as the fallback, every endpoint without its own authorization metadata needs a signed-in user. That is the point of a fallback policy, and its cost: each public endpoint must now say so. These are the endpoints that stay public: the status routes and health check, the catalog, the product API reads, the product list page, the sign-in page and the token API, the test sign-in, and OpenAPI in Development. The /ping branch and the static files run before authorization, so they stay public without a change. Everything else (orders, the order form and its validation API, the profile, the Development diagnostics) now needs a signed-in user.

In `AdventureWorks/StatusEndpoints.cs`, replace these lines.

```csharp
        app.MapHealthChecks("/health");
        app.MapGet("/", () => Results.Redirect("/api/status"));
        app.MapGet("/api/status", () => Results.Ok(new
        {
            Status = "Operational",
            Timestamp = DateTime.UtcNow
        }));
```

With these:

```csharp
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapGet("/", () => Results.Redirect("/api/status"))
            .AllowAnonymous();
        app.MapGet("/api/status", () => Results.Ok(new
        {
            Status = "Operational",
            Timestamp = DateTime.UtcNow
        })).AllowAnonymous();
```

In `AdventureWorks/CatalogEndpoints.cs`, replace these lines.

```csharp
        var catalog = app.MapGroup("/api/catalog")
            .WithTags("Catalog");
```

With these:

```csharp
        var catalog = app.MapGroup("/api/catalog")
            .WithTags("Catalog").AllowAnonymous();
```

In `AdventureWorks/ProductEndpointsV2.cs`, replace these lines. Inside MapProductEndpointsV2: reads are public, writes need the Admin role.

```csharp
        group.MapGet("/", SearchProducts);
        group.MapGet("/{id:int}", GetProduct)
            .WithName("GetProductV2");
        group.MapPost("/", CreateProduct)
            .ProducesValidationProblem();
        group.MapPut("/{id:int}", UpdateProduct)
            .ProducesValidationProblem();
        group.MapDelete("/{id:int}", DeleteProduct);
```

With these:

```csharp
        group.MapGet("/", SearchProducts).AllowAnonymous();
        group.MapGet("/{id:int}", GetProduct)
            .WithName("GetProductV2").AllowAnonymous();
        group.MapPost("/", CreateProduct)
            .ProducesValidationProblem()
            .RequireAuthorization(StorePolicies.AdminOnly);
        group.MapPut("/{id:int}", UpdateProduct)
            .ProducesValidationProblem()
            .RequireAuthorization(StorePolicies.AdminOnly);
        group.MapDelete("/{id:int}", DeleteProduct)
            .RequireAuthorization(StorePolicies.AdminOnly);
```

In `AdventureWorks/ProductEndpointsV2.cs`, add these lines after the line `using AdventureWorks.Core;`.

```csharp
using AdventureWorks.Authorization;
```

In `AdventureWorks/Controllers/ProductsController.cs`, replace these lines. The writes need the Admin role; the two GET actions opt out below.

```csharp
using System.ComponentModel.DataAnnotations;
using AdventureWorks.Core;
using Microsoft.AspNetCore.Mvc;

namespace AdventureWorks.Controllers;

[ApiController]
[Route("api/v1/products")]
```

With these:

```csharp
using System.ComponentModel.DataAnnotations;
using AdventureWorks.Authorization;
using AdventureWorks.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdventureWorks.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize(Policy = StorePolicies.AdminOnly)]
```

In `AdventureWorks/Controllers/ProductsController.cs`, replace these lines.

```csharp
    [HttpGet]
    public async Task<ActionResult<Product[]>> Search(
```

With these:

```csharp
    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<Product[]>> Search(
```

In `AdventureWorks/Controllers/ProductsController.cs`, replace these lines.

```csharp
    [HttpGet("{id:int}", Name = "GetProductV1")]
```

With these:

```csharp
    [HttpGet("{id:int}", Name = "GetProductV1"), AllowAnonymous]
```

In `AdventureWorks/AccountEndpoints.cs`, replace these lines.

```csharp
        app.MapPost("/api/auth/login", LogIn);
        app.MapPost("/api/auth/refresh", Refresh);
```

With these:

```csharp
        app.MapPost("/api/auth/login", LogIn).AllowAnonymous();
        app.MapPost("/api/auth/refresh", Refresh)
            .AllowAnonymous();
        // A signed-in caller fetches its token for API writes.
        app.MapGet("/api/csrf", (HttpContext context,
            IAntiforgery antiforgery) => Results.Ok(new
        {
            Token = antiforgery.GetAndStoreTokens(context)
                .RequestToken
        }));
```

In `AdventureWorks/AccountEndpoints.cs`, add these lines after the line `using AdventureWorks.Infrastructure.Identity;`.

```csharp
using Microsoft.AspNetCore.Antiforgery;
```

In `AdventureWorks/DevSignIn.cs`, replace these lines.

```csharp
        var dev = app.MapGroup("/dev").WithTags("Development");
```

With these:

```csharp
        var dev = app.MapGroup("/dev").WithTags("Development")
            .AllowAnonymous();
```

In `AdventureWorks/Program.cs`, replace these lines.

```csharp
    app.MapOpenApi();
```

With these:

```csharp
    app.MapOpenApi().AllowAnonymous();
```

### Step 3 (continued): the pages

Pages take the same metadata as attributes. The product list and the sign-in page are public; the product editor and the Admin area need the Admin role; a new page of age-restricted products needs the AtLeast21 policy; and a small access-denied page is where the cookie sends a signed-in user who fails a page's policy.

In `AdventureWorks/Pages/_ViewImports.cshtml`, add these lines after the line `@using AdventureWorks.Core`. So every page can name the attributes and the policy names.

```razor
@using AdventureWorks.Authorization
@using Microsoft.AspNetCore.Authorization
```

In `AdventureWorks/Pages/Products/Index.cshtml`, replace these lines.

```razor
@page
@inject IProductService Products
```

With these:

```razor
@page
@attribute [AllowAnonymous]
@inject IProductService Products
```

In `AdventureWorks/Pages/Products/Edit.cshtml.cs`, replace these lines.

```csharp
using AdventureWorks.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdventureWorks.Pages.Products;

public sealed class EditModel(IProductService products)
```

With these:

```csharp
using AdventureWorks.Authorization;
using AdventureWorks.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdventureWorks.Pages.Products;

[Authorize(Policy = StorePolicies.AdminOnly)]
public sealed class EditModel(IProductService products)
```

In `AdventureWorks/Pages/Account/Login.cshtml.cs`, replace these lines.

```csharp
public sealed class LoginModel(
```

With these:

```csharp
[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public sealed class LoginModel(
```

Create `AdventureWorks/Pages/Account/AccessDenied.cshtml` (7 lines).

```razor
@page
@attribute [AllowAnonymous]
@{
    ViewData["Title"] = "Access denied";
}
<h1>Access denied</h1>
<p>Your account cannot open that page.</p>
```

Create `AdventureWorks/Pages/Products/Restricted.cshtml` (8 lines).

```razor
@page
@attribute [Authorize(Policy = StorePolicies.AtLeast21)]
@{
    ViewData["Title"] = "Age-restricted products";
}
<h1>Age-restricted products</h1>
<p>Camp stove fuel and fixed-blade knives ship only to customers
aged 21 or over.</p>
```

Replace the whole of `AdventureWorks/Areas/Admin/Pages/Index.cshtml` with the following.

```razor
@page
@using AdventureWorks.Authorization
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize(Policy = StorePolicies.AdminOnly)]
<h1>Administration</h1>
<p>Signed in as @User.Identity?.Name, in the Admin role.</p>
```

### Step 4: Resource-based order rules

The order endpoints load the order first and authorize second. GET /api/orders/{id} is for the owner or Finance staff. DELETE needs the Admin role. POST /api/orders/{id}/archive needs the Department:Finance policy and then the owner rule on the loaded order, so it needs both. Both writes check the antiforgery header first.

In `AdventureWorks/OrderEndpoints.cs`, replace these lines.

```csharp
        orders.MapGet("/{id:int}", GetOrder)
            .WithName("GetOrderById");
```

With these:

```csharp
        orders.MapGet("/{id:int}", GetOrder)
            .WithName("GetOrderById");
        orders.MapDelete("/{id:int}", DeleteOrder)
            .RequireAuthorization(StorePolicies.AdminOnly);
        orders.MapPost("/{id:int}/archive", ArchiveOrder)
            .RequireAuthorization(
                StorePolicies.FinanceDepartment);
```

In `AdventureWorks/OrderEndpoints.cs`, replace the GetOrder method with the following.

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
        var order = orders.GetById(id) ??
            throw new ResourceNotFoundException(
                $"Order {id} was not found.");
        // Load first, then authorize against the loaded order.
        var access = await authorization.AuthorizeAsync(
            user, order, StorePolicies.OrderAccess);
        return access.Succeeded
            ? Results.Ok(order) : Results.Forbid();
    }
```

In `AdventureWorks/OrderEndpoints.cs`, add the following below `static async Task<IResult> GetOrder(int id,`.

```csharp
    static async Task<IResult> DeleteOrder(int id,
        IOrderRepository orders, HttpContext context,
        IAntiforgery antiforgery)
    {
        if (!await HasValidTokenAsync(context, antiforgery))
            return Results.BadRequest();
        return orders.Delete(id)
            ? Results.NoContent() : Results.NotFound();
    }
```

In `AdventureWorks/OrderEndpoints.cs`, add the following below `static async Task<IResult> DeleteOrder(`.

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
        var order = orders.GetById(id);
        if (order is null) return Results.NotFound();
        var owner = await authorization.AuthorizeAsync(
            user, order, StorePolicies.OrderOwner);
        if (!owner.Succeeded) return Results.Forbid();
        return orders.Archive(id)
            ? Results.NoContent() : Results.NotFound();
    }
```

In `AdventureWorks/OrderEndpoints.cs`, add the following below `static async Task<IResult> ArchiveOrder(`.

```csharp
    static async Task<bool> HasValidTokenAsync(
        HttpContext context, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }
```

In `AdventureWorks/OrderEndpoints.cs`, replace these lines.

```csharp
using AdventureWorks.Core;
using AdventureWorks.Infrastructure;
using Microsoft.Extensions.Options;
```

With these:

```csharp
using System.Security.Claims;
using AdventureWorks.Authorization;
using AdventureWorks.Core;
using AdventureWorks.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
```

### Step 5: Roles and orders in the Development seed

avery joins the new Admin role, and three orders get owners: AW-0001 and AW-0003 belong to casey (a customer), AW-0002 to jordan (a business buyer in Finance). morgan is Finance staff who owns nothing. These accounts give the four archive outcomes: anonymous 401, casey (owner, not Finance) 403, morgan (Finance, not owner) 403, jordan (Finance owner) 204.

In `AdventureWorks/DevelopmentSeed.cs`, replace these lines.

```csharp
        foreach (var account in Accounts)
            await CreateAsync(users, account, password);
    }
```

With these:

```csharp
        foreach (var account in Accounts)
            await CreateAsync(users, account, password);
        await AddAdminAsync(services, users);
        AddOrders(services, users);
    }
```

In `AdventureWorks/DevelopmentSeed.cs`, add the following above `static void Check(IdentityResult result)`.

```csharp
    static async Task AddAdminAsync(IServiceProvider services,
        UserManager<ApplicationUser> users)
    {
        var roles = services
            .GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync("Admin"))
            Check(await roles.CreateAsync(
                new IdentityRole("Admin")));
        var avery =
            await users.FindByNameAsync($"avery@{Domain}");
        if (avery is not null &&
            !await users.IsInRoleAsync(avery, "Admin"))
            Check(await users.AddToRoleAsync(avery, "Admin"));
    }
```

In `AdventureWorks/DevelopmentSeed.cs`, add the following above `static void Check(IdentityResult result)`.

```csharp
    // In-memory orders for the owners' account IDs. Chapter 10
    // moves them into the database.
    static void AddOrders(IServiceProvider services,
        UserManager<ApplicationUser> users)
    {
        var orders =
            services.GetRequiredService<IOrderRepository>();
        if (orders.GetById(1) is not null) return;
        string Id(string name) => users.Users
            .Single(u => u.UserName == $"{name}@{Domain}").Id;
        orders.Add(Id("casey"), 49.00m);    // AW-0001
        orders.Add(Id("jordan"), 1299.00m); // AW-0002
        orders.Add(Id("casey"), 189.00m);   // AW-0003
    }
```

In `AdventureWorks/DevelopmentSeed.cs`, add these lines after the line `using AdventureWorks.Infrastructure;`. Order and IOrderRepository live in Core.

```csharp
using AdventureWorks.Core;
```

## Program.cs at the checkpoint (76 lines)

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

Program.cs, part 2 of 3 (18 lines):

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
```

Program.cs, part 3 of 3 (25 lines):

```csharp
app.UseWhen(context =>
    context.Request.Path.StartsWithSegments("/api"),
    api => api.Use(async (context, next) =>
    {
        context.Response.Headers["X-API-Pipeline"] = "true";
        await next();
    }));
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStatusEndpoints();
app.MapOrderEndpoints();
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

`requests.http` holds these requests in this order; 35 of 35 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `GET /api/catalog` returned **200**

  ```text
  {"items":[{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":...
  ```

- `GET /api/v1/products` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":"Mountain...
  ```

- `GET /Products` returned **200**

  ```text
  (an HTML page)
  ```

- `GET /health` returned **200**

  ```text
  Healthy
  ```

- `GET /api/orders/1` returned **401**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"traceId":"00-570872e5c3c5e70826b66d915a7b66c8-40deffc1c7661964-02","correlationId":"cb6a687f36d141b6ae5aa8efd324f994"}
  ```

- `POST /api/v1/products` returned **401**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"traceId":"00-d9f12b0f2a461c906026d0aabd6b14c5-610d0eb75e5ea723-02","correlationId":"3085ae7a51714bef9a115dd98f2292c1"}
  ```

- `GET /Admin` returned **302** (Location: https://localhost:7001/Account/Login?ReturnUrl=%2FAdmin)

- `POST /api/orders/1/archive` returned **401**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"traceId":"00-042c8461cee9570903d27e34e7aed014-3edc47c9eb9158b5-02","correlationId":"0626295faa004da190a0e8fb7afc3b08"}
  ```

- `POST /dev/sign-in/casey` returned **200**

  ```text
  {"userName":"casey@adventure-works.test"}
  ```

- `GET /api/orders/1` returned **200**

  ```text
  {"id":1,"orderNumber":"AW-0001","ownerId":"580a7cb5-e1ab-4e34-9f7c-c0a856817059","total":49.00,"archived":false}
  ```

- `GET /api/orders/2` returned **403**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.4","title":"Forbidden","status":403,"traceId":"00-2a8b87b1d8300cea278ac86dece1f050-1845ed36cad1ec6d-02","correlationId":"e5b58c97acf34792afdc523952f0d94e"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqbwJNNBy6AHWx2LRAdXd0c6d_gWtb3_BE32XPU-l0id-UMUjT-4u8q--eB6z0dpU5G2B5wPHXL2_-650cUR6WDsA64gwDbvExyW3zKAT9-cseQrJmEcgk6qtBISRa5xdNtT4YvsUaiSe548XwFal22piyWtZmAgao-5pC--FTWZnQ"}
  ```

- `POST /api/orders/1/archive` returned **403**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.4","title":"Forbidden","status":403,"traceId":"00-edbdf8e7f483a1742226ffd99fabc01a-bae747fa71978617-02","correlationId":"e3c36e0e306c431c85d46382aa4cf0f8"}
  ```

- `GET /Products/Restricted` returned **200**

  ```text
  (an HTML page)
  ```

- `POST /api/v2/products` returned **403**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.4","title":"Forbidden","status":403,"traceId":"00-1451adc4d18ccbcdd62beee3dfdaad93-1ed1cffff3bba97f-02","correlationId":"eb91a8d7b5764591ade5087c42c29a11"}
  ```

- `GET /Products/Edit/1` returned **302** (Location: https://localhost:7001/Account/AccessDenied?ReturnUrl=%2FProducts%2FEdit%2F1)

- `POST /dev/sign-in/morgan` returned **200**

  ```text
  {"userName":"morgan@adventure-works.test"}
  ```

- `GET /api/orders/1` returned **200**

  ```text
  {"id":1,"orderNumber":"AW-0001","ownerId":"580a7cb5-e1ab-4e34-9f7c-c0a856817059","total":49.00,"archived":false}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqYuSCMAomLqu2dYlREjVMADDmDjuC-YvZBrNn9moji6oU5Cji9crbW2efqoQQhlwH6Auq4dEdFuK6VcxYItlx8bXIK5NR9y6fpR-Zls2VAXKtAT_K7vDU--kTnrXfHaRr3VVngBCPNIURcQ9Qr44eNyCU-JzuiLmuj1utRXjh4dqQ"}
  ```

- `POST /api/orders/1/archive` returned **403**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.4","title":"Forbidden","status":403,"traceId":"00-00ac7188ca74aa07c792a69ca3a1924b-a3e16069254f6dfb-02","correlationId":"54362256f3d6493994528a68c3f212f1"}
  ```

- `POST /dev/sign-in/jordan` returned **200**

  ```text
  {"userName":"jordan@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqahU6c3SHx5Lx9oRyzVPJwOeufYjnY6yFs1CIN__x11Ccxbyek3eiBeYu5PcFV1l_UD4cE4Gw1SbJZvS5DHbuczsWJQDQv1OEmjW7h35XSU8b1DClK8Hd17abTCd7Z3ZTJ5QRNPpY4lua89jsxEV5-PhWq-ms8w5EWg9kPUwQTJWg"}
  ```

- `POST /api/orders/2/archive` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,"traceId":"00-2ed9aa654cc0edcbb5bffacc307a29eb-4fadacd8a2f165dd-02","correlationId":"ed74171196e1460fac0fbefccf0017b9"}
  ```

- `POST /api/orders/2/archive` returned **204**

- `GET /api/orders/2` returned **200**

  ```text
  {"id":2,"orderNumber":"AW-0002","ownerId":"d35e02f1-3475-4d8d-9d18-4e138162f49c","total":1299.00,"archived":true}
  ```

- `POST /dev/sign-in/riley` returned **200**

  ```text
  {"userName":"riley@adventure-works.test"}
  ```

- `GET /Products/Restricted` returned **302** (Location: https://localhost:7001/Account/AccessDenied?ReturnUrl=%2FProducts%2FRestricted)

- `POST /dev/sign-in/avery` returned **200**

  ```text
  {"userName":"avery@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqZQEUd4SqxbZAj-R25XMvv-woUjT6upHTccrYVs3zlCxEVYi4ObS77JCYfOtobYScd9jl0F7CJ5SvXed4uCxYci1UtdFXGaRyOJJ3z43_SCj_cpKjCUmbdjR8hQYT1HOvuDvVQXVWqJUDkZuJxsgsClWXnw0PSAccLDhUGo_8B65g"}
  ```

- `DELETE /api/orders/3` returned **204**

- `GET /api/orders/3` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found","status":404,"detail":"Order 3 was not found.","traceId":"00-a0217e965b16af64f8cf2eb61d7427f6-97aadcf5f381d246-02","correlationId"...
  ```

- `POST /api/v2/products` returned **201** (Location: https://localhost:7001/api/v2/products/5)

  ```text
  {"id":5,"name":"Trail Bottle","sku":"BTL-5001","price":24.99,"category":"accessories","version":1}
  ```

- `GET /Admin` returned **200**

  ```text
  <h1>Administration</h1>
  <p>Signed in as avery@adventure-works.test, in the Admin role.</p>
  ```

- `GET /Products/Edit/1` returned **200**

  ```text
  (an HTML page)
  ```

- `POST /dev/sign-out` returned **204**

### Other runs

- The four archive outcomes, all checked in requests.http: anonymous 401; casey (owner, not Finance) 403 from the endpoint policy; morgan (Finance, not owner) 403 from the resource rule; jordan (Finance owner) 400 without the antiforgery header and 204 with it, after which the order reads `"archived": true`.
- Production: `POST /dev/sign-in/casey` returns 302 to the sign-in page, not 404: under a fallback policy, authorization challenges a request even when no endpoint matched, and the Identity cookie redirects any path outside /api. `/api/catalog` still returns 200. Checked.

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks/Authorization/Requirements.cs` on line 45:

```csharp
if (id is not null && id == order.OwnerId)
```

Press F5 (the https profile), then send GET https://localhost:7001/api/orders/1 after signing in as morgan (Finance staff). When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
user.Identity!.Name = morgan@adventure-works.test [String]
id = d8932b3d-30d8-492b-b4b9-a271595fa734 [String]
order = Order { Id = 1, OrderNumber = AW-0001, OwnerId = c1640ff8-b028-4a7b-a5ca-b307a1a1388f, Total = 49.00, Archived = False } [Order]
requirement.AllowFinance = True [Boolean]
user.HasClaim("Department", "Finance") = True [Boolean]
```

The handler runs inside AuthorizeAsync, after the endpoint loaded the order. id is morgan's account ID and order.OwnerId is casey's, so the owner test fails; requirement.AllowFinance is true for the read policy and morgan has the Finance claim, so the next branch succeeds. On the archive endpoint the same handler runs with AllowFinance false, and the request ends in 403.

## What the Activity adds

The original Activity repeated the Full Example (continuity finding C09-2). This one adds new code: a Shipping staff account (sam) and `ShippingHolds.cs`, with `POST /api/orders/{id}/hold` guarded by Department:Shipping, the provider's second known department, and `GET /api/policies/department/{name}`, which asks the policy provider directly and shows its closed set. Checked: anonymous hold 401; sam 200 with `{"orderId":1,"held":true}`; morgan (Finance) 403; Shipping exists, Marketing does not.

The reader-path check rebuilt `AdventureWorks/Chapter09-Activity` from the Chapter 9 checkpoint: no differences.
The Activity adds 35 lines of code: 32 in 1 new file and 3 new or changed in existing files, plus 0 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

### Activity: a shipping hold on the second department policy

Work in a copy of the Chapter 9 checkpoint folder. Chapter 10 starts from the checkpoint. The Activity adds a Shipping staff account and an endpoint guarded by Department:Shipping, the second name the provider accepts, and counts how often the department handler runs.

Create `AdventureWorks/ShippingHolds.cs` (32 lines).

```csharp
using AdventureWorks.Core;
using Microsoft.AspNetCore.Authorization;

namespace AdventureWorks;

// Activity: a shipment hold guarded by the provider's second
// department policy, and a route that shows its closed set.
public static class ShippingHolds
{
    public static IEndpointRouteBuilder MapShippingHolds(
        this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/{id:int}/hold", Hold)
            .RequireAuthorization("Department:Shipping");
        app.MapGet("/api/policies/department/{name}", Exists);
        return app;
    }

    static IResult Hold(int id, IOrderRepository orders) =>
        orders.GetById(id) is null
            ? Results.NotFound()
            : Results.Ok(new { OrderId = id, Held = true });

    static async Task<IResult> Exists(string name,
        IAuthorizationPolicyProvider policies) =>
        Results.Ok(new
        {
            Policy = $"Department:{name}",
            Exists = await policies.GetPolicyAsync(
                $"Department:{name}") is not null
        });
}
```

In `AdventureWorks/DevelopmentSeed.cs`, replace these lines.

```csharp
        new("avery", "", "", "1975-11-20")            // admin
```

With these:

```csharp
        new("avery", "", "", "1975-11-20"),           // admin
        new("sam", "", "Shipping", "1992-03-08")      // shipping
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.MapAccountEndpoints();`.

```csharp
app.MapShippingHolds();
```

### Activity requests and responses

42 of 42 requests in `Chapter09-Activity/requests.http` returned their commented status. The responses for the requests the Activity adds:

- `POST /api/orders/1/hold` returned **401**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"traceId":"00-d01cbc68d346d0f015abeb8c59a40d55-a4fe439348ae874c-02","correlationId":"c4654888de5148e491d4fed3ead887b8"}
  ```

- `POST /dev/sign-in/sam` returned **200**

  ```text
  {"userName":"sam@adventure-works.test"}
  ```

- `POST /api/orders/1/hold` returned **200**

  ```text
  {"orderId":1,"held":true}
  ```

- `GET /api/policies/department/Shipping` returned **200**

  ```text
  {"policy":"Department:Shipping","exists":true}
  ```

- `GET /api/policies/department/Marketing` returned **200**

  ```text
  {"policy":"Department:Marketing","exists":false}
  ```

- `POST /dev/sign-in/morgan` returned **200**

  ```text
  {"userName":"morgan@adventure-works.test"}
  ```

- `POST /api/orders/1/hold` returned **403**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.4","title":"Forbidden","status":403,"traceId":"00-a638afbdb72226277164d5fc8541293b-e63a251135b1a336-02","correlationId":"35cf60371c8c44e48969fa798bc4ed00"}
  ```

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| Step 1 Chapter09.csproj | Removed | No new project. |
| AuthorizationTypes.cs (73 lines) | Replaced | Three files in Authorization/: Requirements.cs (the age and order-access requirements and handlers), DepartmentPolicies.cs (department requirement, handler and provider) and StorePolicies.cs (policy names and registrations). Document and DocumentRepository are gone: orders are the resource. |
| Program.cs (63 lines) | Replaced | Chapter 8's Program.cs plus AddStorePolicies, the X-CSRF-TOKEN antiforgery option and `MapOpenApi().AllowAnonymous()`; 76 lines. The "Cookies" scheme and its 401/403 events are gone: Chapter 8's Identity cookie events already answer 401 and 403 for /api paths. |
| /csrf, /, /restricted-content, /api/documents routes | Replaced | `/api/csrf`, the status routes, `/Products/Restricted`, and the `/api/orders` routes. |
| DevSignIn.cs (companion) | Replaced | Chapter 8's DevSignIn, now AllowAnonymous; its four personas map one to one to casey, morgan, jordan and avery. |

## Notes for the writers

- Chapter 9 marks each public endpoint, as the design asks; the delta lists every one, which shows the real cost of a fallback policy.
- The product editor page needs the Admin role. The design lists "the product pages" as public; the list page is, but the editor writes products, and Chapter 5's product writes now need the Admin role, so the editor follows them.
- The order form and `/api/orders/validate` need a signed-in user from here on, because orders belong to the signed-in user.
- The Development diagnostics (`/api/config`, `/examples`) now need a signed-in user too: the design's list of public endpoints does not include them.
- The Chapter 3 requests that read `/api/orders/1` anonymously now get 401; that is this chapter's change.
- The in-memory order repository moves from scoped to singleton because it now holds state; say so in one sentence, and that Chapter 10's repository is scoped again.
- Owner IDs are Identity's GUID account IDs; the seed looks them up by user name.
- Program.cs is 76 lines.
