# Chapter 3 delta: Middleware and the HTTP Request Pipeline

Generated October 30, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter03` and the operations that rebuild it from the Chapter 2 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 2's projects, options, lifetimes and routes stay. The chapter extends the pipeline in Program.cs (still inline, as the design asks for Chapters 1 to 3):

- Core gains the two domain exceptions, `DomainValidationException` and `ResourceNotFoundException`.
- Web gains `CorrelationIdMiddleware` (IMiddleware; accepts a 1 to 64 character X-Correlation-ID of ASCII letters, digits, hyphens and underscores, otherwise a new GUID; sets the response header in OnStarting; opens a log scope) and `DomainExceptionMiddleware` (IMiddleware; maps the two exceptions to 400 and 404 Problem Details).
- Chapter 1's `RequestTimingMiddleware` stays unchanged as the convention-based form, so the app shows both activation styles side by side. It moves from the top of the pipeline to just inside the correlation middleware.
- One Problem Details customization adds `correlationId` to every Problem Details response.
- A terminal `MapWhen` branch answers `/ping` with `pong` before the correlation middleware, so it carries no correlation header. `/health` stays the Chapter 1 health-check endpoint.
- A `UseWhen` branch adds `X-API-Pipeline: true` to `/api` responses and rejoins the pipeline.
- The order route throws the domain exceptions (id 0 gives 400, an unknown id 404), and the in-memory repository throws a simulated storage failure for order 500, so the unexpected-error path can be requested.

The pipeline order is: (Development) developer exception page added by the host, or (other environments) exception handler and HSTS; HTTPS redirection; the `/ping` branch; status-code pages; correlation; timing; domain exceptions; routing; the `/api` branch; endpoints. This is the start of the design's single order; later chapters insert endpoint logging after routing, then authentication, authorization and the rest.

## Files added or changed

| File | Change |
| --- | --- |
| `AdventureWorks/Program.cs` | changed |
| `AdventureWorks.Core/DomainExceptions.cs` | added |
| `AdventureWorks/CorrelationIdMiddleware.cs` | added |
| `AdventureWorks/DomainExceptionMiddleware.cs` | added |
| `AdventureWorks.Infrastructure/Services.cs` | changed |

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 16 of 16 files identical and no differences.

### Step 1: Trace the delegate chain

A temporary demonstration. Add the two delegates after `app.UseHttpsRedirection();`, run, request /api/status once, and the console prints ABba. Then delete the same lines; the checkpoint does not keep them.

In `AdventureWorks/Program.cs`, add these lines after the line `app.UseHttpsRedirection();`.

```csharp
app.Use(async (context, next) =>
{
    Console.Write("A");
    await next();
    Console.Write("a");
});
app.Use(async (context, next) =>
{
    Console.Write("B");
    await next();
    Console.Write("b");
});
```

In `AdventureWorks/Program.cs`, delete these lines. Remove the demonstration before Step 2.

```csharp
app.Use(async (context, next) =>
{
    Console.Write("A");
    await next();
    Console.Write("a");
});
app.Use(async (context, next) =>
{
    Console.Write("B");
    await next();
    Console.Write("b");
});
```

### Step 2: Define the middleware and domain errors

The exception types are domain errors, so they go in Core. The two middleware classes go in Web, one file each. Chapter 1's RequestTimingMiddleware stays as it is: it is the convention-based form, and these two are the factory-activated IMiddleware form, so the app now shows both.

Create `AdventureWorks.Core/DomainExceptions.cs` (7 lines).

```csharp
namespace AdventureWorks.Core;

public sealed class DomainValidationException(string message)
    : Exception(message);

public sealed class ResourceNotFoundException(string message)
    : Exception(message);
```

Create `AdventureWorks/CorrelationIdMiddleware.cs` (35 lines). Chapter 16 extends this file with a trace-ID fallback.

```csharp
namespace AdventureWorks;

public sealed class CorrelationIdMiddleware(
    ILogger<CorrelationIdMiddleware> logger) : IMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(
        HttpContext context, RequestDelegate next)
    {
        var supplied =
            context.Request.Headers[HeaderName].ToString();
        var id = IsValid(supplied)
            ? supplied : Guid.NewGuid().ToString("N");
        context.Items["CorrelationId"] = id;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = id;
            return Task.CompletedTask;
        });
        var scope = new Dictionary<string, object>
        {
            ["CorrelationId"] = id
        };
        using (logger.BeginScope(scope))
        {
            await next(context);
        }
    }

    private static bool IsValid(string value) =>
        value.Length is > 0 and <= 64 &&
        value.All(c =>
            char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
```

Create `AdventureWorks/DomainExceptionMiddleware.cs` (41 lines).

DomainExceptionMiddleware.cs, part 1 of 2 (25 lines):

```csharp
using AdventureWorks.Core;
using Microsoft.AspNetCore.Mvc;

namespace AdventureWorks;

public sealed class DomainExceptionMiddleware(
    IProblemDetailsService problems) : IMiddleware
{
    public async Task InvokeAsync(
        HttpContext context, RequestDelegate next)
    {
        try { await next(context); }
        catch (DomainValidationException ex)
            when (!context.Response.HasStarted)
        {
            await WriteAsync(
                context, 400, "Invalid request", ex.Message);
        }
        catch (ResourceNotFoundException ex)
            when (!context.Response.HasStarted)
        {
            await WriteAsync(
                context, 404, "Resource not found", ex.Message);
        }
    }
```

DomainExceptionMiddleware.cs, part 2 of 2 (15 lines):

```csharp
    private async Task WriteAsync(HttpContext context,
        int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status, Title = title, Detail = detail
        };
        var written = await problems.TryWriteAsync(new()
        {
            HttpContext = context, ProblemDetails = problem
        });
        if (!written) await context.Response.WriteAsync(title);
    }
}
```

### Step 3: Register services before building

Replace Chapter 1's one-line AddProblemDetails with the version that adds the correlation ID to every Problem Details response, and register the two IMiddleware classes after it. RequestTimingMiddleware needs no registration because it is convention-based.

In `AdventureWorks/Program.cs`, replace these lines.

```csharp
builder.Services.AddProblemDetails();
```

With these:

```csharp
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["correlationId"] =
            context.HttpContext.Items["CorrelationId"]);
builder.Services.AddTransient<CorrelationIdMiddleware>();
builder.Services.AddTransient<DomainExceptionMiddleware>();
```

### Step 4: Order errors, transport and routing

Move request timing from the top of the pipeline to just inside the correlation middleware, so its log lines carry the correlation scope. Delete the line after `var app = builder.Build();`, then add the ordered middleware after `app.UseHttpsRedirection();`.

In `AdventureWorks/Program.cs`, delete these lines.

```csharp
app.UseMiddleware<RequestTimingMiddleware>();
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.UseHttpsRedirection();`.

```csharp
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();
app.UseMiddleware<DomainExceptionMiddleware>();
app.UseRouting();
```

### Step 5: Distinguish expected and unexpected errors

The order route now throws the two domain exceptions, and the in-memory repository simulates a storage failure for order 500, so all three outcomes can be requested.

In `AdventureWorks.Infrastructure/Services.cs`, replace the OrderRepository class with the following.

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

In `AdventureWorks/Program.cs`, replace these lines. The last two lines of the /api/orders/{id:int} handler; the 503 check above them stays.

```csharp
    var order = orders.GetById(id);
    return order is null ? Results.NotFound() : Results.Ok(order);
```

With these:

```csharp
    if (id <= 0)
        throw new DomainValidationException(
            "Order ID must be positive.");
    return Results.Ok(orders.GetById(id) ??
        throw new ResourceNotFoundException(
            $"Order {id} was not found."));
```

### Step 6: Branch deliberately

Add the terminal /ping branch directly after `app.UseHttpsRedirection();`, so it runs before status-code pages and the correlation middleware. Add the UseWhen branch after `app.UseRouting();`.

In `AdventureWorks/Program.cs`, add these lines after the line `app.UseHttpsRedirection();`.

```csharp
app.MapWhen(context => context.Request.Path == "/ping",
    ping => ping.Run(context =>
        context.Response.WriteAsync("pong")));
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.UseRouting();`.

```csharp
app.UseWhen(context =>
    context.Request.Path.StartsWithSegments("/api"),
    api => api.Use(async (context, next) =>
    {
        context.Response.Headers["X-API-Pipeline"] = "true";
        await next();
    }));
```

## Program.cs at the checkpoint (78 lines)

Program.cs, part 1 of 3 (26 lines):

```csharp
using AdventureWorks;
using AdventureWorks.Core;
using AdventureWorks.Infrastructure;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});
builder.Services.AddOptions<FeatureFlags>()
    .Bind(builder.Configuration.GetSection("FeatureFlags"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<ICacheService,
    MemoryCacheService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddTransient<IEmailValidator, EmailValidator>();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["correlationId"] =
            context.HttpContext.Items["CorrelationId"]);
builder.Services.AddTransient<CorrelationIdMiddleware>();
builder.Services.AddTransient<DomainExceptionMiddleware>();
```

Program.cs, part 2 of 3 (24 lines):

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
app.UseWhen(context =>
    context.Request.Path.StartsWithSegments("/api"),
    api => api.Use(async (context, next) =>
    {
        context.Response.Headers["X-API-Pipeline"] = "true";
        await next();
    }));
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/api/status"));
```

Program.cs, part 3 of 3 (27 lines):

```csharp
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
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/config", (IOptions<FeatureFlags> flags) =>
        Results.Ok(flags.Value));
    app.MapGet("/api/email-check",
        (string address, IEmailValidator validator) => Results.Ok(
            new { IsValid = validator.IsValid(address) }));
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

`requests.http` holds these requests in this order; 11 of 11 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `GET /ping` returned **200** (no X-Correlation-ID header)

  ```text
  pong
  ```

- `GET /api/status` returned **200** (X-Correlation-ID: checkout-42, X-API-Pipeline: true)

  ```text
  {"status":"Operational","timestamp":"2026-11-03T14:28:50.7373419Z"}
  ```

- `GET /api/orders/1` returned **200**

  ```text
  {"id":1,"orderNumber":"AW-0001","total":49.00}
  ```

- `GET /api/orders/0` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Invalid request","status":400,"detail":"Order ID must be positive.","traceId":"00-fae70feef6f6a5d5e6ac357daa3fa90c-4c6f685d54fc9075-02","correlationId...
  ```

- `GET /api/orders/7` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found","status":404,"detail":"Order 7 was not found.","traceId":"00-8fe842d1e957f548d573a1bae6cb0820-5f5cb424976f7d70-02","correlationId"...
  ```

- `GET /api/orders/500` returned **500**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.6.1","title":"System.InvalidOperationException","status":500,"detail":"Simulated storage failure.","exception":{"details":"System.InvalidOperationException: Simulat...
  ```

- `GET /missing` returned **404**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-29776f944ebe7fa96884410df2b1a510-421dd6b52122c0d4-02","correlationId":"784cdc7b0a3442b7b2f7c8fd3f745641"}
  ```

- `GET /` returned **302** (Location: /api/status)

- `GET /health` returned **200**

  ```text
  Healthy
  ```

- `GET /api/config` returned **200**

  ```text
  {"enableNewCheckout":true,"maxCartItems":30,"allowedRegions":["US"]}
  ```

- `GET /api/email-check?address=ada@example.com` returned **200**

  ```text
  {"isValid":true}
  ```

### Other runs

- Production (`-- --environment Production`): `/api/orders/500` returns a generic 500 Problem Details without the text "Simulated storage failure"; `/api/orders/7` still returns the domain 404. Checked.
- Step 1's delegate demonstration, added after `app.UseHttpsRedirection();` in a scratch copy: one request to `/api/status` prints `ABba` on the console. Checked.
- In Development, `/api/orders/500` returns 500 and the console logs `System.InvalidOperationException: Simulated storage failure.` Checked.

## What the Activity adds

`SecurityHeadersMiddleware` (X-Content-Type-Options nosniff, X-Frame-Options DENY) and `IdempotencyKeyMiddleware` (POST requests need a 1 to 64 character key of ASCII letters, digits, hyphens or underscores; the key is echoed) in ActivityMiddleware.cs; the headers right after HTTPS redirection, so every branch gets them; a terminal `/webhooks` branch with its own idempotency middleware that accepts only POST `/webhooks/demo`; the idempotency middleware and a local exercise guard (401 unless `X-Exercise-Identity: developer`) in the main path after the domain-exception middleware; and `POST /api/payments`. The Chapter 3 routes keep working in the Activity copy.

The reader-path check rebuilt `AdventureWorks/Chapter03-Activity` from the Chapter 3 checkpoint: no differences.

### Activity: security headers, idempotency keys and a webhook branch

Work in a copy of the Chapter 3 checkpoint folder. Chapter 4 starts from the checkpoint.

Create `AdventureWorks/ActivityMiddleware.cs` (42 lines).

ActivityMiddleware.cs, part 1 of 2 (14 lines):

```csharp
namespace AdventureWorks;

// Local exercise only: these are not payment authentication,
// webhook signature checks or durable idempotency.
public sealed class SecurityHeadersMiddleware : IMiddleware
{
    public async Task InvokeAsync(
        HttpContext context, RequestDelegate next)
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        await next(context);
    }
}
```

ActivityMiddleware.cs, part 2 of 2 (27 lines):

```csharp
public sealed class IdempotencyKeyMiddleware : IMiddleware
{
    public async Task InvokeAsync(
        HttpContext context, RequestDelegate next)
    {
        if (HttpMethods.IsPost(context.Request.Method))
        {
            var key = context.Request.Headers["Idempotency-Key"]
                .ToString();
            if (!IsValid(key))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(
                    "A bounded exercise key is required.");
                return;
            }
            context.Items["IdempotencyKey"] = key;
            context.Response.Headers["Idempotency-Key"] = key;
        }
        await next(context);
    }

    private static bool IsValid(string key) =>
        key.Length is > 0 and <= 64 &&
        key.All(c =>
            char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
```

In `AdventureWorks/Program.cs`, add these lines after the line `builder.Services.AddTransient<DomainExceptionMiddleware>();`.

```csharp
builder.Services.AddTransient<SecurityHeadersMiddleware>();
builder.Services.AddTransient<IdempotencyKeyMiddleware>();
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.UseHttpsRedirection();`. Before every branch, so all responses carry the headers.

```csharp
app.UseMiddleware<SecurityHeadersMiddleware>();
```

In `AdventureWorks/Program.cs`, add these lines after the line `context.Response.WriteAsync("pong")));`. After the /ping branch.

```csharp
app.MapWhen(context =>
    context.Request.Path.StartsWithSegments("/webhooks"),
    webhooks =>
    {
        webhooks.UseMiddleware<IdempotencyKeyMiddleware>();
        webhooks.Run(async context =>
        {
            if (!HttpMethods.IsPost(context.Request.Method) ||
                context.Request.Path != "/webhooks/demo")
            {
                context.Response.StatusCode = 404;
                return;
            }
            await context.Response.WriteAsJsonAsync(
                new { Key = context.Items["IdempotencyKey"] });
        });
    });
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.UseMiddleware<DomainExceptionMiddleware>();`.

```csharp
app.UseMiddleware<IdempotencyKeyMiddleware>();
app.Use(async (context, next) =>
{
    // A local exercise guard, not authentication.
    var identity = context.Request.Headers["X-Exercise-Identity"];
    if (context.Request.Path.StartsWithSegments("/api/payments")
        && identity != "developer")
    {
        context.Response.StatusCode = 401;
        return;
    }
    await next();
});
```

In `AdventureWorks/Program.cs`, add these lines above the line `if (app.Environment.IsDevelopment())`. After the order route, before the Development-only block.

```csharp
app.MapPost("/api/payments", (HttpContext context) =>
    Results.Ok(new { Key = context.Items["IdempotencyKey"] }));
```

### Activity requests and responses

17 of 17 requests in `Chapter03-Activity/requests.http` returned their commented status. The responses for the requests the Activity adds:

- `POST /api/payments` returned **401** (X-Content-Type-Options: nosniff)

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"traceId":"00-20f8137fed907c8f225f8b36e618ae04-16f003eea96929d0-02","correlationId":"cab84733d6ba486db6790720cd5507ca"}
  ```

- `POST /api/payments` returned **200**

  ```text
  {"key":"order-1001"}
  ```

- `POST /webhooks/demo` returned **200** (X-Frame-Options: DENY)

  ```text
  {"key":"hook-2001"}
  ```

- `POST /api/payments` returned **400**

  ```text
  A bounded exercise key is required.
  ```

- `POST /api/payments` returned **400**

  ```text
  A bounded exercise key is required.
  ```

- `GET /webhooks/demo` returned **404**

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| Step 1 A/a/B/b delegates | Kept | Temporary; the delta says to add them after `app.UseHttpsRedirection();` and delete them before Step 2. |
| Step 2 PipelineMiddleware.cs (68 lines) | Replaced | Three files: Core/DomainExceptions.cs, CorrelationIdMiddleware.cs (35 lines) and DomainExceptionMiddleware.cs (41 lines). Its second RequestTimingMiddleware class is removed: Chapter 1's file stays. The paragraph telling the reader to replace Chapter 1's RequestTimingMiddleware.cs must go. |
| Step 3 registration excerpt | Replaced | AddProblemDetails now carries the correlationId customizer; only the two IMiddleware classes are registered (no AddTransient for RequestTimingMiddleware, which is convention-based). |
| Step 4 order schematic | Replaced | Must match the delta's actual order (see the summary), with later chapters' additions marked as later. |
| Step 5 and Step 6 text | Changed | `/ping` is the MapWhen branch; `/health` is untouched. Expected and unexpected errors use the order route (0, 7, 500). |
| Full Example PipelineMiddleware.cs repeat | Removed | Printed once, in Step 2. |
| Full Example Program.cs | Replaced | Keeps every Chapter 1 and 2 line; 78 lines, printed in two or three parts (services, pipeline, endpoints). |
| Full Example run text ("/health and /api/products/{id}", IDs 1, 0, 1001, 500) | Replaced | `/ping`, `/api/orders/1`, `/api/orders/0`, `/api/orders/7`, `/api/orders/500`, `/missing`. |

## Notes for the writers

- Program.cs is 78 lines here, over the design's target of about 60, because Chapters 1 to 3 stay inline. Chapter 4 moves the Chapter 1 and 2 routes and registrations into feature files and brings it back to 51.
- Concept Overview line 27 ("Its Full Example replaces Program.cs ... Chapter 2's order routes and options are not carried forward") is now wrong: nothing is dropped.
- The book's `if (IsDevelopment) UseDeveloperExceptionPage(); else UseExceptionHandler();` is not needed: WebApplication adds the developer exception page in Development, so Chapter 1's `if (!IsDevelopment) { UseExceptionHandler(); UseHsts(); }` block stays.
- Order 500's failure lives in the in-memory repository as a stand-in for a storage outage. When Chapter 10 replaces the repository with EF Core, order 500 simply does not exist (404).
- The correlation log scope is real but invisible in the default console format (IncludeScopes is off); Chapter 16 turns on scopes in JSON logs.
- Quiz question 1 (put the terminal health branch before the guard) still fits: the Activity shows it with `/ping` and `/webhooks`.
- `/missing` returns 404 Problem Details from status-code pages, including correlationId, because AddProblemDetails is registered.
- Every Problem Details response already carries `traceId`: the framework's default writer adds it. The design's "traceId added in the same delegate" in Chapter 13 is therefore already true; Chapter 13 should explain it rather than add it.
- Step 1's letters print in the console window that runs the app. A browser may also request /favicon.ico, so the letters can print twice; curl.exe or requests.http sends one request.
- In the finished pipeline, request timing runs inside any delegate added right after HTTPS redirection, so if a reader repeats the Step 1 experiment there, the timing log line appears between `AB` and `ba` (checked). Keep the demonstration at the start of the chapter, where it prints `ABba` (checked on the Chapter 2 checkpoint).
- The objective "Extend Chapter 1's convention-based middleware and implement factory-activated IMiddleware" should become "Compare Chapter 1's convention-based middleware with factory-activated IMiddleware": Chapter 1's file is unchanged (the design allowed converting it only if the lesson needs it; showing both forms side by side serves the lesson better).
