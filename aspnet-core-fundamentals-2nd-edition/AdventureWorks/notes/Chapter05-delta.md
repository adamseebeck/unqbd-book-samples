# Chapter 5 delta: Controllers and Minimal APIs

Generated October 30, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter05` and the operations that rebuild it from the Chapter 4 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 5 adds the product management API in two styles over the one `IProductService` from Chapter 4. They are two versions of the same API, one per style: `/api/v1/products` as an MVC controller and `/api/v2/products` as Minimal APIs. Chapter 14 documents them as versions. This wording replaces "not an API-versioning library".

- Core: `CreateProductRequest` (Name Required, 3 to 100; Sku Required, `^[A-Z]{3}-[0-9]{4}$`; Price Range 0.01 to 100000; Category Required, and an IValidatableObject rule that the category is in ProductCategories.All) and `UpdateProductRequest` (Name, Price). IProductService gains CreateAsync, UpdateAsync and DeleteAsync.
- Infrastructure: InMemoryProductService implements them. New IDs continue after the seed (5, 6, ...), and every update raises Version, which Chapter 6's page checks.
- Web: `ValidationFilter<T>`, `RequestLoggingFilter` (MVC action filter, registered globally) and `RequestLoggingEndpointFilter`; `Controllers/ProductsController.cs` (named route GetProductV1); `ProductEndpointsV2.cs` with `SearchParameters` and typed-result handlers (named route GetProductV2).
- Both versions return Core's `Product`; there is no separate ProductDto. A product created through v1 is visible through v2 and through Chapter 4's `/api/catalog`.
- The route, query and header binding demonstration is Development only, at `/examples/binding/{id:int}`.

## Files added or changed

| File | Change |
| --- | --- |
| `AdventureWorks.Core/ProductRequests.cs` | added |
| `AdventureWorks.Core/Products.cs` | changed |
| `AdventureWorks.Infrastructure/InMemoryProductService.cs` | changed |
| `AdventureWorks/ValidationFilter.cs` | added |
| `AdventureWorks/LoggingFilters.cs` | added |
| `AdventureWorks/Controllers/ProductsController.cs` | added |
| `AdventureWorks/Program.cs` | changed |
| `AdventureWorks/ProductEndpointsV2.cs` | added |
| `AdventureWorks/DevelopmentEndpoints.cs` | changed |

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 29 of 29 files identical and no differences.

### Step 1: Define the product contracts and extend the store

Chapter 4's Product, IProductService and in-memory store already exist, so this step adds the write side. The request types use only DataAnnotations, so they go in Core next to the service contract. No ProductDto is needed: both API styles return Core's Product.

Create `AdventureWorks.Core/ProductRequests.cs` (35 lines). The object-level rule checks the category against Core's ProductCategories list, the same list the Chapter 4 route constraint reads. The decimal ranges use the double overload, which needs no string parsing and so behaves the same in every culture (Chapter 7 adds French).

```csharp
using System.ComponentModel.DataAnnotations;

namespace AdventureWorks.Core;

public sealed class CreateProductRequest : IValidatableObject
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required, RegularExpression(@"^[A-Z]{3}-[0-9]{4}$")]
    public string Sku { get; set; } = string.Empty;

    [Range(0.01, 100000)]
    public decimal Price { get; set; }

    [Required]
    public string Category { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext context)
    {
        if (!ProductCategories.All.Contains(Category))
            yield return new ValidationResult(
                "Unknown category.", [nameof(Category)]);
    }
}

public sealed class UpdateProductRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 100000)]
    public decimal Price { get; set; }
}
```

In `AdventureWorks.Core/Products.cs`, replace these lines. The three new members go at the end of IProductService.

```csharp
    Task<Product[]> ListByCategoryAsync(string category);
}
```

With these:

```csharp
    Task<Product[]> ListByCategoryAsync(string category);
    Task<Product> CreateAsync(CreateProductRequest request);
    Task<bool> UpdateAsync(int id, UpdateProductRequest request);
    Task<bool> DeleteAsync(int id);
}
```

In `AdventureWorks.Infrastructure/InMemoryProductService.cs`, add these lines above the line `private Product Add(string name, string sku,`. Above the private Add helper. CreateAsync reuses Add, so new IDs continue after the seed data.

```csharp
    public Task<Product> CreateAsync(
        CreateProductRequest request) =>
        Task.FromResult(Add(request.Name, request.Sku,
            request.Price, request.Category));

    public Task<bool> UpdateAsync(
        int id, UpdateProductRequest request)
    {
        while (_products.TryGetValue(id, out var current))
        {
            var changed = current with
            {
                Name = request.Name,
                Price = request.Price,
                Version = current.Version + 1
            };
            if (_products.TryUpdate(id, changed, current))
                return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> DeleteAsync(int id) =>
        Task.FromResult(_products.TryRemove(id, out _));
```

### Step 2: Define validation and logging filters

Two files: the manual validation filter, and the two logging filters (one MVC action filter, one endpoint filter).

Create `AdventureWorks/ValidationFilter.cs` (32 lines).

```csharp
using System.ComponentModel.DataAnnotations;

namespace AdventureWorks;

public sealed class ValidationFilter<T> : IEndpointFilter
    where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var model =
            context.Arguments.OfType<T>().FirstOrDefault();
        if (model is null)
            return Results.BadRequest(
                "Request body is required.");
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(model,
            new ValidationContext(model), results,
            validateAllProperties: true);
        if (valid) return await next(context);
        var errors = results
            .SelectMany(result => result.MemberNames
                .DefaultIfEmpty("General")
                .Select(member => (member, result.ErrorMessage)))
            .GroupBy(item => item.member, item =>
                item.ErrorMessage ?? "Invalid value.")
            .ToDictionary(group => group.Key,
                group => group.ToArray());
        return Results.ValidationProblem(errors);
    }
}
```

Create `AdventureWorks/LoggingFilters.cs` (30 lines).

```csharp
using Microsoft.AspNetCore.Mvc.Filters;

namespace AdventureWorks;

public sealed class RequestLoggingFilter(
    ILogger<RequestLoggingFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        logger.LogInformation("Starting action {Action}",
            context.ActionDescriptor.DisplayName);
        await next();
    }
}

public sealed class RequestLoggingEndpointFilter(
    ILogger<RequestLoggingEndpointFilter> logger)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        logger.LogInformation("Starting endpoint {Endpoint}",
            context.HttpContext.GetEndpoint()?.DisplayName);
        return await next(context);
    }
}
```

### Step 3: Implement the controller surface

Create `AdventureWorks/Controllers/ProductsController.cs` (49 lines).

ProductsController.cs, part 1 of 2 (26 lines):

```csharp
using System.ComponentModel.DataAnnotations;
using AdventureWorks.Core;
using Microsoft.AspNetCore.Mvc;

namespace AdventureWorks.Controllers;

[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController(IProductService products)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Product[]>> Search(
        [FromQuery] string? q,
        [FromQuery, Range(1, 10000)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20) =>
        Ok(await products.SearchAsync(q, page, pageSize));

    [HttpGet("{id:int}", Name = "GetProductV1")]
    [ProducesResponseType<Product>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await products.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }
```

ProductsController.cs, part 2 of 2 (22 lines):

```csharp
    [HttpPost]
    [ProducesResponseType<Product>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Product>> Create(
        CreateProductRequest request)
    {
        var product = await products.CreateAsync(request);
        return CreatedAtRoute("GetProductV1",
            new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id, UpdateProductRequest request) =>
        await products.UpdateAsync(id, request)
            ? NoContent() : NotFound();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await products.DeleteAsync(id) ? NoContent() : NotFound();
}
```

In `AdventureWorks/Program.cs`, add these lines after the line `builder.Services.AddOpenApi();`.

```csharp
builder.Services.AddControllers(options =>
    options.Filters.Add<RequestLoggingFilter>());
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.MapCatalogEndpoints();`.

```csharp
app.MapControllers();
```

### Step 4: Implement the Minimal API surface and binding

The handlers are named static methods with typed results, as in Chapter 4's catalog. The binding demonstration joins the Development-only examples, at /examples/binding/{id:int}.

Create `AdventureWorks/ProductEndpointsV2.cs` (70 lines).

ProductEndpointsV2.cs, part 1 of 2 (30 lines):

```csharp
using AdventureWorks.Core;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AdventureWorks;

public sealed record SearchParameters(
    string? Q, int Page = 1, int PageSize = 20);

public static class ProductEndpointsV2
{
    public static IEndpointRouteBuilder MapProductEndpointsV2(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2/products")
            .WithTags("Products V2")
            .AddEndpointFilter<RequestLoggingEndpointFilter>();
        group.MapGet("/", SearchProducts);
        group.MapGet("/{id:int}", GetProduct)
            .WithName("GetProductV2");
        group.MapPost("/", CreateProduct)
            .AddEndpointFilter<
                ValidationFilter<CreateProductRequest>>()
            .ProducesValidationProblem();
        group.MapPut("/{id:int}", UpdateProduct)
            .AddEndpointFilter<
                ValidationFilter<UpdateProductRequest>>()
            .ProducesValidationProblem();
        group.MapDelete("/{id:int}", DeleteProduct);
        return app;
    }
```

ProductEndpointsV2.cs, part 2 of 2 (39 lines):

```csharp
    static async Task<Results<Ok<Product[]>, BadRequest<string>>>
        SearchProducts([AsParameters] SearchParameters request,
            IProductService products)
    {
        if (request.Page is < 1 or > 10000 ||
            request.PageSize is < 1 or > 100)
            return TypedResults.BadRequest(
                "Page must be 1-10000 and pageSize 1-100.");
        return TypedResults.Ok(await products.SearchAsync(
            request.Q, request.Page, request.PageSize));
    }

    static async Task<Results<Ok<Product>, NotFound>> GetProduct(
        int id, IProductService products)
    {
        var product = await products.GetByIdAsync(id);
        return product is null
            ? TypedResults.NotFound() : TypedResults.Ok(product);
    }

    static async Task<CreatedAtRoute<Product>> CreateProduct(
        CreateProductRequest request, IProductService products)
    {
        var product = await products.CreateAsync(request);
        return TypedResults.CreatedAtRoute(
            product, "GetProductV2", new { id = product.Id });
    }

    static async Task<Results<NoContent, NotFound>> UpdateProduct(
        int id, UpdateProductRequest request,
        IProductService products) =>
        await products.UpdateAsync(id, request)
            ? TypedResults.NoContent() : TypedResults.NotFound();

    static async Task<Results<NoContent, NotFound>> DeleteProduct(
        int id, IProductService products) =>
        await products.DeleteAsync(id)
            ? TypedResults.NoContent() : TypedResults.NotFound();
}
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.MapControllers();`.

```csharp
app.MapProductEndpointsV2();
```

In `AdventureWorks/DevelopmentEndpoints.cs`, add these lines after the line `using AdventureWorks.Core;`.

```csharp
using Microsoft.AspNetCore.Mvc;
```

In `AdventureWorks/DevelopmentEndpoints.cs`, replace these lines. The last lines of the file: one more route at the end of MapRouteExamples, and its handler.

```csharp
                new { Id = id, ReviewId = reviewId }));
    }
}
```

With these:

```csharp
                new { Id = id, ReviewId = reviewId }));
        group.MapGet("/binding/{id:int}", EchoBinding);
    }

    static IResult EchoBinding(int id, [FromQuery] string? q,
        [FromHeader(Name = "X-Api-Version")] string version) =>
        Results.Ok(new { id, q, version });
}
```

## Program.cs at the checkpoint (55 lines)

Program.cs, part 1 of 2 (20 lines):

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
```

Program.cs, part 2 of 2 (34 lines):

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
app.MapControllers();
app.MapProductEndpointsV2();
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

`requests.http` holds these requests in this order; 29 of 29 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `GET /api/v1/products` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":"Mountain...
  ```

- `POST /api/v1/products` returned **201** (Location: https://localhost:7001/api/v1/products/5)

  ```text
  {"id":5,"name":"Trail Bottle","sku":"BTL-5001","price":24.99,"category":"accessories","version":1}
  ```

- `GET /api/v2/products/5` returned **200**

  ```text
  {"id":5,"name":"Trail Bottle","sku":"BTL-5001","price":24.99,"category":"accessories","version":1}
  ```

- `GET /api/catalog/5` returned **200**

  ```text
  {"id":5,"name":"Trail Bottle","sku":"BTL-5001","price":24.99,"category":"accessories","reviewsLink":"/api/catalog/5/reviews"}
  ```

- `POST /api/v2/products` returned **201** (Location: https://localhost:7001/api/v2/products/6)

  ```text
  {"id":6,"name":"Camp Mug","sku":"MUG-6001","price":12.50,"category":"camping","version":1}
  ```

- `GET /api/v1/products?q=mug` returned **200**

  ```text
  [{"id":6,"name":"Camp Mug","sku":"MUG-6001","price":12.50,"category":"camping","version":1}]
  ```

- `PUT /api/v1/products/5` returned **204**

- `PUT /api/v2/products/6` returned **204**

- `GET /api/v2/products/5` returned **200**

  ```text
  {"id":5,"name":"Trail Bottle XL","sku":"BTL-5001","price":27.99,"category":"accessories","version":2}
  ```

- `POST /api/v1/products` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Name":["The field Name must be a string with a minimum length of 3 and a maximum leng...
  ```

- `POST /api/v2/products` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Price":["The field Price must be between 0.01 and 100000."]},"traceId":"00-39eaee8b5d...
  ```

- `POST /api/v1/products` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Sku":["The field Sku must match the regular expression '^[A-Z]{3}-[0-9]{4}$'."]},"tra...
  ```

- `POST /api/v2/products` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Sku":["The field Sku must match the regular expression '^[A-Z]{3}-[0-9]{4}$'."]},"tra...
  ```

- `POST /api/v1/products` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Category":["Unknown category."]},"traceId":"00-1d7dd4b03804ad04312a441ff820fa93-1d76a...
  ```

- `POST /api/v2/products` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Category":["Unknown category."]},"traceId":"00-797d306e540aaab7df0bebd2d708e9ec-e5302...
  ```

- `GET /api/v2/products` returned **200**

  ```text
  [{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":"Mountain...
  ```

- `GET /api/v2/products?page=0` returned **400**

  ```text
  "Page must be 1-10000 and pageSize 1-100."
  ```

- `GET /api/v1/products?pageSize=500` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"pageSize":["The field pageSize must be between 1 and 100."]},"traceId":"00-c9efbe4548...
  ```

- `PUT /api/v1/products/99` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-5aaab8df25644c7857eb268b7243f9a5-167bfb2e396dd025-02","correlationId":"29e781a1860a4e68ae4e95688c40d037"}
  ```

- `DELETE /api/v1/products/5` returned **204**

- `GET /api/v2/products/5` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-85b601c3fbc7b3878eed4591bd428166-9ffdb9254b919b84-02","correlationId":"25a5698d91e2449ca0d6a066b63860fe"}
  ```

- `DELETE /api/v2/products/6` returned **204**

- `DELETE /api/v2/products/6` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-227ff836c4aa137d8594a0125e9b6e29-d871be3245ff1533-02","correlationId":"10c8550c9c274a948b32e1f46a42d6d4"}
  ```

- `GET /examples/binding/7?q=tent` returned **200**

  ```text
  {"id":7,"q":"tent","version":"2"}
  ```

- `GET /examples/binding/7?q=tent` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Microsoft.AspNetCore.Http.BadHttpRequestException","status":400,"detail":"Required parameter \"string version\" was not provided from header.","except...
  ```

- `GET /openapi/v1.json` returned **200**

  ```text
  (the OpenAPI JSON document)
  ```

- `GET /api/catalog/category/camping` returned **200**

  ```text
  [{"id":4,"name":"Two-Person Tent","sku":"TNT-4001","price":189.00,"category":"camping","version":1}]
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

- The console logs `Selected endpoint AdventureWorks.Controllers.ProductsController.Create (AdventureWorks)` and `Starting action AdventureWorks.Controllers.ProductsController.Create (AdventureWorks)` for the v1 POST, and `Starting endpoint` lines for v2: checked. The filters now log at Information (master finding C05-6), like Chapter 4's endpoint log, so the default console shows them.

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks/ValidationFilter.cs` on line 21:

```csharp
if (valid) return await next(context);
```

Press F5 (the https profile), then send POST https://localhost:7001/api/v2/products with the lowercase SKU "lan-0005". When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
typeof(T).Name = CreateProductRequest [String]
model = AdventureWorks.Core.CreateProductRequest [CreateProductRequest]
valid = False [Boolean]
results.Count = 1 [Int32]
results[0].ErrorMessage = The field Sku must match the regular expression '^[A-Z]{3}-[0-9]{4}$'. [String]
string.Join(",", results[0].MemberNames) = Sku [String]
```

Binding has already produced the request object, and Validator.TryValidateObject has run. valid is false and results holds one error for Sku, which becomes the 400 ValidationProblem; the handler never runs.

## What the Activity adds

A price-quote group (Quotes.cs and QuoteEndpoints.cs): `CreateQuoteRequest` (ProductId Range 1 and up; Quantity Range 1 to 20), `QuoteDto(int Id, int ProductId, int Quantity, decimal Total)`, a singleton `QuoteStore` (ConcurrentDictionary, Interlocked IDs), a `QuoteTimingFilter` (Stopwatch.GetTimestamp and a finally block) on the group, `POST /api/quotes` with `ValidationFilter<CreateQuoteRequest>` and ProducesValidationProblem returning `CreatedAtRoute<QuoteDto>` (or a ValidationProblem for an unknown product), and `GET /api/quotes/{id:int}` named GetQuote returning `Results<Ok<QuoteDto>, NotFound>`. The server computes the total from the catalog price (2 helmets: 59.98).

This departs from the chapter's "order endpoint group": `GET /api/orders/{id:int}` already exists (Chapter 2), and the design keeps one CreateOrderRequest and one OrderDto for the owner-scoped order contract later chapters build. A quote exercises exactly the same Activity skills without a second order model.

The reader-path check rebuilt `AdventureWorks/Chapter05-Activity` from the Chapter 5 checkpoint: no differences.

### Activity: a price-quote group

Work in a copy of the Chapter 5 checkpoint folder. Chapter 6 starts from the checkpoint. The Activity builds price quotes rather than orders: GET /api/orders/{id:int} already exists, and the running project keeps CreateOrderRequest and OrderDto for the owner-scoped order contract that later chapters build. A quote has the same shape the Activity asks for, and the server computes its total from the catalog price, so a client never sends a price.

Create `AdventureWorks/Quotes.cs` (35 lines).

```csharp
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

namespace AdventureWorks;

public sealed class CreateQuoteRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 20)]
    public int Quantity { get; set; }
}

public sealed record QuoteDto(
    int Id, int ProductId, int Quantity, decimal Total);

public sealed class QuoteStore
{
    private readonly ConcurrentDictionary<int, QuoteDto> _quotes
        = new();
    private int _lastId;

    public QuoteDto Add(
        int productId, int quantity, decimal total)
    {
        var id = Interlocked.Increment(ref _lastId);
        var quote = new QuoteDto(id, productId, quantity, total);
        _quotes[id] = quote;
        return quote;
    }

    public QuoteDto? Find(int id) =>
        _quotes.GetValueOrDefault(id);
}
```

Create `AdventureWorks/QuoteEndpoints.cs` (68 lines).

QuoteEndpoints.cs, part 1 of 3 (26 lines):

```csharp
using System.Diagnostics;
using AdventureWorks.Core;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AdventureWorks;

public sealed class QuoteTimingFilter(
    ILogger<QuoteTimingFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            return await next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            logger.LogInformation("Quote endpoint took {Ms} ms",
                elapsed.TotalMilliseconds);
        }
    }
}
```

QuoteEndpoints.cs, part 2 of 3 (20 lines):

```csharp
public static class QuoteEndpoints
{
    public static IEndpointRouteBuilder MapQuoteEndpoints(
        this IEndpointRouteBuilder app)
    {
        var quotes = app.MapGroup("/api/quotes")
            .WithTags("Quotes")
            .AddEndpointFilter<QuoteTimingFilter>();
        quotes.MapGet("/{id:int}", GetQuote).WithName("GetQuote");
        quotes.MapPost("/", CreateQuote)
            .AddEndpointFilter<
                ValidationFilter<CreateQuoteRequest>>()
            .ProducesValidationProblem();
        return app;
    }

    static Results<Ok<QuoteDto>, NotFound> GetQuote(
        int id, QuoteStore store) =>
        store.Find(id) is { } quote
            ? TypedResults.Ok(quote) : TypedResults.NotFound();
```

QuoteEndpoints.cs, part 3 of 3 (20 lines):

```csharp
    static async Task<Results<CreatedAtRoute<QuoteDto>,
        ValidationProblem>> CreateQuote(
        CreateQuoteRequest request,
        IProductService products, QuoteStore store)
    {
        var product =
            await products.GetByIdAsync(request.ProductId);
        if (product is null)
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["ProductId"] = ["Unknown product."]
                });
        var total = product.Price * request.Quantity;
        var quote = store.Add(
            product.Id, request.Quantity, total);
        return TypedResults.CreatedAtRoute(
            quote, "GetQuote", new { id = quote.Id });
    }
}
```

In `AdventureWorks/Program.cs`, add these lines after the line `builder.Services.AddCatalog();`.

```csharp
builder.Services.AddSingleton<QuoteStore>();
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.MapProductEndpointsV2();`.

```csharp
app.MapQuoteEndpoints();
```

### Activity requests and responses

34 of 34 requests in `Chapter05-Activity/requests.http` returned their commented status. The responses for the requests the Activity adds:

- `POST /api/quotes` returned **201** (Location: https://localhost:7001/api/quotes/1)

  ```text
  {"id":1,"productId":1,"quantity":2,"total":59.98}
  ```

- `GET /api/quotes/1` returned **200**

  ```text
  {"id":1,"productId":1,"quantity":2,"total":59.98}
  ```

- `POST /api/quotes` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Quantity":["The field Quantity must be between 1 and 20."]},"traceId":"00-159b813a5cc...
  ```

- `POST /api/quotes` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"ProductId":["Unknown product."]},"traceId":"00-09afbf13d9c86c2f00a2005a751c2859-5399d...
  ```

- `GET /api/quotes/2` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-203957dda4217bb34fddb4dc63d95dce-613480db17a70a33-02","correlationId":"b354ae8de8524510aef9f6751556c452"}
  ```

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| Step 1 Models.cs | Replaced | ProductDto is removed (both versions return Core's Product). CreateProductRequest and UpdateProductRequest move to Core/ProductRequests.cs, gain Category and the category rule in place of the "Test products" rule, and use `[Range(0.01, 100000)]`. SearchParameters moves into ProductEndpointsV2.cs. The IProductService members are added to Chapter 4's interface. |
| Step 1 ProductService.cs | Replaced | Three methods added to Chapter 4's InMemoryProductService, above its Add helper. |
| Step 2 Filters.cs (40 lines) | Replaced | Two files: ValidationFilter.cs (32 lines) and LoggingFilters.cs (30 lines), line breaks for width. |
| Step 3 ProductsController.cs | Replaced | Product instead of ProductDto; namespace AdventureWorks.Controllers in a Controllers folder; 49 lines, two parts. |
| Step 4 ProductEndpointsV2.cs | Replaced | Named static handlers with the same typed results; 70 lines, two parts. |
| Step 6 package command | Removed | Chapter 4 added the package. |
| Full Example repeats | Removed | Each file is printed once. |
| Full Example Program.cs | Replaced | Adds four lines (the two-line AddControllers call, MapControllers and MapProductEndpointsV2) to Chapter 4's; 55 lines. |
| Full Example text "remove Chapter 4's ProductEndpoints.cs and its old Program.cs registrations" | Removed | Nothing is removed; the routes do not overlap. |

## Notes for the writers

- Concept Overview: "This chapter replaces Chapter 4's fixed product responses with a runnable in-memory service" is now "extends Chapter 4's product store with writes"; "These path prefixes are not an API-versioning library" becomes the two-versions wording above.
- The binding demonstration moved from `/api/binding/{id:int}` to the Development-only `/examples/binding/{id:int}`, beside the other demonstrations, and became a named handler (EchoBinding). It returns `{"id":7,"q":"tent","version":"2"}`; without the header it returns 400.
- The decimal ranges use the double overload `[Range(0.01, 100000)]`. The string form `Range(typeof(decimal), "0.01", "100000")` parses its limits with the current culture, which breaks under Chapter 7's French culture; the double form needs no parsing.
- `Validator.TryValidateObject` with validateAllProperties true runs the IValidatableObject rule only after the property attributes pass, so `"category": "boats"` with valid other fields returns 400 with "Unknown category.".
- The write routes have no authentication yet; Chapter 9 requires the Admin role for them, as the design says.
- Quiz and Key Terms are unaffected.
