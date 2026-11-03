# Chapter 6 delta: Razor Pages and Server-Side Rendering

Generated November 3, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter06` and the operations that rebuild it from the Chapter 5 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 6 adds Razor Pages over the same product store the catalog and both product APIs use. Everything from Chapters 1 to 5 stays.

- Core: `UpdateOutcome { Updated, NotFound, Conflict }` and an `IProductService.UpdateAsync(id, request, expectedVersion)` overload; Infrastructure implements it with the version check. Chapter 5's PUT routes keep the unchecked update.
- Pages: `/Products` (Index, lists the first 20 products), `/Products/Edit/{id:int}` (EditModel with a bound `ProductInput`, server validation, the version check, Post-Redirect-Get with TempData, and a named `OnPostDeleteAsync` handler), `_ViewImports`, `_ViewStart`, `_Layout`, the prefixed `_ProductForm` partial, and the `ProductSummary` View Component.
- `Areas/Admin/Pages/Index.cshtml` serves `/Admin`; Chapter 9 protects it.
- Program.cs adds AddRazorPages, AddAntiforgery, UseAntiforgery (after the routing-dependent middleware, where Chapters 8 and 9 later put authentication and authorization just before it) and MapRazorPages: 59 lines.

Because there is one store, a page edit is visible at `/api/v2/products/1` with version 2, and an API update makes an open edit form stale.

## Files added or changed

| File | Change |
| --- | --- |
| `AdventureWorks.Core/Products.cs` | changed |
| `AdventureWorks.Infrastructure/InMemoryProductService.cs` | changed |
| `AdventureWorks/Pages/Products/ProductInput.cs` | added |
| `AdventureWorks/Pages/Products/Edit.cshtml.cs` | added |
| `AdventureWorks/Pages/Products/Edit.cshtml` | added |
| `AdventureWorks/Pages/Products/Index.cshtml` | added |
| `AdventureWorks/Areas/Admin/Pages/Index.cshtml` | added |
| `AdventureWorks/Pages/_ViewImports.cshtml` | added |
| `AdventureWorks/Pages/_ViewStart.cshtml` | added |
| `AdventureWorks/Pages/Shared/_Layout.cshtml` | added |
| `AdventureWorks/Pages/Shared/_ProductForm.cshtml` | added |
| `AdventureWorks/ProductSummaryViewComponent.cs` | added |
| `AdventureWorks/Pages/Shared/Components/ProductSummary/Default.cshtml` | added |
| `AdventureWorks/Program.cs` | changed |

The reader types 189 lines of code: 164 in 11 new files and 25 new or changed in existing files, plus 0 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 40 of 40 files identical and no differences.

### Step 1: Add a version-checked update to the product store

The pages edit the same products as the catalog and both APIs, so they use IProductService, not a second store. A stale form must not overwrite a newer edit, so the store gains an update that checks the Version the form was loaded with. Chapter 5's PUT routes keep their unchecked update.

In `AdventureWorks.Core/Products.cs`, add these lines above the line `public interface IProductService`.

```csharp
public enum UpdateOutcome { Updated, NotFound, Conflict }
```

In `AdventureWorks.Core/Products.cs`, add these lines after the line `Task<bool> UpdateAsync(int id, UpdateProductRequest request);`.

```csharp
    Task<UpdateOutcome> UpdateAsync(int id,
        UpdateProductRequest request, int expectedVersion);
```

In `AdventureWorks.Infrastructure/InMemoryProductService.cs`, add these lines above the line `public Task<bool> DeleteAsync(int id) =>`. After Chapter 5's UpdateAsync, above DeleteAsync.

```csharp
    public Task<UpdateOutcome> UpdateAsync(int id,
        UpdateProductRequest request, int expectedVersion)
    {
        while (_products.TryGetValue(id, out var current))
        {
            if (current.Version != expectedVersion)
                return Task.FromResult(UpdateOutcome.Conflict);
            var changed = current with
            {
                Name = request.Name,
                Price = request.Price,
                Version = current.Version + 1
            };
            if (_products.TryUpdate(id, changed, current))
                return Task.FromResult(UpdateOutcome.Updated);
        }
        return Task.FromResult(UpdateOutcome.NotFound);
    }
```

### Step 1 (continued): the page input model

ProductInput holds only the editable fields and the version. It is a page type, so it lives with the page in Web.

Create `AdventureWorks/Pages/Products/ProductInput.cs` (15 lines).

```csharp
using System.ComponentModel.DataAnnotations;

namespace AdventureWorks.Pages.Products;

public sealed class ProductInput
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 100000)]
    public decimal Price { get; set; }

    [Range(1, int.MaxValue)]
    public int Version { get; set; }
}
```

### Step 2: Implement the PageModel

Create `AdventureWorks/Pages/Products/Edit.cshtml.cs` (58 lines).

Edit.cshtml.cs, part 1 of 2 (27 lines):

```csharp
using AdventureWorks.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdventureWorks.Pages.Products;

public sealed class EditModel(IProductService products)
    : PageModel
{
    [BindProperty]
    public ProductInput Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var product = await products.GetByIdAsync(id);
        if (product is null) return NotFound();
        Input = new ProductInput
        {
            Name = product.Name,
            Price = product.Price,
            Version = product.Version
        };
        return Page();
    }
```

Edit.cshtml.cs, part 2 of 2 (30 lines):

```csharp
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (await products.GetByIdAsync(id) is null)
            return NotFound();
        if (!ModelState.IsValid) return Page();
        var changes = new UpdateProductRequest
        {
            Name = Input.Name,
            Price = Input.Price
        };
        var outcome = await products.UpdateAsync(
            id, changes, Input.Version);
        if (outcome == UpdateOutcome.NotFound) return NotFound();
        if (outcome == UpdateOutcome.Conflict)
        {
            ModelState.AddModelError(string.Empty,
                "This product changed. Reload before saving.");
            return Page();
        }
        StatusMessage = "Product updated.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!await products.DeleteAsync(id)) return NotFound();
        StatusMessage = "Product deleted.";
        return RedirectToPage("./Index");
    }
}
```

### Step 3: Select routes and named handlers

Create `AdventureWorks/Pages/Products/Edit.cshtml` (20 lines).

```razor
@page "{id:int}"
@model AdventureWorks.Pages.Products.EditModel
@{
    ViewData["Title"] = "Edit product";
}
<h1>Edit product</h1>
@if (Model.StatusMessage is not null)
{
    <p role="status">@Model.StatusMessage</p>
}
<div asp-validation-summary="All"></div>
<form method="post">
    <partial name="_ProductForm" for="Input" />
    <button type="submit">Save</button>
</form>
<form method="post" asp-page-handler="Delete"
      asp-route-id="@RouteData.Values["id"]">
    <button type="submit">Delete</button>
</form>
<vc:product-summary></vc:product-summary>
```

Create `AdventureWorks/Pages/Products/Index.cshtml` (20 lines).

```razor
@page
@inject IProductService Products
@{
    ViewData["Title"] = "Products";
    var items = await Products.SearchAsync(null, 1, 20);
}
<h1>Products</h1>
@if (TempData["StatusMessage"] is string message)
{
    <p role="status">@message</p>
}
<ul>
@foreach (var product in items)
{
    <li>
        <a asp-page="./Edit" asp-route-id="@product.Id">
            @product.Name</a> (@product.Sku)
    </li>
}
</ul>
```

### Step 4: Partition with areas

The Admin area exists now and Chapter 9 protects it.

Create `AdventureWorks/Areas/Admin/Pages/Index.cshtml` (4 lines).

```razor
@page
<h1>Administration</h1>
<p>No authentication is configured yet. Chapter 9 protects this
area; keep privileged content out of it until then.</p>
```

### Step 5: Compose layouts and partials

_ViewImports also imports AdventureWorks.Core, so pages can name IProductService.

Create `AdventureWorks/Pages/_ViewImports.cshtml` (4 lines).

```razor
@namespace AdventureWorks.Pages
@using AdventureWorks.Core
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, AdventureWorks
```

Create `AdventureWorks/Pages/_ViewStart.cshtml` (3 lines).

```razor
@{
    Layout = "_Layout";
}
```

Create `AdventureWorks/Pages/Shared/_Layout.cshtml` (16 lines).

```razor
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport"
          content="width=device-width, initial-scale=1" />
    <title>@ViewData["Title"] - AdventureWorks</title>
</head>
<body>
    <header>
        <a asp-page="/Products/Index">AdventureWorks products</a>
    </header>
    <main>@RenderBody()</main>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

Create `AdventureWorks/Pages/Shared/_ProductForm.cshtml` (8 lines).

```razor
@model AdventureWorks.Pages.Products.ProductInput
<input type="hidden" asp-for="Version" />
<label asp-for="Name"></label>
<input asp-for="Name" />
<span asp-validation-for="Name"></span>
<label asp-for="Price"></label>
<input asp-for="Price" />
<span asp-validation-for="Price"></span>
```

### Step 6: Add a View Component

Create `AdventureWorks/ProductSummaryViewComponent.cs` (14 lines).

```csharp
using AdventureWorks.Core;
using Microsoft.AspNetCore.Mvc;

namespace AdventureWorks;

public sealed class ProductSummaryViewComponent(
    IProductService products) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var visible = await products.SearchAsync(null, 1, 20);
        return View(visible.Length);
    }
}
```

Create `AdventureWorks/Pages/Shared/Components/ProductSummary/Default.cshtml` (2 lines).

```razor
@model int
<p>Products on the first catalog page: @Model</p>
```

### Step 7: Configure rendering and form protection

Register Razor Pages and antiforgery after the controllers, put UseAntiforgery after the routing-dependent middleware (Chapters 8 and 9 add authentication and authorization just before it), and map the pages after the APIs.

In `AdventureWorks/Program.cs`, add these lines after the line `options.Filters.Add<RequestLoggingFilter>());`. The anchor is the second line of the AddControllers call.

```csharp
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery();
```

In `AdventureWorks/Program.cs`, add these lines after the line `}));`. The anchor is the last line of the UseWhen branch from Chapter 3.

```csharp
app.UseAntiforgery();
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.MapProductEndpointsV2();`.

```csharp
app.MapRazorPages();
```

## Program.cs at the checkpoint (59 lines)

Program.cs, part 1 of 2 (22 lines):

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
builder.Services.AddControllers(options =>
    options.Filters.Add<RequestLoggingFilter>());
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery();
```

Program.cs, part 2 of 2 (36 lines):

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
app.UseAntiforgery();
app.MapStatusEndpoints();
app.MapOrderEndpoints();
app.MapCatalogEndpoints();
app.MapControllers();
app.MapProductEndpointsV2();
app.MapRazorPages();
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

`requests.http` holds these requests in this order; 10 of 10 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `GET /Products` returned **200**

  ```text
  (an HTML page)
  ```

- `GET /Products/Edit/1` returned **200**

  ```text
  (an HTML page)
  ```

- `GET /Products/Edit/not-an-int` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-58de8b19d11da921eb03b38927a1e9f5-aba2be035c22890a-02","correlationId":"bfa0523b2d6b45c6a43b64df51c7c3e8"}
  ```

- `GET /Products/Edit/999` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-b1189ebd0e1b7a4848d60743c4d0ee66-8f6fcaaf3345fb3a-02","correlationId":"32dd1268ca5141838ab6358ac426ec4e"}
  ```

- `GET /Admin` returned **200**

  ```text
  <h1>Administration</h1>
  <p>No authentication is configured yet. Chapter 9 protects this
  area; keep privileged content out of it until then.</p>
  ```

- `POST /Products/Edit/1` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,"traceId":"00-82546a1f2f62e5e2726742e968a4e48f-20952c29f4cbbf06-02","correlationId":"0900d82228354dec82e7eef365bcdaf1"}
  ```

- `GET /api/v2/products/1` returned **200**

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1}
  ```

- `GET /api/catalog` returned **200**

  ```text
  {"items":[{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":...
  ```

- `GET /api/orders/1` returned **200**

  ```text
  {"id":1,"orderNumber":"AW-0001","total":49.00}
  ```

- `GET /ping` returned **200**

  ```text
  pong
  ```

### Other runs

A Python check fetched the real forms and their antiforgery tokens (a .http file cannot read a token from HTML), with these results, all as expected:

- `/Products/Edit/1` renders `Input.Version` = 1; a two-letter name redisplays the page (200) with a field validation error.
- A valid save returns 302 to `/Products/Edit/1`; the next GET shows "Product updated." from TempData.
- `/api/v2/products/1` then reports version 2 and the new name.
- Posting the same form again (still version 1) returns 200 with "This product changed. Reload before saving."
- `/Products` lists the edited name; the named Delete handler returns 302 to `/Products`, and `/api/v2/products/1` returns 404.
- A POST with no cookie and token returns 400 before any handler runs.

The README's PowerShell section repeats the same flow with curl.exe (statuses 200, 302, 200, 200, 302, 404), and it passed when run as printed in Windows PowerShell 5.1.

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks/Pages/Products/Edit.cshtml.cs` on line 41:

```csharp
if (outcome == UpdateOutcome.NotFound) return NotFound();
```

Press F5 (the https profile), then send POST /Products/Edit/1 twice from the same form (the README's PowerShell flow, or two browser tabs): the first save, then the stale one. When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
id = 1 [Int32]
Input.Name = Trail Helmet II [String]
Input.Version = 1 [Int32]
outcome = Updated [UpdateOutcome]
id = 1 [Int32]
Input.Name = Trail Helmet II [String]
Input.Version = 1 [Int32]
outcome = Conflict [UpdateOutcome]
```

The breakpoint is hit once per save that passes validation (the two-letter name never gets here). On the first save outcome is Updated; on the second, the form still carries Version 1 while the store is at 2, so outcome is Conflict.

## What the Activity adds

A customer profile page over one fixed, fictional customer: `CustomerProfileStore` (System.Threading.Lock; Task-returning read, save and deactivate), `Pages/Customer/Profile` with `ProfileInput` (Name Required, StringLength 100; Email Required, EmailAddress), Post-Redirect-Get with a TempData message, a named `OnPostDeactivateAsync` handler that reads none of the edit fields and redirects to `/Customer/Deactivated`, and a `RecentOrders` View Component that lists Core `Order` records (AW-0001, AW-0002) rather than a new order type. Checked with the same form script: the page renders the recent orders; an invalid email redisplays the form; a valid save redirects, shows "Profile saved." once, then not again; Deactivate needs only the token and the confirmation page shows "Active: False"; a POST without cookie and token returns 400.

The reader-path check rebuilt `AdventureWorks/Chapter06-Activity` from the Chapter 6 checkpoint: no differences.
The Activity adds 135 lines of code: 134 in 6 new files and 1 new or changed in existing files, plus 0 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

### Activity: a customer profile page

Work in a copy of the Chapter 6 checkpoint folder. Chapter 7 starts from the checkpoint. The customer is fictional local data; Chapter 8 replaces it with the signed-in user. Recent orders reuse Core's Order record rather than a new order type.

Create `AdventureWorks/CustomerProfileStore.cs` (41 lines).

CustomerProfileStore.cs, part 1 of 2 (23 lines):

```csharp
using AdventureWorks.Core;
using AdventureWorks.Pages.Customer;

namespace AdventureWorks;

// Fictional local data for the Activity: one fixed customer.
// Real profile pages read the signed-in user (Chapter 8).
public sealed class CustomerProfileStore
{
    private readonly Lock _gate = new();
    private string _name = "Example Customer";
    private string _email = "customer@example.com";
    private bool _active = true;

    public Task<ProfileInput> ReadAsync()
    {
        lock (_gate)
            return Task.FromResult(new ProfileInput
            {
                Name = _name,
                Email = _email
            });
    }
```

CustomerProfileStore.cs, part 2 of 2 (17 lines):

```csharp
    public Task SaveAsync(ProfileInput input)
    {
        lock (_gate) (_name, _email) = (input.Name, input.Email);
        return Task.CompletedTask;
    }

    public Task DeactivateAsync()
    {
        lock (_gate) _active = false;
        return Task.CompletedTask;
    }

    public bool IsActive { get { lock (_gate) return _active; } }

    public Order[] RecentOrders() =>
        [new(1, "AW-0001", 49.00m), new(2, "AW-0002", 189.00m)];
}
```

Create `AdventureWorks/RecentOrdersViewComponent.cs` (10 lines).

```csharp
using Microsoft.AspNetCore.Mvc;

namespace AdventureWorks;

public sealed class RecentOrdersViewComponent(
    CustomerProfileStore store) : ViewComponent
{
    public IViewComponentResult Invoke() =>
        View(store.RecentOrders().Take(5).ToArray());
}
```

Create `AdventureWorks/Pages/Customer/Profile.cshtml.cs` (42 lines).

Profile.cshtml.cs, part 1 of 2 (20 lines):

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdventureWorks.Pages.Customer;

public sealed class ProfileInput
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public sealed class ProfileModel(CustomerProfileStore store)
    : PageModel
{
    [BindProperty]
    public ProfileInput Input { get; set; } = new();
```

Profile.cshtml.cs, part 2 of 2 (21 lines):

```csharp
    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync() =>
        Input = await store.ReadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await store.SaveAsync(Input);
        StatusMessage = "Profile saved.";
        return RedirectToPage();
    }

    // A named handler: it reads none of the edit form's fields.
    public async Task<IActionResult> OnPostDeactivateAsync()
    {
        await store.DeactivateAsync();
        return RedirectToPage("./Deactivated");
    }
}
```

Create `AdventureWorks/Pages/Customer/Profile.cshtml` (24 lines).

```razor
@page
@model AdventureWorks.Pages.Customer.ProfileModel
@{
    ViewData["Title"] = "Profile";
}
<h1>Customer profile</h1>
@if (Model.StatusMessage is not null)
{
    <p role="status">@Model.StatusMessage</p>
}
<div asp-validation-summary="All"></div>
<form method="post">
    <label asp-for="Input.Name"></label>
    <input asp-for="Input.Name" />
    <span asp-validation-for="Input.Name"></span>
    <label asp-for="Input.Email"></label>
    <input asp-for="Input.Email" />
    <span asp-validation-for="Input.Email"></span>
    <button type="submit">Save</button>
</form>
<form method="post" asp-page-handler="Deactivate">
    <button type="submit">Deactivate</button>
</form>
<vc:recent-orders></vc:recent-orders>
```

Create `AdventureWorks/Pages/Customer/Deactivated.cshtml` (7 lines).

```razor
@page
@inject CustomerProfileStore Store
@{
    ViewData["Title"] = "Profile deactivated";
}
<h1>Profile deactivated</h1>
<p>Active: @Store.IsActive</p>
```

Create `AdventureWorks/Pages/Shared/Components/RecentOrders/Default.cshtml` (10 lines).

```razor
@model Order[]
<table>
    <thead><tr><th>Order</th><th>Total</th></tr></thead>
    <tbody>
    @foreach (var order in Model)
    {
        <tr><td>@order.OrderNumber</td><td>@order.Total</td></tr>
    }
    </tbody>
</table>
```

In `AdventureWorks/Program.cs`, add these lines after the line `builder.Services.AddAntiforgery();`.

```csharp
builder.Services.AddSingleton<CustomerProfileStore>();
```

### Activity requests and responses

13 of 13 requests in `Chapter06-Activity/requests.http` returned their commented status. The responses for the requests the Activity adds:

- `GET /Customer/Profile` returned **200**

  ```text
  (an HTML page)
  ```

- `POST /Customer/Profile?handler=Deactivate` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,"traceId":"00-d4dfa60a6c6bfafe3d1e9c684e296f45-f0384dfb4e7521ec-02","correlationId":"6835e6ee0c164ca79c9380a9fa6138b7"}
  ```

- `GET /Customer/Deactivated` returned **200**

  ```text
  (an HTML page)
  ```

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| Step 1 ProductStore.cs (41 lines) | Replaced | ProductItem and ProductStore are removed: the pages use Core's Product and IProductService. ProductInput moves to Pages/Products/ProductInput.cs. The version check becomes the UpdateAsync overload added to Core and Infrastructure. |
| Step 2 Edit.cshtml.cs | Replaced | IProductService and UpdateOutcome; the conflict message is "This product changed. Reload before saving." (fits 66 characters); 58 lines, two parts. |
| Step 3 Edit.cshtml | Replaced | The Delete form's attributes are on two lines. |
| Step 4 Areas/Admin/Pages/Index.cshtml | Replaced | Text says Chapter 9 protects the area. |
| Step 5 _ViewImports.cshtml | Replaced | Adds `@using AdventureWorks.Core`. |
| Step 5 _ViewStart.cshtml | Kept | Identical. |
| Step 5 _Layout.cshtml | Replaced | The viewport meta and header link break for width; the title adds " - AdventureWorks". |
| Step 5 _ProductForm.cshtml | Replaced | `@model AdventureWorks.Pages.Products.ProductInput`. |
| Step 6 ProductSummaryViewComponent.cs and Default.cshtml | Replaced | IProductService; namespace AdventureWorks; the text reads "Products on the first catalog page". |
| Full Example Index.cshtml | Replaced | Injects IProductService and shows each SKU. |
| Full Example repeats | Removed | Each file is printed once. |
| Full Example Program.cs | Replaced | Adds four lines to Chapter 5's; 59 lines. |

## Notes for the writers

- Concept Overview line 25 ("its Program.cs replaces Chapter 5's, and ProductStore takes the place of Chapter 5's ProductService") is wrong now; say the pages share Chapter 4's store.
- Performance section: "Chapter 10 shows database constraints and concurrency tokens in a separate project" becomes "Chapter 10 moves this store to EF Core behind the same interface".
- Full Example run text: after the delete, `/Products` lists the three remaining products, not an empty list.
- `UseAntiforgery` sits after the `/api` UseWhen branch now; the design's order puts authentication and authorization (Chapters 8 and 9) between routing and antiforgery.
- `ProductInput` uses `[Range(0.01, 100000)]` for the same culture reason as Chapter 5.
- requests.http covers only token-free requests (pages, 404s, a 400 POST without a token); the README adds the PowerShell form flow, and the browser remains the natural tool for the forms.
