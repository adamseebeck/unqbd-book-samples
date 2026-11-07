# Chapter 1 delta: Introduction to ASP.NET Core

Generated October 30, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter01` and the operations that rebuild it from an empty folder. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

The reader creates the running project. In a working folder that holds `global.json`, `dotnet new web` makes the `AdventureWorks` web project; the chapter then configures the host and pipeline. At the checkpoint:

- Kestrel omits the Server header and caps request bodies at 10 MiB.
- Health checks and Problem Details are registered.
- `RequestTimingMiddleware` (convention-based) runs first and logs `{Method} {Path} elapsed {ElapsedMs} ms` for every request.
- Outside Development: `UseExceptionHandler` and `UseHsts`. Always: `UseHttpsRedirection`.
- Routes: `/health`, `/` (302 to `/api/status`), `/api/status` (`{"status":"Operational","timestamp":"..."}`).
- One launch profile, `https`, on https://localhost:7001 and http://localhost:5001, environment Development.

The working folder also holds `AdventureWorks.slnx`, so Visual Studio 2026 opens the project as a solution and F5 starts the https profile.

Every web type uses the file-scoped namespace `AdventureWorks` (the design's "namespaces follow the project names"), so Program.cs starts with `using AdventureWorks;`.

## Files added or changed

| File | Change |
| --- | --- |
| `global.json` | added |
| `AdventureWorks/Program.cs` | changed |
| `AdventureWorks/RequestTimingMiddleware.cs` | added |
| `AdventureWorks/Properties/launchSettings.json` | replaced |
| (command) `dotnet new web --name AdventureWorks --framework net11.0` | creates or edits project files |
| (command) `dotnet new sln -n AdventureWorks` | creates or edits project files |
| (command) `dotnet sln add AdventureWorks` | creates or edits project files |

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 8 of 8 files identical and no differences.

### Step 1: Create the project

In an empty working folder, save global.json, check the SDK, then create the web project. The template creates AdventureWorks/ with AdventureWorks.csproj, Program.cs, appsettings.json, appsettings.Development.json and Properties/launchSettings.json. Leave the csproj and both appsettings files as generated.

Create `global.json` (6 lines). Save it in the working folder; every later chapter keeps it unchanged.

```json
{
    "sdk": {
        "version": "11.0.100",
        "rollForward": "latestFeature"
    }
}
```

Run this from the working folder.

```shell
dotnet --version
```

It prints:

```text
11.0.100
```

Run this from the working folder.

```shell
dotnet new web --name AdventureWorks --framework net11.0
```

It prints:

```text
The template "ASP.NET Core Empty" was created successfully.
Processing post-creation actions...
Restoring AdventureWorks\AdventureWorks.csproj:
  Determining projects to restore...
  Restored AdventureWorks\AdventureWorks.csproj (in 213 ms).
Restore succeeded.
```

Create the solution file and add the project, so the folder opens in Visual Studio 2026 exactly as the File > New > Project route leaves it (an AdventureWorks.slnx beside the project folder). Visual Studio users skip these two commands.

Run this from the working folder.

```shell
dotnet new sln -n AdventureWorks
```

It prints:

```text
The template "Solution File" was created successfully.
```

Run this from the working folder.

```shell
dotnet sln add AdventureWorks
```

It prints:

```text
Project `AdventureWorks\AdventureWorks.csproj` added to the solution.
```

### Step 3: Configure the application builder

Program.cs from the template has four lines. Add the using directive at the top, then the builder configuration after the CreateBuilder line: two Kestrel limits, health checks and Problem Details. The template already separates the builder and app with a blank line; keep it.

In `AdventureWorks/Program.cs`, add these lines above the line `var builder = WebApplication.CreateBuilder(args);`.

```csharp
using AdventureWorks;
```

In `AdventureWorks/Program.cs`, add these lines after the line `var builder = WebApplication.CreateBuilder(args);`.

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
```

### Step 4: Configure middleware and endpoints

Create the middleware class first, then replace everything from `var app = builder.Build();` to the end of Program.cs.

Create `AdventureWorks/RequestTimingMiddleware.cs` (24 lines). File-scoped namespace AdventureWorks matches the project name. Program.cs imports it with `using AdventureWorks;` (Step 3).

```csharp
using System.Diagnostics;

namespace AdventureWorks;

public sealed class RequestTimingMiddleware(
    RequestDelegate next,
    ILogger<RequestTimingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            logger.LogInformation(
                "{Method} {Path} elapsed {ElapsedMs} ms",
                context.Request.Method, context.Request.Path,
                timer.Elapsed.TotalMilliseconds);
        }
    }
}
```

In `AdventureWorks/Program.cs`, replace these lines.

```csharp
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
```

With these:

```csharp
var app = builder.Build();
app.UseMiddleware<RequestTimingMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}
app.UseHttpsRedirection();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/api/status"));
app.MapGet("/api/status", () => Results.Ok(new
{
    Status = "Operational",
    Timestamp = DateTime.UtcNow
}));
app.Run();
```

### Step 5: Configure host URLs and environment

Replace the generated launch settings. The template makes an http and an https profile with random ports; this keeps one https profile on the book's fixed ports. The applicationUrl value sits on its own line so the listing fits the print width; JSON allows the line break there.

Replace the whole of `AdventureWorks/Properties/launchSettings.json` with the following.

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl":
        "https://localhost:7001;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## Program.cs at the checkpoint (27 lines)

```csharp
using AdventureWorks;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseMiddleware<RequestTimingMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}
app.UseHttpsRedirection();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/api/status"));
app.MapGet("/api/status", () => Results.Ok(new
{
    Status = "Operational",
    Timestamp = DateTime.UtcNow
}));
app.Run();
```

## Commands the reader runs and what they print

Trust the local development certificate once per machine if the browser or curl.exe rejects it:

```shell
dotnet dev-certs https --trust
```

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

In Visual Studio 2026, open `AdventureWorks.slnx` and press F5; the https profile starts the same app on https://localhost:7001. A headless Visual Studio 2026 rebuild of this checkpoint (devenv /rebuild Debug) rebuilt 1 project(s) with 0 warning(s) and 0 error(s).

## Requests and real responses

`requests.http` holds these requests in this order; 5 of 5 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `GET /` returned **302** (Location: /api/status)

- `GET /api/status` returned **200**

  ```text
  {"status":"Operational","timestamp":"2026-11-03T14:23:09.3717607Z"}
  ```

- `GET /health` returned **200**

  ```text
  Healthy
  ```

- `GET http://localhost:5001/api/status` returned **307** (Location: https://localhost:7001/api/status)

- `GET /missing` returned **404**

### Other runs

- Production (`dotnet run --project AdventureWorks -- --environment Production`): `/api/status` and `/health` still return 200, and the console shows `Hosting environment: Production`. HSTS adds no header on localhost, because the HSTS middleware excludes localhost by default.

## What the Activity adds

`EnvironmentLoggingMiddleware` logs `Environment {Environment}` for each request, registered only when the app runs in Development, right after the timing middleware. A diagnostic route, `/api/config`, returns the environment name and `app.Urls`. In Staging (`-- --environment Staging`), `/api/config` returns `"environment":"Staging"` and no `Environment` lines appear; both were checked. Chapter 2 starts from the main checkpoint, so its own `/api/config` (FeatureFlags) does not collide with this one.

The reader-path check rebuilt `AdventureWorks/Chapter01-Activity` from the Chapter 1 checkpoint: no differences.

### Activity: environment logging and a diagnostic route

Work in a copy of the Chapter 1 checkpoint folder. Chapter 2 starts from the checkpoint, not from this copy.

Create `AdventureWorks/EnvironmentLoggingMiddleware.cs` (14 lines).

```csharp
namespace AdventureWorks;

public sealed class EnvironmentLoggingMiddleware(
    RequestDelegate next,
    IHostEnvironment environment,
    ILogger<EnvironmentLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogInformation("Environment {Environment}",
            environment.EnvironmentName);
        await next(context);
    }
}
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.UseMiddleware<RequestTimingMiddleware>();`.

```csharp
if (app.Environment.IsDevelopment())
    app.UseMiddleware<EnvironmentLoggingMiddleware>();
```

In `AdventureWorks/Program.cs`, add these lines after the line `}));`. The anchor is the `}));` line that closes the /api/status route, just before `app.Run();`.

```csharp
app.MapGet("/api/config", () => Results.Ok(new
{
    Environment = app.Environment.EnvironmentName,
    Urls = app.Urls
}));
```

### Activity requests and responses

6 of 6 requests in `Chapter01-Activity/requests.http` returned their commented status. The responses for the requests the Activity adds:

- `GET /api/config` returned **200**

  ```text
  {"environment":"Development","urls":["https://localhost:7001","http://localhost:5001"]}
  ```

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| Step 1 global.json | Kept | Unchanged. |
| Step 2 generated Program.cs | Kept | It is what `dotnet new web` generates. |
| Step 3 logging demonstration (ClearProviders, AddConsole, SetMinimumLevel) | Replaced | Use the Step 3 block in this delta: the two Kestrel options, health checks and Problem Details, which is what the checkpoint keeps; give each Kestrel option one sentence (master finding C01-3). If the ClearProviders lesson stays, present it as a try-and-revert aside; it is not in the checkpoint. |
| Step 4 RequestTimingMiddleware.cs | Replaced | Adds `namespace AdventureWorks;` and breaks the LogInformation call after its opening parenthesis to fit 66 characters. |
| Step 4 "replace everything after Build" listing | Replaced | Now includes the `/` redirect and the timestamp, so Step 4 already equals the Full Example's pipeline. |
| Step 5 launchSettings.json | Replaced | The template's file with the http profile removed: keeps `$schema`, uses the template's two-space indentation, and puts the applicationUrl value on its own line (valid JSON) so no line exceeds 66 characters. |
| Step 5 UseUrls one-liner | Kept | An illustration only; not in the checkpoint. |
| Step 6 csproj | Replaced | Show the template's exact output (two-space indentation, blank lines); the content is the same. |
| Full Example Program.cs | Replaced | Adds `using AdventureWorks;` and drops ConfigureHttpJsonOptions and its two usings, which no step taught (master finding C01-3; camelCase is already the web default, and nothing in Chapters 1 to 6 returns a null property the reader sees). 27 lines. |
| Full Example RequestTimingMiddleware.cs repeat | Removed | Printed once, in Step 4. |

## Notes for the writers

- Line 59 says AdventureWorks is the example project for Chapters 1 to 6 and that later chapters build their own projects. Every chapter now extends this one app.
- Run commands: from the working folder, `dotnet run --project AdventureWorks`; or `dotnet run` inside the AdventureWorks folder. `--launch-profile https` is optional because https is the only profile. Plain HTTP answers 307 to HTTPS in Development (checked).
- Every checkpoint opens in Visual Studio 2026 through `AdventureWorks.slnx` (Adam, November 3). Visual Studio's File > New > Project creates the solution file; the CLI route creates it with `dotnet new sln -n AdventureWorks` and `dotnet sln add AdventureWorks` in Step 1. This moves the solution file from Chapter 2 (the design) to Chapter 1, so Chapter 2 only adds the two class libraries to it.
- F5 in Visual Studio uses the only profile, https, so the book's ports (7001 and 5001) are what the reader sees. The headless Visual Studio 2026 rebuild of the checkpoint (devenv /rebuild) reported 1 project rebuilt, 0 warnings, 0 errors.
- No code line or command in this chapter exceeds 66 characters. The template's csproj and appsettings files are unchanged.
