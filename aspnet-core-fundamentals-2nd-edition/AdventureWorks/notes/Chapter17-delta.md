# Chapter 17 delta: Performance Optimization and Benchmarking

Generated November 3, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter17` and the operations that rebuild it from the Chapter 16 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 17 measures before it optimizes: a price-feed import for the Admin that reads its body as a bounded stream, and a BenchmarkDotNet project that compares two ways to check a feed line.

- Core `PriceFeed.cs`: CountAsync reads "sku,name,price" lines with a strict UTF-8 decoder, skips empty lines, and refuses a line over 1,024 characters, a record past 1,000, a quote, a missing field or a price that is not a positive number (FormatException; an invalid byte sequence is DecoderFallbackException). IsRecord checks one line through spans.
- Web `ProductImport.cs`: `POST /api/admin/products/import` (Admin, antiforgery, text body) answers 200 `{"records":n}`, 400 with the reason, 403 for a customer, and 413 Problem Details above 64 KB (RequestSizeLimit metadata sets the route's limit; the read that passes it throws BadHttpRequestException, which the endpoint catches). Program.cs gains `app.MapProductImport();`: 90 lines.
- `AdventureWorks.Benchmarks`: a console project (`dotnet new console`) referencing Core, with BenchmarkDotNet 0.16.0-preview.2; `PriceFeedBenchmarks` compares Split with PriceFeed.IsRecord over 1,000 lines, with MemoryDiagnoser. Six projects in AdventureWorks.slnx.

## Files added or changed

| File | Change |
| --- | --- |
| `AdventureWorks.Core/PriceFeed.cs` | added |
| `AdventureWorks/ProductImport.cs` | added |
| `AdventureWorks/Program.cs` | changed |
| `AdventureWorks.Benchmarks/Program.cs` | replaced |
| `AdventureWorks.Benchmarks/PriceFeedBenchmarks.cs` | added |
| (command) `dotnet new console -o AdventureWorks.Benchmarks` | creates or edits project files |
| (command) `dotnet sln add AdventureWorks.Benchmarks` | creates or edits project files |
| (command) `dotnet add AdventureWorks.Benchmarks reference AdventureWorks.Core` | creates or edits project files |
| (command) `dotnet add AdventureWorks.Benchmarks package BenchmarkDotNet@0.16.0-preview.2` | creates or edits project files |

The reader types 139 lines of code: 133 in 3 new files and 6 new or changed in existing files, plus 7 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 101 of 101 files identical and no differences.

### Step 1: A price feed, checked and counted

The Admin can post a price feed: plain text, one "sku,name,price" line per product. This chapter checks and counts it and saves nothing; the point is what reading it costs. PriceFeed lives in Core, so the benchmark project can call it without the web app. It reads the body as a stream with a strict UTF-8 decoder (an invalid or cut-off character is an error, not a replacement character), skips empty lines, and stops at 1,000 records or a line over 1,024 characters. IsRecord checks each line through spans over it: no array from Split and no string per field.

Create `AdventureWorks.Core/PriceFeed.cs` (44 lines).

PriceFeed.cs, part 1 of 2 (29 lines):

```csharp
using System.Globalization;
using System.Text;

namespace AdventureWorks.Core;

// Chapter 17: checks a price feed of "sku,name,price" lines and
// counts its records. Strict UTF-8, at most 1,024 characters a
// line and 1,000 records; empty lines are skipped.
public static class PriceFeed
{
    public const int MaxLine = 1024, MaxRecords = 1000;
    static readonly UTF8Encoding Strict = new(false, true);

    public static async Task<int> CountAsync(Stream body,
        CancellationToken ct)
    {
        using var reader = new StreamReader(body, Strict);
        var records = 0;
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (line.Length == 0) continue;
            if (++records > MaxRecords)
                throw new FormatException("Over 1,000 records.");
            if (line.Length > MaxLine || !IsRecord(line))
                throw new FormatException(
                    $"Record {records} is not sku,name,price.");
        }
        return records;
    }
```

PriceFeed.cs, part 2 of 2 (14 lines):

```csharp
    // Three fields, no quotes, and a positive price, read through
    // spans over the line: no array and no string per field.
    public static bool IsRecord(ReadOnlySpan<char> line)
    {
        var first = line.IndexOf(',');
        var last = line.LastIndexOf(',');
        if (first <= 0 || last == first || line.Contains('"') ||
            line[(first + 1)..last].Contains(','))
            return false;
        return decimal.TryParse(line[(last + 1)..],
            NumberStyles.Number, CultureInfo.InvariantCulture,
            out var price) && price > 0;
    }
}
```

### Step 2: The import endpoint

Admin only, with Chapter 9's antiforgery check like the other Admin writes. RequestSizeLimit caps this route's body at 64 KB. The limit applies when the endpoint reads the body: with a Content-Length over 64 KB the first read throws BadHttpRequestException before any byte reaches the code, and a chunked body throws once the read passes 64 KB. The endpoint catches it and answers its own 413 Problem Details, so nothing reaches the developer exception page. A feed the parser refuses is 400 with the reason.

Create `AdventureWorks/ProductImport.cs` (50 lines).

ProductImport.cs, part 1 of 2 (22 lines):

```csharp
using System.Text;
using AdventureWorks.Authorization;
using AdventureWorks.Core;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace AdventureWorks;

// Chapter 17: the Admin posts a price feed as text. The app
// reads it as a stream, checks and counts it, and saves nothing.
// A body over 64 KB is 413.
public static class ProductImport
{
    public static IEndpointRouteBuilder MapProductImport(
        this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/products/import", ImportAsync)
            .RequireAuthorization(StorePolicies.AdminOnly)
            .WithMetadata(
                new RequestSizeLimitAttribute(64 * 1024));
        return app;
    }
```

ProductImport.cs, part 2 of 2 (27 lines):

```csharp
    static async Task<IResult> ImportAsync(HttpContext context,
        IAntiforgery antiforgery, CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(context))
            return Results.BadRequest();
        try
        {
            var records = await PriceFeed.CountAsync(
                context.Request.Body, ct);
            return Results.Ok(new { Records = records });
        }
        // Kestrel throws when a read passes 64 KB: at the first
        // read with Content-Length, later for a chunked body.
        catch (BadHttpRequestException tooLarge)
            when (tooLarge.StatusCode == 413)
        {
            return Results.Problem("The feed is over 64 KB.",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }
        catch (Exception ex) when (ex is FormatException
            or DecoderFallbackException)
        {
            return Results.Problem(ex.Message, statusCode:
                StatusCodes.Status400BadRequest);
        }
    }
}
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.MapInventoryEndpoints();`.

```csharp
app.MapProductImport();
```

### Step 3: A benchmark project

BenchmarkDotNet runs each method many times in a separate process and reports its time and, with MemoryDiagnoser, what it allocates. Use 0.16.0-preview.2: 0.15.8 does not recognize .NET 11, and its out-of-process jobs stop with NotImplementedException before any benchmark runs (accuracy 17.1). The project references Core only.

Run this from the working folder.

```shell
dotnet new console -o AdventureWorks.Benchmarks
```

It prints:

```text
The template "Console App" was created successfully.
Processing post-creation actions...
Restoring AdventureWorks.Benchmarks\AdventureWorks.Benchmarks.csproj:
  Determining projects to restore...
  Restored AdventureWorks.Benchmarks\AdventureWorks.Benchmarks.csproj (in 90 ms).
Restore succeeded.
```

Run this from the working folder.

```shell
dotnet sln add AdventureWorks.Benchmarks
```

It prints:

```text
Project `AdventureWorks.Benchmarks\AdventureWorks.Benchmarks.csproj` added to the solution.
```

Run this from the working folder.

```shell
dotnet add AdventureWorks.Benchmarks reference AdventureWorks.Core
```

It prints:

```text
Reference `..\AdventureWorks.Core\AdventureWorks.Core.csproj` added to the project.
```

Run this from the working folder. The backtick at the end of a line continues the command on the next line in PowerShell.

```powershell
dotnet add AdventureWorks.Benchmarks package `
    BenchmarkDotNet@0.16.0-preview.2
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'BenchmarkDotNet' into project 'AdventureWorks.Benchmarks\AdventureWorks.Benchmarks.csproj'.
info : Restoring packages for AdventureWorks.Benchmarks\AdventureWorks.Benchmarks.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.11.42.14/vulnerability.update.json
info : Package 'BenchmarkDotNet' is compatible with all the specified frameworks in project 'AdventureWorks.Benchmarks\AdventureWorks.Benchmarks.csproj'.
info : PackageReference for package 'BenchmarkDotNet' version '0.16.0-preview.2' added to file 'AdventureWorks.Benchmarks\AdventureWorks.Benchmarks.csproj'.
info : Generating MSBuild file AdventureWorks.Benchmarks\obj\AdventureWorks.Benchmarks.csproj.nuget.g.props.
(3 more lines)
```

Replace the whole of `AdventureWorks.Benchmarks/Program.cs` with the following.

```csharp
using BenchmarkDotNet.Running;

// Runs the benchmarks that --filter selects.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
    .Run(args);
```

Create `AdventureWorks.Benchmarks/PriceFeedBenchmarks.cs` (39 lines).

```csharp
using System.Globalization;
using AdventureWorks.Core;
using BenchmarkDotNet.Attributes;

namespace AdventureWorks.Benchmarks;

// The same check two ways over 1,000 feed lines: Split makes an
// array and three strings a line; PriceFeed.IsRecord reads spans.
// Each returns its count, so the work cannot be optimized away.
[MemoryDiagnoser]
public class PriceFeedBenchmarks
{
    readonly string[] _lines = [.. Enumerable.Repeat(
        "HLM-1001,Trail Helmet,29.99", 1000)];

    [Benchmark(Baseline = true)]
    public int Split()
    {
        var valid = 0;
        foreach (var line in _lines)
        {
            var fields = line.Split(',');
            if (fields.Length == 3 && decimal.TryParse(fields[2],
                    CultureInfo.InvariantCulture, out _))
                valid++;
        }
        return valid;
    }

    [Benchmark]
    public int Span()
    {
        var valid = 0;
        foreach (var line in _lines)
            if (PriceFeed.IsRecord(line))
                valid++;
        return valid;
    }
}
```

### Step 4: Run the benchmarks

Always in Release: a Debug build measures code the JIT has not optimized, and BenchmarkDotNet refuses it. The short job runs fewer iterations than the default, enough to compare the two methods; use the default job for numbers you publish.

Run this from the working folder. The backtick at the end of a line continues the command on the next line in PowerShell. The report is under Other runs below.

```powershell
dotnet run -c Release --project AdventureWorks.Benchmarks `
    -- --filter "*PriceFeed*" --job short
```

## Program.cs at the checkpoint (90 lines)

Program.cs, part 1 of 3 (37 lines):

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
builder.Services.AddStoreObservability(builder.Configuration);
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

Program.cs, part 2 of 3 (26 lines):

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
app.UseAuthentication();
```

Program.cs, part 3 of 3 (26 lines):

```csharp
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
app.MapProductImport();
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

In Visual Studio 2026, open `AdventureWorks.slnx` and press F5; the https profile starts the same app on https://localhost:7001. A headless Visual Studio 2026 rebuild of this checkpoint (devenv /rebuild Debug) rebuilt 6 project(s) with 0 warning(s) and 0 error(s).

## Requests and real responses

`requests.http` holds these requests in this order; 9 of 9 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `POST /dev/sign-in/avery` returned **200**

  ```text
  {"userName":"avery@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqain6JzyQsly19LHhL62AoJoKCx6AnxCVG06RXOIzNE4EWrK-GiIauRr_KZ8XKdEwLLw3_Re02iAw241KvXjm8Y17Ln_eT9WsxbLJakYIQT4rk0RKOhlUplGfGL3rwThGYhy_LesEEzVCkoDe6-cfatIvIMDQeeK2mca1LTQdvB7A"}
  ```

- `POST /api/admin/products/import` returned **200**

  ```text
  {"records":3}
  ```

- `POST /api/admin/products/import` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,"detail":"Record 1 is not sku,name,price.","traceId":"00-7876f94450d3b37b29e85c8790527484-8c70da55306a12f2-03","correlationI...
  ```

- `POST /api/admin/products/import` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,"detail":"Record 1 is not sku,name,price.","traceId":"00-cbf0dcf737f9cb5823821341266b3283-ffc11fbc88dbe89d-03","correlationI...
  ```

- `POST /dev/sign-in/casey` returned **200**

  ```text
  {"userName":"casey@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqb49p9FeCk-hiMrAwZTb-xsLqvtmbopJ1Hs0vhYIzMpLyVudAaK3sTSsq9aSvfrOak1b1JATRdfQV7SXRTsnK-i72-C5MHw_k4908M-m-YYO-RraDgEVl-5Q22-OptZxwNS14tj_NAxnK3HtTMIuXmoevEUyfVryT1TW-zTs8X-Cg"}
  ```

- `POST /api/admin/products/import` returned **403**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.4","title":"Forbidden","status":403,"traceId":"00-b1944987dcbacec0fd7d588022fda26c-d217b9a37c7a5424-03","correlationId":"b1944987dcbacec0fd7d588022fda26c"}
  ```

- `POST /dev/sign-out` returned **204**

### Other runs

- `dotnet run -c Release --project AdventureWorks.Benchmarks -- --filter "*PriceFeed*" --job short` on RC3 (BenchmarkDotNet's own report, BenchmarkDotNet.Artifacts/results; the reader-path run, on this shared 8-core Hyper-V machine):

  ```text
  BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9711) (Hyper-V)
  AMD EPYC 7763 2.44GHz, 1 CPU, 8 logical and 4 physical cores
  .NET SDK 11.0.100-rc.3.26522.148
    [Host]   : .NET 11.0.0 (11.0.0-rc.3.26522.148, 11.0.26.52348), X64 RyuJIT x86-64-v3
    ShortRun : .NET 11.0.0 (11.0.0-rc.3.26522.148, 11.0.26.52348), X64 RyuJIT x86-64-v3

  Job=ShortRun  IterationCount=3  LaunchCount=1  WarmupCount=3

  | Method | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
  |------- |----------:|---------:|---------:|------:|--------:|--------:|----------:|------------:|
  | Split  | 127.40 us | 92.60 us | 5.076 us |  1.00 |    0.00 | 10.0098 |  168000 B |        1.00 |
  | Span   |  70.01 us | 83.79 us | 4.593 us |  0.55 |    0.04 |       - |         - |        0.00 |
  ```

  Split allocates 168 bytes a line (an array and three strings), Span nothing. The short job's three iterations give a wide Error column; two earlier runs on the same machine measured 123.83 and 67.22 us, and 131.52 and 58.42 us. Say "about twice as fast, and no allocation", not a precise ratio. BenchmarkDotNet ran the benchmarks out of process on the host runtime with no runtime moniker given.
- `dotnet test --project AdventureWorks.Tests`: total 7, succeeded 7 (Chapter 16's suite; this chapter adds no tests); the unit filter 3. The Visual Studio 2026 rebuild built all six projects without warnings.
- requests.http 9 of 9, and the README's curl.exe run 9 of 9: avery's three-line feed gave 200 `{"records":3}`; a quoted field and the price "free" gave 400 "Record 1 is not sku,name,price."; casey got 403.
- The limits, with bodies a .http file cannot type (`tools/scenarios/ch17_import.py`), 8 of 8: a line of exactly 1,024 characters ending in CRLF, then a last line without a newline: 200 `{"records":2}`; 1,025 characters: 400; 1,001 records: 400 "Over 1,000 records."; the bytes E2 82 (a cut-off three-byte character) at the end: 400 "Unable to translate bytes [E2][82] at index -2 from specified code page to Unicode."; 80,000 bytes with Content-Length, and 80,000 bytes chunked (80 lines of 1,000 bytes, so the record limit cannot fire first): both 413 with `{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.14","title":"Content Too Large","status":413,"detail":"The feed is over 64 KB.","traceId":"...","correlationId":"..."}`; a valid feed afterward: 200. The app log for the whole run has no `fail:` or `warn:` line: each import logs only `Selected endpoint HTTP: POST /api/admin/products/import => ImportAsync`, the user lookup and `POST /api/admin/products/import elapsed 4.4087 ms` (5.5721 ms for the chunked one). Checked.
- Review 17-S2, before the catch: both 413s came from the developer exception page. The console logged `fail: Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware[1] An unhandled exception has occurred while executing the request.` with `Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException: Request body too large. The max request body size is 65536 bytes.` and a stack trace, and the 413 body carried the exception's details.

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks.Core/PriceFeed.cs` on line 21:

```csharp
if (line.Length == 0) continue;
```

Press F5 (the https profile), then send POST https://localhost:7001/api/admin/products/import as avery with the three-line feed in requests.http. When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
line = HLM-1001,Trail Helmet,31.99 [String]
line.Length = 27 [Int32]
records = 0 [Int32]
line = JRS-2001,Cycling Jersey,54.50 [String]
line.Length = 29 [Int32]
records = 1 [Int32]
line = TNT-4001,Two-Person Tent,179.00 [String]
line.Length = 31 [Int32]
records = 2 [Int32]
```

ReadLineAsync has handed over the first line without its newline, decoded strictly as UTF-8; records is still 0 because the count rises only after the empty-line check. Step Into IsRecord to watch first and last, the two comma positions, slice the line without copying it.

## What the Activity adds

The original Activity ran the chapter's code (benchmarks, checks, limits); those runs are the checkpoint's above. This Activity adds the pooling lesson the app code does not need (Chapter17-Activity, `BufferBenchmarks.cs`): a new 1,000-character array per call against one rented from ArrayPool<char>.Shared and returned. Rent returns an array at least as long as asked for (accuracy 17.3: 1,024 for 1,000), so code must use the length it asked for, not buffer.Length, when it fills the array.

`dotnet run -c Release --project AdventureWorks.Benchmarks -- --filter "*BufferBenchmarks*" --job short` on RC3:

```text
| Method   | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| NewArray | 117.63 ns | 356.988 ns | 19.568 ns |  1.00 |    0.00 | 0.1209 |    2024 B |        1.00 |
| Rented   |  19.63 ns |   5.884 ns |  0.323 ns |  0.17 |    0.02 |      - |         - |        0.00 |
```

2,024 bytes is the array of 1,000 chars plus its object header and length. The new array's wide Error column (three iterations on a shared machine; an earlier run gave 88.72 ns) is why the delta says "several times faster and no allocation", not a ratio. Checked: the build, the six-project Visual Studio rebuild, the tests (7 of 7) and requests.http (9 of 9) as for the checkpoint.

The reader-path check rebuilt `AdventureWorks/Chapter17-Activity` from the Chapter 17 checkpoint: no differences.
The Activity adds 33 lines of code: 33 in 1 new file and 0 new or changed in existing files, plus 2 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

### Activity: a new buffer or a rented one

Work in a copy of the Chapter 17 checkpoint folder; Chapter 18 starts from the checkpoint. A second benchmark class compares a new 1,000-character array per call with one rented from ArrayPool and returned. Rent returns an array at least as long as the request (1,024 for 1,000, accuracy 17.3).

Create `AdventureWorks.Benchmarks/BufferBenchmarks.cs` (33 lines).

```csharp
using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace AdventureWorks.Benchmarks;

// Activity: a line buffer per call, new or rented. Rent returns
// an array at least as long as asked for; Return hands it back.
[MemoryDiagnoser]
public class BufferBenchmarks
{
    [Benchmark(Baseline = true)]
    public int NewArray()
    {
        var buffer = new char[1000];
        buffer[0] = 'x';
        return buffer.Length;
    }

    [Benchmark]
    public int Rented()
    {
        var buffer = ArrayPool<char>.Shared.Rent(1000);
        try
        {
            buffer[0] = 'x';
            return buffer.Length;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }
}
```

Run this from the working folder. The backtick at the end of a line continues the command on the next line in PowerShell. The report is below.

```powershell
dotnet run -c Release --project AdventureWorks.Benchmarks `
    -- --filter "*BufferBenchmarks*" --job short
```

### Activity requests and responses

9 of 9 requests in `Chapter17-Activity/requests.http` returned their commented status. They are the checkpoint's requests, with the same results; the Activity adds a benchmark, not routes.

- `POST /dev/sign-in/avery` returned **200**

  ```text
  {"userName":"avery@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqabn9PQVc7GSOu4beIMp6aFh3QPzm1yRpge8dppETt37Jw3KdAKJLMtWgb-9x4MXj0nO1B8to0CtGUT_hwcWdnc3bt6VouX_YhpNPKWjX15TLOeERA91fcbzVTqM68OC4ggkhT9ztNnLYdANsUOdUOG8ep1UASUFMXpEDMHRoaIpg"}
  ```

- `POST /api/admin/products/import` returned **200**

  ```text
  {"records":3}
  ```

- `POST /api/admin/products/import` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,"detail":"Record 1 is not sku,name,price.","traceId":"00-4cf8104e362387ae19d2f1f6ba2cfabc-16ff36a5c731ef96-03","correlationI...
  ```

- `POST /api/admin/products/import` returned **400**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,"detail":"Record 1 is not sku,name,price.","traceId":"00-66b1a6f425a0ff588252a8c67801696d-7a585cb8b2788d28-03","correlationI...
  ```

- `POST /dev/sign-in/casey` returned **200**

  ```text
  {"userName":"casey@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqZJcgFD_JwLBl6LD6ShJHqeHK0w5J2N1OO6tZz2Cf76pqy4sOCd99hH300HkqB8HURGFPBd6h1_tTDUV-JeMO-CJXHr-HSick5lOxuEAbxiYEL3_rwBdl2qFrkh8OX_7BFqa1RyfUZvTAPsImBDJNLy1Xa6AwwqmEPU6bq5xWsPYw"}
  ```

- `POST /api/admin/products/import` returned **403**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.4","title":"Forbidden","status":403,"traceId":"00-c22c329a84cbb88af56eab89cf81e142-be48277737aef3af-03","correlationId":"c22c329a84cbb88af56eab89cf81e142"}
  ```

- `POST /dev/sign-out` returned **204**

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| Api/AdventureWorks.Performance.csproj, Api/Program.cs (29 lines, printed twice) | Removed | No new web project; one route in the app (`ProductImport.cs`) and one line in its Program.cs. |
| Api/BatchProcessor.cs (96 lines; excerpts of 26 and 70) | Replaced | Core's `PriceFeed.cs` (about 45 lines): the same limits and statuses (strict UTF-8, 1,024 characters a line, 1,000 records, 413 over 64 KB, 400 for a quote), without ObjectPool<StringBuilder> or a hand-rolled line buffer: the 64 KB body limit already bounds what one request can hold, so StreamReader.ReadLineAsync is enough here. The Activity shows ArrayPool in a benchmark instead. |
| PerformanceJsonContext, BatchResult | Removed | The response is `{"records":n}`; Chapter 13's StoreJsonContext stays the app's source-generated context. |
| Benchmarks/CsvParsingBenchmarks.cs (25 lines) and Program.cs (3 lines) | Replaced | `AdventureWorks.Benchmarks/PriceFeedBenchmarks.cs` and a three-statement Program.cs; BenchmarkDotNet 0.16.0-preview.2 instead of 0.15.8 (accuracy 17.1). |
| Checks/ (companion only) | Removed | The checks are requests.http and the delta's limit runs. |

## Notes for the writers

- Accuracy 17.1: BenchmarkDotNet 0.16.0-preview.2 (the newest on nuget.org on November 3, 2026; no stable 0.16 yet). 0.15.8 knows runtimes only up to net10.0, and on a .NET 11 host its out-of-process jobs fail validation with NotImplementedException; this chapter does not use --inProcess. Leave the runtime unspecified so the benchmark runs on the host runtime.
- The benchmark project references Core only, so BenchmarkDotNet's generated project builds Core and the benchmarks, not the web app.
- Release is required: BenchmarkDotNet refuses a Debug build of the benchmarks.
- Review 17-S2: the limit applies when the endpoint reads the body, not before it runs. The endpoint and its antiforgery check run in both cases; with Content-Length the first read throws (Http1ContentLengthMessageBody.OnReadStarting), chunked the read that passes 64 KB does. ImportAsync catches BadHttpRequestException with StatusCode 413 and returns Results.Problem("The feed is over 64 KB.", statusCode: 413), so the reader sees a clean 413 and no exception in the log. Say so where the chapter explains the limit.
- StreamReader with `new UTF8Encoding(false, true)` throws DecoderFallbackException for invalid bytes instead of inserting U+FFFD; the endpoint turns it and FormatException into 400 (accuracy 17.7).
- Program.cs is 90 lines.
