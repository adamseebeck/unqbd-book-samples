# Chapter 16 delta: Observability and Diagnostics in Production Systems

Generated November 3, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter16` and the operations that rebuild it from the Chapter 15 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 16 makes the running app observable: OpenTelemetry traces and metrics for the order flow, one correlation ID shared by logs, Problem Details and traces, and readiness checks on the database and the warehouse service.

- `OrderTelemetry.cs`: the order flow's ActivitySource and Meter, both "AdventureWorks.Orders" (the meter from IMeterFactory): counter `orders.created` ({order}) and histogram `orders.processing.duration` (s, tagged operation and outcome). `MeteredOrderService` is one more link in Chapter 11's decorator chain (metered, then logging, then the service).
- `Observability.cs` (AddStoreObservability): OpenTelemetry with the service name AdventureWorks; traces from Microsoft.AspNetCore (the framework's own server spans; no ASP.NET Core instrumentation package) and AdventureWorks.Orders plus the HttpClient instrumentation, under a parent-based ratio sampler (Telemetry:SampleRatio, default 1.0); metrics from AdventureWorks.Orders, Microsoft.AspNetCore.Hosting and System.Net.Http; the exporter chosen by Telemetry:Exporter (console, otlp with Telemetry:OtlpEndpoint, or none). Two readiness checks tagged ready: "database" (AddDbContextCheck) and "inventory" (`InventoryHealthCheck`: GET /health on InventoryService through a plain named client with a one-second timeout; a timeout is Degraded, a refusal or an error status Unhealthy).
- `/health` splits into `/health/live` (no checks) and `/health/ready` (the ready checks; Degraded and Unhealthy both 503). InventoryService answers `/health`, slowed by its /control delay like the stock route.
- CorrelationIdMiddleware falls back to the request's W3C trace ID instead of a new GUID.
- Tests: `OrderTelemetryTests` reads `orders.created` through MetricCollector on the app's IMeterFactory.
- Packages: OpenTelemetry.Extensions.Hosting, OpenTelemetry.Exporter.Console, OpenTelemetry.Exporter.OpenTelemetryProtocol and OpenTelemetry.Instrumentation.Http 1.20.1; Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 11.0.0; Microsoft.Extensions.Diagnostics.Testing 10.11.0 in the tests.
- Program.cs gains one line (AddStoreObservability): 89 lines.

## Files added or changed

| File | Change |
| --- | --- |
| `AdventureWorks/OrderTelemetry.cs` | added |
| `AdventureWorks/OrderEndpoints.cs` | changed |
| `AdventureWorks/Observability.cs` | added |
| `AdventureWorks/InventoryHealthCheck.cs` | added |
| `AdventureWorks/Program.cs` | changed |
| `AdventureWorks/Properties/launchSettings.json` | changed |
| `AdventureWorks/StatusEndpoints.cs` | changed |
| `InventoryService/Program.cs` | changed |
| `AdventureWorks/CorrelationIdMiddleware.cs` | changed |
| `AdventureWorks.Tests/Integration/OrderTelemetryTests.cs` | added |
| (command) `dotnet add AdventureWorks package OpenTelemetry.Extensions.Hosting@1.20.1` | creates or edits project files |
| (command) `dotnet add AdventureWorks package OpenTelemetry.Exporter.Console@1.20.1` | creates or edits project files |
| (command) `dotnet add AdventureWorks package OpenTelemetry.Exporter.OpenTelemetryProtocol@1.20.1` | creates or edits project files |
| (command) `dotnet add AdventureWorks package OpenTelemetry.Instrumentation.Http@1.20.1` | creates or edits project files |
| (command) `dotnet add AdventureWorks package --version 11.0.0 Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | creates or edits project files |
| (command) `dotnet add AdventureWorks.Tests package Microsoft.Extensions.Diagnostics.Testing@10.11.0` | creates or edits project files |

The reader types 282 lines of code: 220 in 4 new files and 62 new or changed in existing files, plus 13 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 96 of 96 files identical and no differences.

### Step 1: The order flow's own signals

OrderTelemetry owns the order flow's ActivitySource and Meter, both named AdventureWorks.Orders: a counter of created orders and a histogram of how long each service call took, in seconds. The meter comes from IMeterFactory, so it belongs to the app's container and a test can read it. MeteredOrderService is one more decorator in Chapter 11's chain: it starts a span per call, records the duration with two bounded tags (the operation and the outcome) and counts each Created outcome. Order numbers and IDs go on the span, never on a metric: each distinct tag value is another time series.

Create `AdventureWorks/OrderTelemetry.cs` (74 lines).

OrderTelemetry.cs, part 1 of 2 (37 lines):

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;
using AdventureWorks.Core;

namespace AdventureWorks;

// The order flow's own signals: a span per service call, a count
// of created orders and how long each call took. OpenTelemetry
// (Observability.cs) collects them by these names.
public sealed class OrderTelemetry
{
    public const string Name = "AdventureWorks.Orders";

    public ActivitySource Source { get; } = new(Name);
    public Counter<long> Created { get; }
    public Histogram<double> Duration { get; }

    public OrderTelemetry(IMeterFactory meters)
    {
        var meter = meters.Create(Name);
        Created = meter.CreateCounter<long>(
            "orders.created", unit: "{order}");
        Duration = meter.CreateHistogram<double>(
            "orders.processing.duration", unit: "s");
    }
}

// Chapter 11's decorator chain, one link longer: this one wraps
// the logging decorator and records the signals.
public sealed class MeteredOrderService(IOrderService inner,
    OrderTelemetry telemetry) : IOrderService
{
    public Task<OrderResult> CreateAsync(string ownerId,
        CreateOrderRequest request,
        CancellationToken ct = default) =>
        TrackAsync("create", () =>
            inner.CreateAsync(ownerId, request, ct));
```

OrderTelemetry.cs, part 2 of 2 (36 lines):

```csharp
    public Task<OrderResult> CancelAsync(string ownerId, int id,
        CancellationToken ct = default) =>
        TrackAsync("cancel", () =>
            inner.CancelAsync(ownerId, id, ct));

    public Task<OrderResult> UpdateNotesAsync(string ownerId,
        int id, string? notes, Guid version,
        CancellationToken ct = default) =>
        TrackAsync("update", () => inner.UpdateNotesAsync(
            ownerId, id, notes, version, ct));

    public Task<OrderPage> ListAsync(string ownerId, int page,
        int pageSize, CancellationToken ct = default) =>
        inner.ListAsync(ownerId, page, pageSize, ct);

    async Task<OrderResult> TrackAsync(string operation,
        Func<Task<OrderResult>> call)
    {
        using var span = telemetry.Source.StartActivity(
            "orders." + operation);
        var start = Stopwatch.GetTimestamp();
        var result = await call();
        TagList tags = new()
        {
            { "operation", operation },
            { "outcome", result.Outcome.ToString() }
        };
        telemetry.Duration.Record(
            Stopwatch.GetElapsedTime(start).TotalSeconds, tags);
        if (result.Outcome == OrderOutcome.Created)
            telemetry.Created.Add(1);
        span?.SetTag("order.outcome", result.Outcome.ToString());
        span?.SetTag("order.number", result.Order?.OrderNumber);
        return result;
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
        // The service, wrapped by its logging decorator, wrapped
        // by the metrics and tracing one (Chapter 16).
        services.AddScoped<OrderService>();
        services.AddScoped<IOrderService>(provider =>
            new MeteredOrderService(
                new LoggingOrderService(
                    provider.GetRequiredService<OrderService>(),
                    provider.GetRequiredService<
                        ILogger<LoggingOrderService>>()),
                provider.GetRequiredService<OrderTelemetry>()));
```

### Step 2: OpenTelemetry and the readiness checks

Four OpenTelemetry packages: the hosting integration, the console and OTLP exporters, and the HttpClient instrumentation. ASP.NET Core in .NET 11 already emits server spans with the semantic-convention attributes from its Microsoft.AspNetCore source, so no ASP.NET Core instrumentation package is needed. AddDbContextCheck comes from its own package.

Run this from the working folder. The backtick at the end of a line continues the command on the next line in PowerShell.

```powershell
dotnet add AdventureWorks package `
    OpenTelemetry.Extensions.Hosting@1.20.1
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'OpenTelemetry.Extensions.Hosting' into project 'AdventureWorks\AdventureWorks.csproj'.
info : Restoring packages for AdventureWorks\AdventureWorks.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.11.42.14/vulnerability.update.json
info : Package 'OpenTelemetry.Extensions.Hosting' is compatible with all the specified frameworks in project 'AdventureWorks\AdventureWorks.csproj'.
info : PackageReference for package 'OpenTelemetry.Extensions.Hosting' version '1.20.1' added to file 'AdventureWorks\AdventureWorks.csproj'.
info : Generating MSBuild file AdventureWorks\obj\AdventureWorks.csproj.nuget.g.props.
(3 more lines)
```

Run this from the working folder.

```powershell
dotnet add AdventureWorks package `
    OpenTelemetry.Exporter.Console@1.20.1
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'OpenTelemetry.Exporter.Console' into project 'AdventureWorks\AdventureWorks.csproj'.
info : Restoring packages for AdventureWorks\AdventureWorks.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.11.42.14/vulnerability.update.json
info : Package 'OpenTelemetry.Exporter.Console' is compatible with all the specified frameworks in project 'AdventureWorks\AdventureWorks.csproj'.
info : PackageReference for package 'OpenTelemetry.Exporter.Console' version '1.20.1' added to file 'AdventureWorks\AdventureWorks.csproj'.
info : Writing assets file to disk. Path: AdventureWorks\obj\project.assets.json
(1 more lines)
```

Run this from the working folder.

```powershell
dotnet add AdventureWorks package `
    OpenTelemetry.Exporter.OpenTelemetryProtocol@1.20.1
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'OpenTelemetry.Exporter.OpenTelemetryProtocol' into project 'AdventureWorks\AdventureWorks.csproj'.
info : Restoring packages for AdventureWorks\AdventureWorks.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.11.42.14/vulnerability.update.json
info : Package 'OpenTelemetry.Exporter.OpenTelemetryProtocol' is compatible with all the specified frameworks in project 'AdventureWorks\AdventureWorks.csproj'.
info : PackageReference for package 'OpenTelemetry.Exporter.OpenTelemetryProtocol' version '1.20.1' added to file 'AdventureWorks\AdventureWorks.csproj'.
info : Writing assets file to disk. Path: AdventureWorks\obj\project.assets.json
(1 more lines)
```

Run this from the working folder.

```powershell
dotnet add AdventureWorks package `
    OpenTelemetry.Instrumentation.Http@1.20.1
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'OpenTelemetry.Instrumentation.Http' into project 'AdventureWorks\AdventureWorks.csproj'.
info : Restoring packages for AdventureWorks\AdventureWorks.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.11.42.14/vulnerability.update.json
info : Package 'OpenTelemetry.Instrumentation.Http' is compatible with all the specified frameworks in project 'AdventureWorks\AdventureWorks.csproj'.
info : PackageReference for package 'OpenTelemetry.Instrumentation.Http' version '1.20.1' added to file 'AdventureWorks\AdventureWorks.csproj'.
info : Writing assets file to disk. Path: AdventureWorks\obj\project.assets.json
(1 more lines)
```

Run this from the working folder. The package name fills the second line, so the version comes first.

```powershell
dotnet add AdventureWorks package --version 11.0.0 `
Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore' into project 'AdventureWorks\AdventureWorks.csproj'.
info : Restoring packages for AdventureWorks\AdventureWorks.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.11.42.14/vulnerability.update.json
info : Package 'Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore' is compatible with all the specified frameworks in project 'AdventureWorks\AdventureWorks.csproj'.
info : PackageReference for package 'Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore' version '11.0.0' added to file 'AdventureWorks\AdventureWorks.csproj'.
info : Writing assets file to disk. Path: AdventureWorks\obj\project.assets.json
(1 more lines)
```

Observability.cs registers OrderTelemetry, subscribes traces to the Microsoft.AspNetCore and AdventureWorks.Orders sources (AddSource) and metrics to three meters (AddMeter; the two registrations are separate), and picks the exporter from configuration: `Telemetry:Exporter` console writes to the app's window, otlp sends to `Telemetry:OtlpEndpoint`, and anything else collects without exporting. It also registers the two readiness checks, each tagged ready.

Create `AdventureWorks/Observability.cs` (72 lines).

Observability.cs, part 1 of 3 (6 lines):

```csharp
using AdventureWorks.Infrastructure;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AdventureWorks;
```

Observability.cs, part 2 of 3 (40 lines):

```csharp
public static class Observability
{
    // Traces and metrics through OpenTelemetry, and the readiness
    // checks. Telemetry:Exporter picks where the data goes:
    // "console" (this window), "otlp" (Telemetry:OtlpEndpoint, a
    // collector), or nothing, the default. Telemetry:SampleRatio
    // keeps that share of new traces (1.0, all, by default).
    public static IServiceCollection AddStoreObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<OrderTelemetry>();
        var exporter = configuration["Telemetry:Exporter"];
        var endpoint = configuration["Telemetry:OtlpEndpoint"];
        var ratio = configuration.GetValue(
            "Telemetry:SampleRatio", 1.0);
        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService("AdventureWorks"))
            .WithTracing(tracing =>
            {
                tracing.SetSampler(new ParentBasedSampler(
                        new TraceIdRatioBasedSampler(ratio)))
                    .AddSource("Microsoft.AspNetCore",
                        OrderTelemetry.Name)
                    .AddHttpClientInstrumentation();
                if (exporter == "console")
                    tracing.AddConsoleExporter();
                else if (exporter == "otlp")
                    tracing.AddOtlpExporter(o =>
                        o.Endpoint = new Uri(endpoint!));
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(OrderTelemetry.Name,
                    "Microsoft.AspNetCore.Hosting",
                    "System.Net.Http");
                // Every 30 s, so a request's spans stay on screen
                // between two dumps of every metric.
                if (exporter == "console")
```

Observability.cs, part 3 of 3 (25 lines):

```csharp
                    metrics.AddConsoleExporter((_, r) =>
                        r.PeriodicExportingMetricReaderOptions
                            .ExportIntervalMilliseconds = 30_000);
                else if (exporter == "otlp")
                    metrics.AddOtlpExporter(o =>
                        o.Endpoint = new Uri(endpoint!));
            });
        // Ready means the app can serve: its database answers and
        // the warehouse service is reachable within a second.
        services.AddHealthChecks()
            .AddDbContextCheck<AdventureWorksDbContext>(
                "database", tags: ["ready"])
            .AddCheck<InventoryHealthCheck>(
                "inventory", tags: ["ready"]);
        var inventory = new Uri(configuration["Inventory:BaseUrl"]
            ?? "http://127.0.0.1:42132/");
        services.AddHttpClient(InventoryHealthCheck.Client,
            http =>
            {
                http.BaseAddress = inventory;
                http.Timeout = TimeSpan.FromSeconds(1);
            });
        return services;
    }
}
```

The inventory check calls the warehouse service's /health route on a plain named client with a one-second timeout, not through the resilience handler, so a probe never waits for retries. A timeout is Degraded; a refused connection or an error status is Unhealthy.

Create `AdventureWorks/InventoryHealthCheck.cs` (41 lines).

InventoryHealthCheck.cs, part 1 of 2 (12 lines):

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AdventureWorks;

// Readiness of the warehouse service: one plain call to its
// /health route, without the resilience handler's retries. A slow
// answer is Degraded, a failed one Unhealthy; /health/ready turns
// both into 503.
public sealed class InventoryHealthCheck(
    IHttpClientFactory clients) : IHealthCheck
{
    public const string Client = "inventory-health";
```

InventoryHealthCheck.cs, part 2 of 2 (28 lines):

```csharp
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var http = clients.CreateClient(Client);
        try
        {
            using var response = await http.GetAsync(
                "health", cancellationToken);
            var status = (int)response.StatusCode;
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy(
                    $"Inventory answered {status}.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded(
                "Inventory did not answer within a second.");
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Unhealthy(
                "Inventory is unreachable.", ex);
        }
    }
}
```

In `AdventureWorks/Program.cs`, add these lines after the line `builder.Services.AddStorePerformance(builder.Configuration);`.

```csharp
builder.Services.AddStoreObservability(builder.Configuration);
```

A second launch profile, telemetry, turns on the console exporter and JSON logs with scopes through environment variables, the same settings as `--Telemetry:Exporter=console --Logging:Console:FormatterName=json --Logging:Console:FormatterOptions:IncludeScopes=true`. Visual Studio's F5 with the telemetry profile and `dotnet run --project AdventureWorks --launch-profile telemetry` then run the app the same way, and the https profile stays quiet.

In `AdventureWorks/Properties/launchSettings.json`, replace these lines. The last lines of the file: the https profile ends, and the telemetry profile follows it.

```json
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

With these:

```json
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "telemetry": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "applicationUrl":
        "https://localhost:7001;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "Telemetry__Exporter": "console",
        "Logging__Console__FormatterName": "json",
        "Logging__Console__FormatterOptions__IncludeScopes":
          "true"
      }
    }
  }
}
```

### Step 3: Live and ready

Chapter 1's /health splits in two. /health/live runs no checks: it answers while the process can answer, and an orchestrator restarts the app when it stops. /health/ready runs the checks tagged ready: the database and the warehouse. By default ASP.NET Core maps Degraded to 200, so ResultStatusCodes maps it to 503 here; a slow warehouse takes the instance out of rotation without restarting it. Chapter 18's container and Kubernetes files use both routes.

In `AdventureWorks/StatusEndpoints.cs`, add these lines above the line `namespace AdventureWorks;`.

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
```

In `AdventureWorks/StatusEndpoints.cs`, replace these lines. In MapStatusEndpoints.

```csharp
        app.MapHealthChecks("/health").AllowAnonymous();
```

With these:

```csharp
        // Live: the process answers; no checks run.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        }).AllowAnonymous();
        // Ready: the checks tagged "ready" pass. Degraded maps to
        // 200 by default; here it means not ready (503).
        var ready = new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        };
        ready.ResultStatusCodes[HealthStatus.Degraded] =
            StatusCodes.Status503ServiceUnavailable;
        app.MapHealthChecks("/health/ready", ready)
            .AllowAnonymous();
```

In `InventoryService/Program.cs`, add these lines above the line `var control = app.MapGroup("/control");`. The warehouse service answers its own health route.

```csharp
// For the store's readiness check (Chapter 16). The delay set
// through /control slows it too.
app.MapGet("/health", async (
    CancellationToken cancellationToken) =>
{
    if (faults.DelayMs > 0)
        await Task.Delay(faults.DelayMs, cancellationToken);
    return "Healthy";
});
```

### Step 4: One ID in logs, Problem Details and traces

Chapter 3's middleware generated a new GUID when a request had no usable X-Correlation-ID. It now reuses the request's W3C trace ID instead, so the same 32-character value appears in the response header, the log scope, Problem Details and the trace. A caller's valid header still wins.

In `AdventureWorks/CorrelationIdMiddleware.cs`, add these lines above the line `namespace AdventureWorks;`.

```csharp
using System.Diagnostics;
```

In `AdventureWorks/CorrelationIdMiddleware.cs`, replace these lines.

```csharp
        var id = IsValid(supplied)
            ? supplied : Guid.NewGuid().ToString("N");
```

With these:

```csharp
        // Without a usable header, reuse the request's trace ID,
        // so logs, Problem Details and traces share one ID.
        var id = IsValid(supplied) ? supplied
            : Activity.Current?.TraceId.ToHexString()
                ?? Guid.NewGuid().ToString("N");
```

### Step 5: Test the counter

Microsoft.Extensions.Diagnostics.Testing supplies MetricCollector, which listens to one instrument of a meter created by the app's own IMeterFactory. The test places an order through the factory from Chapter 15 and reads one measurement, with no exporter involved.

Run this from the working folder.

```powershell
dotnet add AdventureWorks.Tests package `
    Microsoft.Extensions.Diagnostics.Testing@10.11.0
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.Extensions.Diagnostics.Testing' into project 'AdventureWorks.Tests\AdventureWorks.Tests.csproj'.
info : Restoring packages for AdventureWorks.Tests\AdventureWorks.Tests.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.11.42.14/vulnerability.update.json
info : Package 'Microsoft.Extensions.Diagnostics.Testing' is compatible with all the specified frameworks in project 'AdventureWorks.Tests\AdventureWorks.Tests.csproj'.
info : PackageReference for package 'Microsoft.Extensions.Diagnostics.Testing' version '10.11.0' added to file 'AdventureWorks.Tests\AdventureWorks.Tests.csproj'.
info : Generating MSBuild file AdventureWorks.Tests\obj\AdventureWorks.Tests.csproj.nuget.g.props.
(3 more lines)
```

Create `AdventureWorks.Tests/Integration/OrderTelemetryTests.cs` (33 lines).

```csharp
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Xunit;

namespace AdventureWorks.Tests.Integration;

// Chapter 16: the counter a dashboard would read, checked through
// the app's own IMeterFactory, so no exporter is needed.
[Trait("Category", "Integration")]
public sealed class OrderTelemetryTests(StoreFactory factory)
    : IClassFixture<StoreFactory>
{
    [Fact]
    public async Task APlacedOrderIsCounted()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateCustomerAsync(ct);
        using var created = new MetricCollector<long>(
            factory.Services.GetRequiredService<IMeterFactory>(),
            OrderTelemetry.Name, "orders.created");

        var response = await client.PostAsJsonAsync("/api/orders",
            new { Items = new[] { new { ProductId = 2,
                Quantity = 1 } } }, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, created.GetMeasurementSnapshot()
            .EvaluateAsCounter());
    }
}
```

Run this from the working folder.

```shell
dotnet test --project AdventureWorks.Tests
```

It prints:

```text
Running tests from AdventureWorks.Tests\bin\Debug\net11.0\AdventureWorks.Tests.dll (net11.0|x64)
warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]
      Failed to determine the https port for redirect.
AdventureWorks.Tests\bin\Debug\net11.0\AdventureWorks.Tests.dll (net11.0|x64) passed [+7/x0/?0] (8s 251ms)
Test run summary: Passed!
  total: 7
  failed: 0
  succeeded: 7
  skipped: 0
  duration: 9s 932ms
```

## Program.cs at the checkpoint (89 lines)

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

In Visual Studio 2026, open `AdventureWorks.slnx` and press F5; the https profile starts the same app on https://localhost:7001. A headless Visual Studio 2026 rebuild of this checkpoint (devenv /rebuild Debug) rebuilt 5 project(s) with 0 warning(s) and 0 error(s).

## Requests and real responses

`requests.http` holds these requests in this order; 18 of 18 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `GET /health/live` returned **200**

  ```text
  Healthy
  ```

- `GET /health/ready` returned **200**

  ```text
  Healthy
  ```

- `GET /api/catalog` returned **200** (X-Correlation-ID: 9db00d5aa5421a97df8520266e4d4cdb)

  ```text
  {"items":[{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":...
  ```

- `GET /api/catalog` returned **200** (X-Correlation-ID: order-lab-1)

  ```text
  {"items":[{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":...
  ```

- `GET /api/catalog` returned **200** (X-Correlation-ID: 74df6d1457d368c4eadd96d71b718b89)

  ```text
  {"items":[{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":...
  ```

- `POST http://127.0.0.1:42132/control/delay/1500` returned **204**

- `GET /health/ready` returned **503**

  ```text
  Degraded
  ```

- `GET /health/live` returned **200**

  ```text
  Healthy
  ```

- `POST http://127.0.0.1:42132/control/reset` returned **204**

- `GET /health/ready` returned **200**

  ```text
  Healthy
  ```

- `POST /dev/sign-in/casey` returned **200**

  ```text
  {"userName":"casey@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqY2Odbob5pvG8wv6bfQMdpoSxwVXqo2yQTyuoqQ0wMNsNkqyvcSKEg-RZj6u9VVbgQvtyiXPXpTRY4nVrvaCLsFA9EUCWoOZM2nZZRivLa1m1NZynaoFlgqwVhEQts3nPsSWunR2xTyO8Fs4mgUrrGNWWIPiaSeMsbzCSHvbdIzeA"}
  ```

- `POST /api/orders` returned **201** (Location: https://localhost:7001/api/orders/13)

  ```text
  {"id":13,"orderNumber":"AW-0013","status":"Pending","archived":false,"total":49.99,"notes":null,"items":[{"productId":2,"quantity":1,"unitPrice":49.99}]}
  ```

- `DELETE /api/orders/13` returned **204**

- `POST /dev/sign-in/avery` returned **200**

  ```text
  {"userName":"avery@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqZlICNOFqsSduIvnK0fP6ro0EHcdYAGWOXhBVZ_luge52IAICnP3XzdulV2CAKQ5m1At4UnOGNymzSDzYo8RJUaZPInUoz1xr5zc5E9Okac9lIwyC8fonLrGhstkIKjw81sSssNcUk4FMgI_Of_y-G2rQYtI3Fumo08e_vrSCrw2A"}
  ```

- `DELETE /api/admin/orders/13` returned **204**

- `POST /dev/sign-out` returned **204**

### Other runs

- `dotnet test --project AdventureWorks.Tests`: total 7, succeeded 7 (OrderTelemetryTests adds one); the unit filter 3. The Visual Studio 2026 rebuild built all five projects without warnings.
- requests.http 18 of 18, and the README's curl.exe run 18 of 18: live and ready 200 ("Healthy"); no X-Correlation-ID, or "bad value", came back as a 32-character trace ID (for example 7643067327491606d8b997321be28070), and "order-lab-1" came back unchanged; with InventoryService slowed to 1.5 s through /control/delay, ready answered 503 "Degraded" after the one-second timeout while live answered 200; after /control/reset, ready answered 200 again.
- InventoryService stopped (nothing on 127.0.0.1:42132): live 200 "Healthy", ready 503 "Degraded". Accuracy 16.1 holds here: on Windows a refused loopback connection takes about two seconds, so the one-second timeout fires first and the check reports Degraded, not Unhealthy; Linux normally refuses at once (Unhealthy). Both are 503.
- With `--Telemetry:Exporter=console`, one order and a 32-second wait (the metric export runs every 30 seconds) printed the spans and metrics (excerpt; the orders.create span is a child of the framework's POST /api/orders/ server span, same trace ID, ParentSpanId 6a2a7943e6b37a92 = its SpanId):

  ```text
  Activity.TraceId:            3a3ebd9f92d038dcc8d5b7347588dbf5
  Activity.ParentSpanId:       6a2a7943e6b37a92
  Activity.DisplayName:        orders.create
  Activity.Kind:               Internal
  Activity.Tags:
      order.outcome: Created
      order.number: AW-0013
  Instrumentation scope (ActivitySource):
      Name: AdventureWorks.Orders

  Metric Name: orders.created, Unit: {order}, Metric Type: LongSum
  Instrumentation scope (Meter):
  	Name: AdventureWorks.Orders
  Value: 1

  Metric Name: orders.processing.duration, Unit: s, Metric Type: Histogram
  (...] operation: create outcome: Created
  Value: Sum: 0.3037356 Count: 1 Min: 0.3037356 Max: 0.3037356
  ```

  The server span carried http.request.method, url.scheme, url.path, http.route, http.response.status_code, server.address, server.port and network.protocol.version with no ASP.NET Core instrumentation package (accuracy 16.2, 16.3); the hosting meter's http.server.request.duration appeared too. Checked.
- The telemetry launch profile (review 16-S2), `dotnet run --project AdventureWorks --no-build --launch-profile telemetry` with no other argument, gave the same output as the three command-line arguments: the seven spans as their requests ended (POST /dev/sign-in/{name}, GET /api/csrf, orders.create, POST /api/orders/, orders.cancel, DELETE /api/orders/{id:int}, GET /health/live), JSON log lines whose Scopes carry `"CorrelationId":"order-lab-1"` (9 of them for the order), and one metric export 30 seconds after startup: 4 metrics, about 113 lines, then nothing more until the next 30 seconds. Before the change the exporter reprinted every metric every 2 seconds, about 195 lines each time after Step 3's requests (review 16-S1), so a request's spans scrolled away within seconds. Checked.
- JSON logs by configuration only (`--Logging:Console:FormatterName=json --Logging:Console:FormatterOptions:IncludeScopes=true`): a request with X-Correlation-ID: order-lab-1 logged lines whose Scopes carry `"CorrelationId":"order-lab-1"`. Checked.
- The delete-race probe from Chapter 11 passed against this checkpoint too (24 of 24), now through MeteredOrderService.

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks/OrderTelemetry.cs` on line 66:

```csharp
telemetry.Duration.Record(
```

Press F5 (the https profile), then send POST https://localhost:7001/api/orders as casey (requests.http, the order it places). When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
operation = create [String]
result.Outcome = Created [OrderOutcome]
span?.DisplayName = orders.create [String]
span?.Source.Name = AdventureWorks.Orders [String]
tags.Count = 2 [Int32]
```

The decorator has the result of the whole chain below it (logging, then the service) and is about to record the call: the span is the one it started for this call, a child of the framework's server span for the request, and the two tags are the only dimensions the histogram gets. Step Over the next lines to see the counter add 1 for a Created outcome and the span take the order number.

## What the Activity adds

The original Activity added an order-attempts counter; on this app it is the next counter on the same meter (Chapter16-Activity). OrderTelemetry gains `Cancelled`, `orders.cancelled` ({order}), and MeteredOrderService adds 1 for each Cancelled outcome. No registration changes: OpenTelemetry already listens to the AdventureWorks.Orders meter. `CancelledCounterTests` places an order, cancels it through its Location, and reads 1 from MetricCollector.

Checked: total 8, succeeded 8; the unit filter 3; with `--Telemetry:Exporter=console` the log shows orders.cancelled next to orders.created.

The reader-path check rebuilt `AdventureWorks/Chapter16-Activity` from the Chapter 16 checkpoint: no differences.
The Activity adds 40 lines of code: 35 in 1 new file and 5 new or changed in existing files, plus 1 command line. Blank lines in listings are counted; deleted lines and files that a command writes are not.

### Activity: count cancelled orders

Work in a copy of the Chapter 16 checkpoint folder; Chapter 17 starts from the checkpoint. A second counter on the same meter, orders.cancelled, counts each Cancelled outcome. It needs no new registration: OpenTelemetry already listens to the AdventureWorks.Orders meter, so the console exporter shows it next to orders.created.

In `AdventureWorks/OrderTelemetry.cs`, add these lines after the line `public Histogram<double> Duration { get; }`.

```csharp
    public Counter<long> Cancelled { get; }
```

In `AdventureWorks/OrderTelemetry.cs`, add these lines after the line `"orders.processing.duration", unit: "s");`. In the constructor.

```csharp
        Cancelled = meter.CreateCounter<long>(
            "orders.cancelled", unit: "{order}");
```

In `AdventureWorks/OrderTelemetry.cs`, add these lines after the line `telemetry.Created.Add(1);`. In TrackAsync.

```csharp
        if (result.Outcome == OrderOutcome.Cancelled)
            telemetry.Cancelled.Add(1);
```

Create `AdventureWorks.Tests/Integration/CancelledCounterTests.cs` (35 lines).

```csharp
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Xunit;

namespace AdventureWorks.Tests.Integration;

// Activity: the counter of cancelled orders.
[Trait("Category", "Integration")]
public sealed class CancelledCounterTests(StoreFactory factory)
    : IClassFixture<StoreFactory>
{
    [Fact]
    public async Task ACancelledOrderIsCounted()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateCustomerAsync(ct);
        using var cancelled = new MetricCollector<long>(
            factory.Services.GetRequiredService<IMeterFactory>(),
            OrderTelemetry.Name, "orders.cancelled");
        var created = await client.PostAsJsonAsync("/api/orders",
            new { Items = new[] { new { ProductId = 2,
                Quantity = 1 } } }, ct);

        var response = await client.DeleteAsync(
            created.Headers.Location, ct);

        Assert.Equal(HttpStatusCode.NoContent,
            response.StatusCode);
        Assert.Equal(1, cancelled.GetMeasurementSnapshot()
            .EvaluateAsCounter());
    }
}
```

Run this from the working folder.

```shell
dotnet test --project AdventureWorks.Tests
```

It prints:

```text
Running tests from AdventureWorks.Tests\bin\Debug\net11.0\AdventureWorks.Tests.dll (net11.0|x64)
warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]
      Failed to determine the https port for redirect.
AdventureWorks.Tests\bin\Debug\net11.0\AdventureWorks.Tests.dll (net11.0|x64) passed [+8/x0/?0] (8s 006ms)
Test run summary: Passed!
  total: 8
  failed: 0
  succeeded: 8
  skipped: 0
  duration: 10s 011ms
```

### Activity requests and responses

18 of 18 requests in `Chapter16-Activity/requests.http` returned their commented status. They are the checkpoint's requests, with the same results; the Activity adds a counter and a test.

- `GET /health/live` returned **200**

  ```text
  Healthy
  ```

- `GET /health/ready` returned **200**

  ```text
  Healthy
  ```

- `GET /api/catalog` returned **200** (X-Correlation-ID: 814354173fc72bfe56a331340d846a7d)

  ```text
  {"items":[{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":...
  ```

- `GET /api/catalog` returned **200** (X-Correlation-ID: order-lab-1)

  ```text
  {"items":[{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":...
  ```

- `GET /api/catalog` returned **200** (X-Correlation-ID: 95bf0e032d4d2efdce8d821372f4debd)

  ```text
  {"items":[{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":...
  ```

- `POST http://127.0.0.1:42132/control/delay/1500` returned **204**

- `GET /health/ready` returned **503**

  ```text
  Degraded
  ```

- `GET /health/live` returned **200**

  ```text
  Healthy
  ```

- `POST http://127.0.0.1:42132/control/reset` returned **204**

- `GET /health/ready` returned **200**

  ```text
  Healthy
  ```

- `POST /dev/sign-in/casey` returned **200**

  ```text
  {"userName":"casey@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqaK8QNXI4yQtgGGBfAOeaIWNVC189xK0Vdp0wgjfrCrRzA_ENyLm5itil21GS7Jrb5K5P4V9CZQxzqCVOjQx6PdbOKffhN3_vhQfIxBJWXI0R5wmWYoXZQ9fNfkWxsg9d0Pse_PFAoqIqoBVoybP21cGO5QO7ZlkP4dlXAL84nLqw"}
  ```

- `POST /api/orders` returned **201** (Location: https://localhost:7001/api/orders/13)

  ```text
  {"id":13,"orderNumber":"AW-0013","status":"Pending","archived":false,"total":49.99,"notes":null,"items":[{"productId":2,"quantity":1,"unitPrice":49.99}]}
  ```

- `DELETE /api/orders/13` returned **204**

- `POST /dev/sign-in/avery` returned **200**

  ```text
  {"userName":"avery@adventure-works.test"}
  ```

- `GET /api/csrf` returned **200**

  ```text
  {"token":"CfDJ8B_g-SIhaa9Im_JmScWyQqawW8cZYUm_k8Jwp111T8jkDI9m-4UdM3tNwXuaRN-cy-j62EPZJAK3OCnnFkMZJ5fmY-oREWyR8ImLExyzukPcXTiaUAftjse61hsUERJB8NTRm0sgSZ9KHL6ORdcMSy9qtbKG0wW22BcQJoXdIA1OqBbI3Cuuu8ZDklFZThx-_Q"}
  ```

- `DELETE /api/admin/orders/13` returned **204**

- `POST /dev/sign-out` returned **204**

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| AdventureWorks.Observability.csproj (14 lines) | Removed | No new project; the four OpenTelemetry packages and the EF Core health-check package go into the app's project with `dotnet add AdventureWorks package`. |
| Diagnostics.cs (62 lines, printed twice) | Replaced | `OrderTelemetry.cs` (the meter, the source and MeteredOrderService), `Observability.cs` (the OpenTelemetry and health-check registrations) and `InventoryHealthCheck.cs`. OrderMetrics becomes OrderTelemetry; order.processing.duration becomes orders.processing.duration, tagged operation and outcome, not success. |
| Program.cs (76 lines, printed twice) | Replaced | One line in the app's Program.cs: `builder.Services.AddStoreObservability(builder.Configuration);`. No ClearProviders: the app keeps its logging providers; JSON console logs are a configuration switch (Logging:Console:FormatterName json, IncludeScopes true), so no code changes. |
| CorrelationIdMiddleware (a second one) | Replaced | Chapter 3's middleware (C16-1), with the trace-ID fallback. It keeps Chapter 3's IMiddleware form and its character rule (letters, digits, '-' and '_'). |
| CreateOrderRequest(Region), GET /catalog?delayMs&status on the same host | Removed | The real order flow; the dependency is Chapter 12's InventoryService on 127.0.0.1:42132, whose /control/delay slows /health too. |
| CatalogHealthCheck ("catalog") | Replaced | InventoryHealthCheck ("inventory"), so Chapter 18 can add its own check without a duplicate name. |

## Notes for the writers

- Accuracy 16.1: a closed loopback port on Windows is not refused at once; the readiness probe reports Degraded (the one-second timeout) rather than Unhealthy there, and 503 either way. The InventoryService-stopped run shows it.
- Accuracy 16.2: no OpenTelemetry.Instrumentation.AspNetCore. AddSource("Microsoft.AspNetCore") collects the framework's own server spans, which carry the semantic-convention attributes in .NET 11.
- AddSource subscribes to traces and AddMeter to metrics; they are separate lists (16.5).
- Metric tags stay bounded: operation (create, cancel, update) and outcome (the OrderOutcome names). The order number goes on the span only (16.10).
- By default ASP.NET Core maps Degraded to 200; ResultStatusCodes maps it to 503 here (16.9), so a slow warehouse takes the instance out of rotation.
- Review 16-S1: ExportIntervalMilliseconds is 30,000 for the console exporter, so the spans of a request stay on screen between metric dumps; the lesson is unchanged (the counter and the histogram still print, every 30 seconds). Say "within 30 seconds the counter follows", not two.
- Review 16-S2: the telemetry profile sets the three settings as environment variables in launchSettings.json (Telemetry__Exporter, Logging__Console__FormatterName, Logging__Console__FormatterOptions__IncludeScopes), which every launcher that reads the profile applies: Visual Studio's F5 with telemetry chosen on the Start button, `dotnet run --launch-profile telemetry`, and VS Code with C# Dev Kit, which reads launchSettings.json (its "Configuring C# debugging" page). The https profile stays the default and stays quiet. The `dotnet run` route was run here; VS Code's F5 was not, so the chapter's VS Code step should name the profile and keep the dotnet run command as its fallback.
- The readiness client is a plain named client ("inventory-health") with a one-second timeout, not the resilience-wrapped InventoryClient, so a probe never waits for retries.
- OpenTelemetry 1.20.1 for all four packages, the newest on nuget.org on November 3, 2026 (the chapter printed 1.19.1, and 1.19.0 for the HttpClient instrumentation). Microsoft.Extensions.Diagnostics.Testing 10.11.0 matches the other Microsoft.Extensions packages in the app (Http.Resilience 10.11.0).
- Program.cs is 89 lines.
