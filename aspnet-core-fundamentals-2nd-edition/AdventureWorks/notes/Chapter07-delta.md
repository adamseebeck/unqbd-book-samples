# Chapter 7 delta: Forms, Model Binding, and Validation

Generated October 30, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter07` and the operations that rebuild it from the Chapter 6 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 7 adds order input to the running app instead of a separate Chapter07Sample project. Everything from Chapters 1 to 6 stays.

- `OrderInputs.cs` (Web): `OrderFormInput` (ProductId Range 1 and up, Quantity Range 1 to 20, Email Required and EmailAddress) for the page, and `OrderApiInput` (Sku Required, Chapter 5's `^[A-Z]{3}-[0-9]{4}$` pattern and `[AvailableSku]`; Quantity Range 1 to 20) for the API. Neither accepts a price or an owner.
- The asynchronous check asks the real catalog: `IProductService.GetBySkuAsync(sku, cancellationToken)` (Core and the in-memory store), `CatalogAvailability` (registered in AddCatalog) and `AvailableSkuAttribute`, an AsyncValidationAttribute with a culture-aware FormatMessage. Available SKUs are the catalog's: HLM-1001, JRS-2001, BIK-3001, TNT-4001, plus any product added through the API.
- `AddLocalization`, `AddValidation` and a 1 MiB multipart limit; `UseStaticFiles` and `UseRequestLocalization("en", "fr")` after the domain-exception middleware and before routing. A request in French gets French number formatting in validation messages ("0,01").
- `POST /api/orders/validate` in the orders group, validated by the built-in Minimal API validation.
- Because AddValidation covers every Minimal API endpoint, Chapter 5's v2 product endpoints are now validated by the framework; their manual `ValidationFilter<T>` is removed and ValidationFilter.cs is deleted. The 400 responses keep the same shape.
- `/Orders/Create`: a Razor Page with `[RequestSizeLimit]`, ModelState errors, a lookup that the product exists, and an optional upload saved under a generated `.bin` name in `PrivateUploads` under the content root, never in wwwroot.
- `wwwroot/css/site.css`, linked from the layout, is the app's first public static file; the layout also links the new page.

## Files added or changed

| File | Change |
| --- | --- |
| `AdventureWorks/OrderInputs.cs` | added |
| `AdventureWorks.Core/Products.cs` | changed |
| `AdventureWorks.Infrastructure/InMemoryProductService.cs` | changed |
| `AdventureWorks/CatalogAvailability.cs` | added |
| `AdventureWorks/CatalogEndpoints.cs` | changed |
| `AdventureWorks/AvailableSkuAttribute.cs` | added |
| `AdventureWorks/Program.cs` | changed |
| `AdventureWorks/OrderEndpoints.cs` | changed |
| `AdventureWorks/ProductEndpointsV2.cs` | changed |
| `AdventureWorks/ValidationFilter.cs` | deleted |
| `AdventureWorks/wwwroot/css/site.css` | added |
| `AdventureWorks/Pages/Shared/_Layout.cshtml` | replaced |
| `AdventureWorks/Pages/Orders/Create.cshtml.cs` | added |
| `AdventureWorks/Pages/Orders/Create.cshtml` | added |

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 45 of 45 files identical and no differences.

### Step 1: The input models

The order form and the order API each get a dedicated input model. Neither accepts a price or an owner: the server looks those up. The API input reuses Chapter 5's SKU pattern, so a SKU means the same thing everywhere in the app.

Create `AdventureWorks/OrderInputs.cs` (26 lines).

```csharp
using System.ComponentModel.DataAnnotations;

namespace AdventureWorks;

// What a caller may submit. Price and owner never come from here.
public sealed class OrderFormInput
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 20)]
    public int Quantity { get; set; } = 1;

    [Required, EmailAddress]
    public string Email { get; set; } = "";
}

public sealed class OrderApiInput
{
    [Required, RegularExpression(@"^[A-Z]{3}-[0-9]{4}$")]
    [AvailableSku]
    public string Sku { get; set; } = "";

    [Range(1, 20)]
    public int Quantity { get; set; } = 1;
}
```

### Step 2: Define the asynchronous check before using it

The availability check asks the real product store, so the store gains a SKU lookup that accepts a cancellation token. CatalogAvailability is the boundary the attribute calls; the in-memory store answers with a completed task, and Chapter 10's EF Core store awaits a real query through the same method.

In `AdventureWorks.Core/Products.cs`, add these lines after the line `Task<Product?> GetByIdAsync(int id);`.

```csharp
    Task<Product?> GetBySkuAsync(string sku,
        CancellationToken cancellationToken = default);
```

In `AdventureWorks.Infrastructure/InMemoryProductService.cs`, add these lines above the line `public Task<Product[]> SearchAsync(`. Below GetByIdAsync. The comparison is ordinal, like the SKU pattern.

```csharp
    public Task<Product?> GetBySkuAsync(string sku,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_products.Values
            .FirstOrDefault(p => p.Sku == sku));
    }
```

Create `AdventureWorks/CatalogAvailability.cs` (13 lines).

```csharp
using AdventureWorks.Core;

namespace AdventureWorks;

// The asynchronous check behind AvailableSkuAttribute. It asks
// the store the catalog uses and passes cancellation on.
public sealed class CatalogAvailability(IProductService products)
{
    public async Task<bool> IsAvailableAsync(
        string sku, CancellationToken cancellationToken) =>
        await products.GetBySkuAsync(sku, cancellationToken)
            is not null;
}
```

In `AdventureWorks/CatalogEndpoints.cs`, add these lines after the line `InMemoryProductService>();`. Inside AddCatalog, after the store registration.

```csharp
        services.AddSingleton<CatalogAvailability>();
```

Create `AdventureWorks/AvailableSkuAttribute.cs` (41 lines).

AvailableSkuAttribute.cs, part 1 of 2 (17 lines):

```csharp
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AdventureWorks;

public sealed class AvailableSkuAttribute
    : AsyncValidationAttribute
{
    public AvailableSkuAttribute() =>
        ErrorMessage = "{0} is unavailable in {1}.";

    public string CatalogName { get; set; } = "the catalog";

    public override string FormatMessage(
        string format, string name) =>
        string.Format(CultureInfo.CurrentCulture,
            format, name, CatalogName);
```

AvailableSkuAttribute.cs, part 2 of 2 (23 lines):

```csharp
    protected override async Task<ValidationResult?> IsValidAsync(
        object? value, ValidationContext context,
        CancellationToken cancellationToken)
    {
        if (value is not string sku ||
            string.IsNullOrWhiteSpace(sku))
            return ValidationResult.Success;
        var catalog =
            context.GetRequiredService<CatalogAvailability>();
        if (await catalog.IsAvailableAsync(
                sku, cancellationToken))
            return ValidationResult.Success;
        string[] members = context.MemberName is null
            ? [] : [context.MemberName];
        return new ValidationResult(
            FormatErrorMessage(context.DisplayName), members);
    }

    protected override ValidationResult? IsValid(
        object? value, ValidationContext context) =>
        throw new InvalidOperationException(
            "Use the asynchronous validation path.");
}
```

### Step 3: Configure Minimal API validation

AddValidation turns on the built-in validation for every Minimal API endpoint, so the order API needs no filter, and Chapter 5's v2 product endpoints get the same validation. Their manual ValidationFilter<T> would now validate a second time, so remove it and delete ValidationFilter.cs; the 400 responses are unchanged. AddLocalization comes first so that validation messages are formatted in the request culture, and the multipart limit bounds every form body.

In `AdventureWorks/Program.cs`, add these lines after the line `using AdventureWorks;`.

```csharp
using Microsoft.AspNetCore.Http.Features;
```

In `AdventureWorks/Program.cs`, add these lines after the line `builder.Services.AddAntiforgery();`.

```csharp
builder.Services.AddLocalization();
builder.Services.AddValidation();
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = 1024 * 1024);
```

In `AdventureWorks/OrderEndpoints.cs`, add these lines after the line `.WithName("GetOrderById");`. Inside MapOrderEndpoints, before `return app;`.

```csharp
        orders.MapPost("/validate", (OrderApiInput input) =>
            TypedResults.Ok(new { input.Sku, input.Quantity }));
```

In `AdventureWorks/ProductEndpointsV2.cs`, replace these lines. Inside MapProductEndpointsV2.

```csharp
        group.MapPost("/", CreateProduct)
            .AddEndpointFilter<
                ValidationFilter<CreateProductRequest>>()
            .ProducesValidationProblem();
        group.MapPut("/{id:int}", UpdateProduct)
            .AddEndpointFilter<
                ValidationFilter<UpdateProductRequest>>()
            .ProducesValidationProblem();
```

With these:

```csharp
        group.MapPost("/", CreateProduct)
            .ProducesValidationProblem();
        group.MapPut("/{id:int}", UpdateProduct)
            .ProducesValidationProblem();
```

Delete `AdventureWorks/ValidationFilter.cs`. AddValidation replaces it.

### Step 3 (continued): Static files and the request culture

Put both after the domain-exception middleware and before routing, in the design's order. The one-line UseRequestLocalization overload makes the first culture the default and supports both. A French request formats validation numbers the French way, for example "0,01".

In `AdventureWorks/Program.cs`, add these lines after the line `app.UseMiddleware<DomainExceptionMiddleware>();`.

```csharp
app.UseStaticFiles();
app.UseRequestLocalization("en", "fr");
```

Create `AdventureWorks/wwwroot/css/site.css` (6 lines). A public static file, so the contrast with private uploads is visible.

```text
/* A public static file. Uploads never go under wwwroot. */
body { font-family: system-ui, sans-serif; margin: 2rem; }
header { margin-bottom: 1rem; }
label { display: block; margin-top: 0.5rem; }
.field-validation-error,
.validation-summary-errors { color: #b00020; }
```

Replace the whole of `AdventureWorks/Pages/Shared/_Layout.cshtml` with the following. Links the stylesheet and the new page.

```razor
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport"
          content="width=device-width, initial-scale=1" />
    <title>@ViewData["Title"] - AdventureWorks</title>
    <link rel="stylesheet" href="~/css/site.css" />
</head>
<body>
    <header>
        <a asp-page="/Products/Index">AdventureWorks products</a>
        <a asp-page="/Orders/Create">Check an order</a>
    </header>
    <main>@RenderBody()</main>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

### Steps 4 and 5: Display form errors and bound the upload

The page validates its input with ModelState, checks the product exists in the store, and saves an optional upload under a generated name outside wwwroot.

Create `AdventureWorks/Pages/Orders/Create.cshtml.cs` (65 lines).

Create.cshtml.cs, part 1 of 2 (40 lines):

```csharp
using AdventureWorks.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdventureWorks.Pages.Orders;

[RequestSizeLimit(2 * 1024 * 1024)]
public sealed class CreateModel(IProductService products,
    IWebHostEnvironment environment) : PageModel
{
    const long MaxUploadBytes = 1024 * 1024;

    [BindProperty]
    public OrderFormInput Input { get; set; } = new();

    [BindProperty]
    public IFormFile? Upload { get; set; }

    public string? StatusMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Upload is { Length: 0 or > MaxUploadBytes })
            ModelState.AddModelError(nameof(Upload),
                "Choose a nonempty file up to 1 MiB.");
        var product =
            await products.GetByIdAsync(Input.ProductId);
        if (product is null)
            ModelState.AddModelError("Input.ProductId",
                "No product has that ID.");
        if (!ModelState.IsValid) return Page();
        if (Upload is not null)
            await SavePrivateCopyAsync(Upload);
        StatusMessage = "Input validated for " +
            $"{Input.Quantity} x {product!.Name}. " +
            "No order has been placed.";
        return Page();
    }
```

Create.cshtml.cs, part 2 of 2 (24 lines):

```csharp
    async Task SavePrivateCopyAsync(IFormFile upload)
    {
        var directory = Path.Combine(
            environment.ContentRootPath, "PrivateUploads");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(
            directory, Guid.NewGuid().ToString("N") + ".bin");
        var created = false;
        try
        {
            await using var output = new FileStream(path,
                FileMode.CreateNew, FileAccess.Write,
                FileShare.None);
            created = true;
            await upload.CopyToAsync(
                output, HttpContext.RequestAborted);
        }
        catch
        {
            if (created) System.IO.File.Delete(path);
            throw;
        }
    }
}
```

Create `AdventureWorks/Pages/Orders/Create.cshtml` (22 lines).

```razor
@page
@model AdventureWorks.Pages.Orders.CreateModel
@{
    ViewData["Title"] = "Check an order";
}
<h1>Check an order</h1>
@if (Model.StatusMessage is not null)
{
    <p role="status">@Model.StatusMessage</p>
}
<form method="post" enctype="multipart/form-data">
    <div asp-validation-summary="All"></div>
    <label asp-for="Input.ProductId"></label>
    <input asp-for="Input.ProductId" />
    <label asp-for="Input.Quantity"></label>
    <input asp-for="Input.Quantity" />
    <label asp-for="Input.Email"></label>
    <input asp-for="Input.Email" />
    <label asp-for="Upload"></label>
    <input asp-for="Upload" type="file" />
    <button type="submit">Validate</button>
</form>
```

## Program.cs at the checkpoint (66 lines)

Program.cs, part 1 of 2 (27 lines):

```csharp
using AdventureWorks;
using Microsoft.AspNetCore.Http.Features;

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
builder.Services.AddLocalization();
builder.Services.AddValidation();
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = 1024 * 1024);
```

Program.cs, part 2 of 2 (38 lines):

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

`requests.http` holds these requests in this order; 14 of 14 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `POST /api/orders/validate` returned **200**

  ```text
  {"sku":"HLM-1001","quantity":2}
  ```

- `POST /api/orders/validate` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Sku":["Sku is unavailable in the catalog."]},"traceId":"00-08bb60641d176ca7d0a0f191d9...
  ```

- `POST /api/orders/validate` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Sku":["The field Sku must match the regular expression '^[A-Z]{3}-[0-9]{4}$'."]},"tra...
  ```

- `POST /api/orders/validate` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Quantity":["The field Quantity must be between 1 and 20."]},"traceId":"00-3def1aab405...
  ```

- `POST /api/orders/validate` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Sku":["The Sku field is required."]},"traceId":"00-6ab75ecd00a3c51dbab3f88b9e933075-e...
  ```

- `POST /api/v2/products` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Category":["Unknown category."]},"traceId":"00-5444af2103fb4a84f778db02de3e0dfa-8e3bd...
  ```

- `POST /api/v2/products` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Price":["The field Price must be between 0,01 and 100000."]},"traceId":"00-71a1801688...
  ```

- `POST /api/v2/products` returned **201** (Location: https://localhost:7001/api/v2/products/5)

  ```text
  {"id":5,"name":"Trail Bottle","sku":"BTL-5001","price":24.99,"category":"accessories","version":1}
  ```

- `GET /Orders/Create` returned **200**

  ```text
  (an HTML page)
  ```

- `POST /Orders/Create` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,"traceId":"00-12585ffe881a03c491ba20c7dc210355-fa1057ebfef4785b-02","correlationId":"1f1b703b3c3a4d35859dcbd7ddc57c63"}
  ```

- `GET /css/site.css` returned **200**

  ```text
  /* A public static file. Uploads never go under wwwroot. */
  body { font-family: system-ui, sans-serif; margin: 2rem; }
  header { margin-bottom: 1rem; }
  label { display: block; margin-top: 0.5rem; }
  .field-validation-error...
  ```

- `GET /PrivateUploads/example.bin` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-2dca0c6ab037874e117a9026fb76745a-7351bf8a9d83b019-02","correlationId":"7d84af83e2824b1aac45833db7d417f9"}
  ```

- `GET /api/catalog/1` returned **200**

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","reviewsLink":"/api/catalog/1/reviews"}
  ```

- `GET /Products` returned **200**

  ```text
  (an HTML page)
  ```

### Other runs

A Python check fetched the real form and its token, with these results:

- Quantity 50 and email "not-an-email" redisplay the page (200) with both field errors.
- Product 99 gives "No product has that ID."
- Product 1, quantity 2 and a 17-byte upload named `..\..\evil name.txt` return "Input validated for 2 x Trail Helmet. No order has been placed."; the file is stored as a 32-hex-character `.bin` name in PrivateUploads with the uploaded bytes, and `/PrivateUploads/<that name>` returns 404.
- A 1.5 MiB upload returns 400 (over the 1 MiB multipart limit).
- A POST without the cookie and token returns 400.
The check deletes its test upload afterward, as the Activity's last step asks.

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks/AvailableSkuAttribute.cs` on line 28:

```csharp
if (await catalog.IsAvailableAsync(
```

Press F5 (the https profile), then send POST https://localhost:7001/api/orders/validate with {"sku": "PEN-2020", "quantity": 1}. When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
sku = PEN-2020 [String]
context.MemberName = Sku [String]
context.DisplayName = Sku [String]
context.ObjectType.Name = OrderApiInput [String]
cancellationToken.CanBeCanceled = True [Boolean]
```

The SKU has already passed Required and the pattern; this asynchronous check runs inside AddValidation before the endpoint handler. The ValidationContext names the member and its owner, and the cancellation token is the request's, so it can be canceled. Step Over: IsAvailableAsync returns false and the next line builds the error that becomes the 400.

## What the Activity adds

The Activity copy adds a separate seven-line host, `HeaderOnlyForms` (its own project in the solution, on http://localhost:5107), so the .NET 11 header-only CSRF check can be seen without the store's token antiforgery taking precedence. It maps one form-bound Minimal API endpoint, `POST /newsletter`, and a CORS policy that trusts the store's origin, https://localhost:7001, with credentials. Checked on RC3: a same-origin post returns 200; a cross-site post from https://evil.example returns 400 from the header check alone; the same cross-site post from the trusted credentialed origin returns 200; and cross-site metadata on the store's JSON route `/api/orders/validate` returns 200, because the form defense does not apply to JSON. This answers master findings C07-1 and C07-2 (CT): the book can now show the host and the exact requests. The other Activity bullets (invalid and valid form values, removing the token, the JSON cases, the upload and its cleanup) run against the checkpoint itself and are covered by its requests.http and the form check above.

In Visual Studio 2026, set both projects to start: right-click the solution, Configure Startup Projects, Multiple startup projects.

The reader-path check rebuilt `AdventureWorks/Chapter07-Activity` from the Chapter 7 checkpoint: no differences.

### Activity: a header-only form host for the CSRF checks

The checkpoint uses token antiforgery, which takes precedence over .NET 11's automatic Sec-Fetch-Site and Origin check. To see the header check on its own, the Activity copy adds a separate seven-line host with no token middleware: one form-bound Minimal API endpoint, and a CORS policy that trusts one credentialed origin. Work in a copy of the Chapter 7 checkpoint; Chapter 8 starts from the checkpoint.

Run this from the working folder.

```shell
dotnet new web --name HeaderOnlyForms --framework net11.0
```

It prints:

```text
The template "ASP.NET Core Empty" was created successfully.
Processing post-creation actions...
Restoring HeaderOnlyForms\HeaderOnlyForms.csproj:
  Determining projects to restore...
  Restored HeaderOnlyForms\HeaderOnlyForms.csproj (in 133 ms).
Restore succeeded.
```

Run this from the working folder.

```shell
dotnet sln add HeaderOnlyForms
```

It prints:

```text
Project `HeaderOnlyForms\HeaderOnlyForms.csproj` added to the solution.
```

Replace the whole of `HeaderOnlyForms/Program.cs` with the following.

```csharp
using Microsoft.AspNetCore.Mvc;

// No token middleware: only the .NET 11 Sec-Fetch-Site and Origin
// check protects this form endpoint.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options => options.AddPolicy("store",
    policy => policy.WithOrigins("https://localhost:7001")
        .AllowCredentials().AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseCors();
app.MapPost("/newsletter", ([FromForm] string email) =>
    Results.Ok(new { email })).RequireCors("store");
app.Run();
```

Replace the whole of `HeaderOnlyForms/Properties/launchSettings.json` with the following. One profile on http://localhost:5107, so it runs beside the store app.

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5107",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Activity requests and responses

18 of 18 requests in `Chapter07-Activity/requests.http` returned their commented status. The responses for the requests the Activity adds:

- `POST http://localhost:5107/newsletter` returned **200**

  ```text
  {"email":"ada@example.com"}
  ```

- `POST http://localhost:5107/newsletter` returned **400**

  ```text
  Microsoft.AspNetCore.Http.BadHttpRequestException: Antiforgery validation failed when reading parameter "string email" from the request body as form.
   ---> Microsoft.AspNetCore.Antiforgery.CsrfValidationException: Cross-...
  ```

- `POST http://localhost:5107/newsletter` returned **200**

  ```text
  {"email":"ada@example.com"}
  ```

- `POST /api/orders/validate` returned **200**

  ```text
  {"sku":"HLM-1001","quantity":1}
  ```

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| Step 1 `dotnet new webapp` and `cd Chapter07Sample` | Removed | No new project: Chapter 7 continues the AdventureWorks app. |
| Step 1 OrderFormInput | Replaced | In OrderInputs.cs, namespace AdventureWorks. |
| Step 2 CatalogAvailability.cs | Replaced | Delegates to the new `IProductService.GetBySkuAsync`; NOTEBOOK-101 is gone (it fails the app's SKU pattern). |
| Step 2 AvailableSkuAttribute.cs | Replaced | Same logic; namespace AdventureWorks; CatalogName "the catalog"; lines broken to fit 66 characters. |
| Step 3 OrderApiInput | Replaced | Adds Chapter 5's SKU RegularExpression ahead of AvailableSku. |
| Step 3 registration and MapPost excerpt | Replaced | CatalogAvailability is registered in AddCatalog; AddValidation and AddLocalization go in Program.cs after AddAntiforgery; the route is `orders.MapPost("/validate", ...)` inside MapOrderEndpoints. |
| Step 4 Create.cshtml.cs | Replaced | Takes IProductService and checks the product exists; the upload code moves into a SavePrivateCopyAsync helper; 61 lines, two parts. |
| Step 4 form excerpt | Replaced | Name the file, Pages/Orders/Create.cshtml, and show its @page and @model lines (master finding C07-4). |
| Full Example Program.cs | Replaced | Adds one using, four registration lines and two pipeline lines to Chapter 6's; 66 lines. The 139-character UseRequestLocalization line becomes `app.UseRequestLocalization("en", "fr");`. |
| Full Example model and page repeats | Removed | Printed once, in the steps. |

## Notes for the writers

- Concept Overview line 20 ("Chapter07Sample is an isolated checkpoint ...") is wrong now: the chapter extends the store app.
- Master finding C07-3 suggests removing AddLocalization and the French culture because there are no resource files. The design (decision 12) keeps the French culture, so the code keeps both and the chapter can show a real effect: a request with `Accept-Language: fr` formats the Range limits as "0,01" (checked). Translating message text still needs resources, as the chapter says.
- Accuracy claim 7.1 (Step 3): do not say that a fixed ValidationResult string bypasses localization. When localization is active and a localized template resolves for the attribute (its ErrorMessage key, or a conventional key such as {Type}_{Member}_{Attribute}_Error), the framework replaces the returned message with that template, formatted through the attribute's FormatMessage. AvailableSkuAttribute builds its message with FormatErrorMessage, so its {0} and {1} formatting applies either way. This app registers the resource-file localizer and ships no resource files, so no template resolves and a French request gets the English text: `Accept-Language: fr` with ZZZ-9999 returned "Sku is unavailable in the catalog." and "The field Quantity must be between 1 and 20." (checked on RC3).
- Removing Chapter 5's ValidationFilter<T> is a deliberate change: with AddValidation registered, every Minimal API parameter with validation attributes is validated by the framework (checked: the lowercase SKU and the unknown category still return 400 with the same `errors` keys). Say so in Step 3, and keep Chapter 5's filter lesson as the manual path that AddValidation replaces.
- Every validation 400 carries `traceId` and Chapter 3's `correlationId`.
- PrivateUploads is created under the AdventureWorks project folder at run time; the companion does not ship it.
- AddValidation needs no attribute on the request types and works for Core's types (checked); the source generator finds them through the endpoint parameters.
