# Chapter 18 delta: Deployment, Containerization, and Native AOT

Generated November 3, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter18` and the operations that rebuild it from the Chapter 17 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 18 ships the running app: one container image for the store, the warehouse service and the migrations, a Kubernetes file that runs it with Chapter 16's probes, a 15-second graceful shutdown, and, in the Activity, a small Native AOT service, because the store app itself cannot be compiled ahead of time.

- Program.cs: `Configure<HostOptions>` with a 15-second ShutdownTimeout: 93 lines.
- `Dockerfile` (multi-stage): the SDK image (sdk:11.0-resolute) restores the dotnet-ef tool, publishes AdventureWorks to /app/store and InventoryService to /app/inventory (framework-dependent, UseAppHost=false), and builds an EF Core migrations bundle (/app/efbundle, linux-x64, framework-dependent: the runtime image already has the runtime); the runtime image is aspnet:11.0-resolute-chiseled-extra (no shell, non-root APP_UID, ICU for the fr culture) with /app/data owned by the app's user and the connection string pointing there. One image, three commands: the store (default), the warehouse (`-w /app/inventory`, command `InventoryService.dll`), the migrations (`--entrypoint /app/efbundle`).
- `.dockerignore`: bin, obj, local databases, benchmark output, .vs and publish folders stay out of the build context.
- `kubernetes.yaml`: Deployments and Services for the warehouse and the store (two replicas), an init container that runs the bundle on the pod's emptyDir database, liveness /health/live and readiness /health/ready on 8080, a 30-second termination grace period.
- No new package and no new route.

## Files added or changed

| File | Change |
| --- | --- |
| `AdventureWorks/Program.cs` | changed |
| `Dockerfile` | added |
| `.dockerignore` | added |
| `kubernetes.yaml` | added |

The reader types 110 lines of code: 107 in 3 new files and 3 new or changed in existing files, plus 0 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 104 of 104 files identical and no differences.

### Step 1: Stop gracefully

An orchestrator stops a container with SIGTERM, then kills it after its grace period. The host then stops taking new requests and gives the running ones until ShutdownTimeout to finish (30 seconds by default); 15 seconds here leaves room inside Kubernetes' default 30-second grace period. It is a deadline, not a promise that every request completes.

In `AdventureWorks/Program.cs`, add these lines after the line `builder.Services.AddStoreObservability(builder.Configuration);`.

```csharp
// On SIGTERM, requests get up to 15 seconds to finish.
builder.Services.Configure<HostOptions>(options =>
    options.ShutdownTimeout = TimeSpan.FromSeconds(15));
```

### Step 2: One image for the store, the warehouse and the migrations

A multi-stage build: the SDK image restores and publishes, and only the published output reaches the runtime image. The runtime image is the chiseled ASP.NET image with ICU (-extra): no shell, no package manager, a non-root user, and the culture data the app's fr localization needs (Chapter 7). It holds three things: the store, the warehouse stand-in from Chapter 12, and an EF Core migrations bundle, a self-contained program that applies Chapter 10's migrations to a database as a deployment step, instead of the app migrating itself at startup. The database lives in /app/data, which belongs to the image's non-root user. .dockerignore keeps bin, obj, local databases and benchmark output out of the build context.

Create `Dockerfile` (30 lines).

```text
# Chapter 18: build with the SDK image, run on the chiseled
# ASP.NET image (no shell, no package manager, a non-root user).
# The -extra variant keeps ICU, which the fr culture needs.
FROM mcr.microsoft.com/dotnet/sdk:11.0-resolute AS build
WORKDIR /src
COPY . .
RUN dotnet tool restore \
    && dotnet publish AdventureWorks -c Release -o /app/store \
        -p:UseAppHost=false \
    && dotnet publish InventoryService -c Release \
        -o /app/inventory -p:UseAppHost=false \
    && dotnet ef migrations bundle -r linux-x64 \
        --project AdventureWorks.Infrastructure \
        --startup-project AdventureWorks -o /app/efbundle \
    && mkdir /data

FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute-chiseled-extra
COPY --from=build /app /app
# The database folder belongs to the app's user (APP_UID, 1654).
COPY --from=build --chown=1654 /data /app/data
ENV ConnectionStrings__AdventureWorks=\
"Data Source=/app/data/adventureworks.db"
WORKDIR /app/store
# The image listens on 8080; EXPOSE only documents it.
EXPOSE 8080
USER $APP_UID
# The store by default. The warehouse: -w /app/inventory and the
# command InventoryService.dll; the migrations: /app/efbundle.
ENTRYPOINT ["dotnet"]
CMD ["AdventureWorks.dll"]
```

Create `.dockerignore` (6 lines).

```text
**/bin/
**/obj/
**/*.db*
**/BenchmarkDotNet.Artifacts/
.vs/
publish/
```

### Step 3: Kubernetes

The same image runs three ways: the warehouse (working folder /app/inventory, command InventoryService.dll), the migrations (an init container that runs /app/efbundle before the store starts), and the store, two replicas behind one Service. The liveness probe is /health/live, so a stuck process restarts; the readiness probe is /health/ready, so a pod whose database or warehouse does not answer stops receiving traffic without restarting (Chapter 16).

Create `kubernetes.yaml` (71 lines).

kubernetes.yaml, part 1 of 2 (34 lines):

```text
# Chapter 18: the store (two replicas) and the warehouse service,
# from the image the Dockerfile builds (adventureworks:ch18). Each
# store pod applies the migrations to its own database first.
apiVersion: apps/v1
kind: Deployment
metadata:
  name: inventory
spec:
  replicas: 1
  selector:
    matchLabels: { app: inventory }
  template:
    metadata:
      labels: { app: inventory }
    spec:
      containers:
        - name: inventory
          image: adventureworks:ch18
          workingDir: /app/inventory
          args: ["InventoryService.dll"]
          ports: [{ containerPort: 8080 }]
---
apiVersion: v1
kind: Service
metadata:
  name: inventory
spec:
  selector: { app: inventory }
  ports: [{ port: 8080 }]
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: store
```

kubernetes.yaml, part 2 of 2 (37 lines):

```text
spec:
  replicas: 2
  selector:
    matchLabels: { app: store }
  template:
    metadata:
      labels: { app: store }
    spec:
      # SIGTERM first; the app's 15-second shutdown fits in 30.
      terminationGracePeriodSeconds: 30
      volumes: [{ name: data, emptyDir: {} }]
      initContainers:
        - name: migrate
          image: adventureworks:ch18
          command: ["/app/efbundle"]
          volumeMounts: [{ name: data, mountPath: /app/data }]
      containers:
        - name: store
          image: adventureworks:ch18
          ports: [{ containerPort: 8080 }]
          env:
            - name: Inventory__BaseUrl
              value: http://inventory:8080/
          volumeMounts: [{ name: data, mountPath: /app/data }]
          livenessProbe:
            httpGet: { path: /health/live, port: 8080 }
          readinessProbe:
            httpGet: { path: /health/ready, port: 8080 }
            periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: store
spec:
  selector: { app: store }
  ports: [{ port: 80, targetPort: 8080 }]
```

## Program.cs at the checkpoint (93 lines)

Program.cs, part 1 of 3 (40 lines):

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
// On SIGTERM, requests get up to 15 seconds to finish.
builder.Services.Configure<HostOptions>(options =>
    options.ShutdownTimeout = TimeSpan.FromSeconds(15));
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

`requests.http` holds these requests in this order; 3 of 3 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `GET /health/live` returned **200**

  ```text
  Healthy
  ```

- `GET /health/ready` returned **200**

  ```text
  Healthy
  ```

- `GET /api/catalog` returned **200**

  ```text
  {"items":[{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":...
  ```

### Other runs

- The app's own checks (no container): the build and the six-project Visual Studio 2026 rebuild without warnings, tests 7 of 7 (unit 3), requests.http 3 of 3 and the README's curl.exe run 3 of 3.
- The container, for real: Docker 29.1.3 in the WSL distro unQbd-BookValidation, with images already on the machine (nothing pulled). The only change for the run: a scratch copy maps the release versions to RC3 (global.json 11.0.100-rc.3.26522.148, packages 11.0.0-rc.3.26522.148) and the image tags to the local RC3 ones (sdk:11.0.100-rc.3-resolute, aspnet:11.0.0-rc.3-resolute-chiseled-extra); the SDK inside the image is the same RC3 build. `tools/docker18.py`, 15 of 15:
  - `docker build -t adventureworks:ch18 .` from the working folder: built in about 220 s (the restore inside the build stage reads nuget.org). `docker images` lists it at 397 MB, of which the chiseled-extra base is 249 MB; the store's publish output is 64 MB, the warehouse 34 KB, the bundle 38.6 MB.
  - .dockerignore: a planted `AdventureWorks/bin/planted.txt` and `AdventureWorks/adventureworks.db` never reached the build stage's /src.
  - The image: user 1654 (APP_UID), working folder /app/store, entrypoint `dotnet`, command `AdventureWorks.dll`, port 8080 (ASPNETCORE_HTTP_PORTS=8080 comes from the base image), `ConnectionStrings__AdventureWorks=Data Source=/app/data/adventureworks.db`; it holds app/store/AdventureWorks.dll with appsettings.json and wwwroot (the Web SDK publishes them; no Content or None lines needed, accuracy 18.1), app/inventory/InventoryService.dll, app/efbundle and app/data/.
  - These commands, as the README prints them, ran in order:

    ```text
    docker network create aw
    docker volume create awdata
    docker run --rm -v awdata:/app/data --entrypoint /app/efbundle adventureworks:ch18
    docker run -d --name inventory --network aw -w /app/inventory adventureworks:ch18 InventoryService.dll
    docker run -d --name store --network aw -p 8080:8080 -v awdata:/app/data -e Inventory__BaseUrl=http://inventory:8080/ adventureworks:ch18
    ```

    The bundle ended with `INSERT INTO "__EFMigrationsHistory" ... VALUES ('20261103153217_InitialCreate', '11.0.0-rc.3.26522.148');` and `Done.` The store logged "Now listening on: http://[::]:8080", "Hosting environment: Production" and "Content root path: /app/store", plus two warnings readers will see: Data Protection keys in /home/app/.aspnet/DataProtection-Keys "may not be persisted outside of the container", and "Failed to determine the https port for redirect" (the container serves HTTP; TLS ends at the proxy or ingress).
  - `curl -i http://localhost:8080/health/live`: 200 "Healthy"; `/health/ready`: 200 "Healthy" (the store called http://inventory:8080/health and got 200); `/api/catalog`: 200 `{"items":[],"featured":"/api/catalog/3"}`: the tables exist, and nothing seeds a Production database.
  - `docker stop store`: the store logged "Application is shutting down..." and exited with code 0 in 1.6 s (no request was running, so the 15-second timeout was not needed).
  - kubernetes.yaml: parsed with PyYAML 6.0.1 (two Deployments and two Services), and its image (adventureworks:ch18), container port 8080, probe paths /health/live and /health/ready, `Inventory__BaseUrl=http://inventory:8080/`, init container command /app/efbundle, and the warehouse's working folder and argument match the container that ran. Not applied to a cluster: there is no cluster or kubectl here. Do not say kubectl apply was run.

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks/InventoryHealthCheck.cs` on line 23:

```csharp
var status = (int)response.StatusCode;
```

Press F5 (the https profile), then send GET https://localhost:7001/health/ready, the request a readiness probe sends. When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
context.Registration.Name = inventory [String]
http.BaseAddress = http://127.0.0.1:42132/ [Uri]
http.Timeout = 00:00:01 [TimeSpan]
response.StatusCode = OK [HttpStatusCode]
```

This is what Kubernetes' readiness probe runs every five seconds in kubernetes.yaml: the inventory check calls the warehouse on its own plain client, with a one-second timeout and no retries. In the container the base address is http://inventory:8080/, from Inventory__BaseUrl.

## What the Activity adds

The original Activity exercised the chapter's sample (publish, readiness, containers, AOT, CI). This Activity adds the AOT lesson the store cannot carry (Chapter18-Activity): `AdventureWorks.CatalogFeed`, from `dotnet new web`, with PublishAot and InvariantGlobalization in its project file and a 25-line Program.cs (CreateSlimBuilder, FeedJsonContext first in the resolver chain, GET /feed and GET /health/live), and `Dockerfile.aot`.

Checked (`tools/docker18a.py`, 9 of 9): `docker build -f Dockerfile.aot -t adventureworks-feed:ch18 .` published with Native AOT in the -aot SDK image with no trim or AOT warnings; the only local AOT SDK image is RC2 (sdk:11.0.100-rc.2-resolute-aot), so that run mapped global.json to 11.0.100-rc.2.26504.105 and runtime-deps to 11.0.0-rc.2-resolute-chiseled in a scratch copy (the feed has no package references). The image holds one 9.7 MB native executable and no .dll; `docker images` lists it at 38.9 MB (runtime-deps is 24.4 MB). It ran as user 1654 on 8080: /health/live 200 "Healthy", /feed 200 `[{"sku":"HLM-1001","price":29.99},{"sku":"JRS-2001","price":54.50},{"sku":"BIK-3001","price":1299.00},{"sku":"TNT-4001","price":189.00}]`, 10.2 MiB of memory after the two requests. Copying only the executable keeps its 24.7 MB .dbg symbol file and a static-web-assets manifest out of the image (the first build copied the whole publish folder: 18.9 MB by docker image inspect, against 11.8 MB). The Visual Studio rebuild built all seven projects; the tests and requests are the checkpoint's.

Native AOT was not compiled on Windows: it needs the Visual Studio C++ build tools, which this machine does not have (accuracy 18.18), and Windows cannot cross-compile to linux-x64 (18.17); the -aot image is the documented way.

The reader-path check rebuilt `AdventureWorks/Chapter18-Activity` from the Chapter 18 checkpoint: no differences.
The Activity adds 43 lines of code: 16 in 1 new file and 27 new or changed in existing files, plus 2 command lines. Blank lines in listings are counted; deleted lines and files that a command writes are not.

### Activity: a Native AOT price feed

Work in a copy of the Chapter 18 checkpoint folder. The store app cannot be compiled ahead of time (the AOT report below says why), so this Activity builds a separate, small read-only service that can: the slim builder, one source-generated JSON context and no reflection. The -aot SDK image has the native toolchain Native AOT needs, and the result runs on runtime-deps, which has no .NET runtime at all.

Run this from the working folder.

```shell
dotnet new web -o AdventureWorks.CatalogFeed
```

It prints:

```text
The template "ASP.NET Core Empty" was created successfully.
Processing post-creation actions...
Restoring AdventureWorks.CatalogFeed\AdventureWorks.CatalogFeed.csproj:
  Determining projects to restore...
  Restored AdventureWorks.CatalogFeed\AdventureWorks.CatalogFeed.csproj (in 106 ms).
Restore succeeded.
```

Delete `AdventureWorks.CatalogFeed/Properties/launchSettings.json`. The template picks random ports; the feed runs in its container on 8080.

Run this from the working folder.

```shell
dotnet sln add AdventureWorks.CatalogFeed
```

It prints:

```text
Project `AdventureWorks.CatalogFeed\AdventureWorks.CatalogFeed.csproj` added to the solution.
```

In `AdventureWorks.CatalogFeed/AdventureWorks.CatalogFeed.csproj`, add these lines after the line `<ImplicitUsings>enable</ImplicitUsings>`. InvariantGlobalization: the feed formats no culture-specific text, and runtime-deps has no ICU.

```xml
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>true</InvariantGlobalization>
```

Replace the whole of `AdventureWorks.CatalogFeed/Program.cs` with the following.

```csharp
using System.Text.Json.Serialization;

// Chapter 18 Activity: a read-only price feed small enough for
// Native AOT. The slim builder, a source-generated JSON context
// and no reflection; the store app itself is not AOT-compatible.
var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0,
        FeedJsonContext.Default));
var app = builder.Build();

FeedItem[] feed =
[
    new("HLM-1001", 29.99m), new("JRS-2001", 54.50m),
    new("BIK-3001", 1299.00m), new("TNT-4001", 189.00m)
];
app.MapGet("/feed", () => feed);
app.MapGet("/health/live", () => "Healthy");
app.Run();

public sealed record FeedItem(string Sku, decimal Price);

[JsonSerializable(typeof(FeedItem[]))]
internal sealed partial class FeedJsonContext
    : JsonSerializerContext;
```

Create `Dockerfile.aot` (16 lines).

```text
# Chapter 18 Activity: Native AOT needs the native toolchain the
# -aot SDK image carries. The result runs on runtime-deps, an
# image with no .NET runtime at all.
FROM mcr.microsoft.com/dotnet/sdk:11.0-resolute-aot AS build
WORKDIR /src
COPY global.json .
COPY AdventureWorks.CatalogFeed/ AdventureWorks.CatalogFeed/
RUN dotnet publish AdventureWorks.CatalogFeed -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime-deps:11.0-resolute-chiseled
WORKDIR /app
# The native executable only; its .dbg symbol file stays behind.
COPY --from=build /app/AdventureWorks.CatalogFeed .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["./AdventureWorks.CatalogFeed"]
```

### Activity requests and responses

3 of 3 requests in `Chapter18-Activity/requests.http` returned their commented status. They are the checkpoint's requests, with the same results; the Activity adds a project and an image.

- `GET /health/live` returned **200**

  ```text
  Healthy
  ```

- `GET /health/ready` returned **200**

  ```text
  Healthy
  ```

- `GET /api/catalog` returned **200**

  ```text
  {"items":[{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":...
  ```

## Step 3's startup failure, verified

For the writer's Step 3, run on RC3 in an RC3 copy of this checkpoint (`tools/startup18.py`), as a host would start it: no ASPNETCORE_ENVIRONMENT (so Production), no ASPNETCORE_URLS, InventoryService not running. Port 5000, the default without URLs, was taken by another program on this machine, so both runs add `--urls http://localhost:5085`; without it the app listens on http://localhost:5000.

1. `dotnet publish AdventureWorks -c Release -o publish` from the working folder: exit 0. The folder holds AdventureWorks.dll and .exe, their .pdb, deps.json and runtimeconfig.json, AdventureWorks.Core.dll and AdventureWorks.Infrastructure.dll, appsettings.json and appsettings.Development.json, web.config, AdventureWorks.staticwebassets.endpoints.json and wwwroot.
2. `dotnet publish/AdventureWorks.dll --urls http://localhost:5085` from the working folder stops at once with exit code 3762504530 (0xE0434352, the code Windows gives an unhandled .NET exception; -532462766 as a signed number). The log, with the stack trace cut after its first three frames and the user folder name replaced by <user>:

```text
info: Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager[63]
      User profile is available. Using 'C:\Users\<user>\AppData\Local\ASP.NET\DataProtection-Keys' as key repository and Windows DPAPI to encrypt keys at rest.
fail: Microsoft.Extensions.Hosting.Internal.Host[11]
      Hosting failed to start
      Microsoft.Extensions.Options.OptionsValidationException: DataAnnotation validation failed for 'FeatureFlags' members: 'MaxCartItems' with the error: 'The field MaxCartItems must be between 1 and 100.'.; DataAnnotation validation failed for 'FeatureFlags' members: 'AllowedRegions' with the error: 'The field AllowedRegions must be a string or array type with a minimum length of '1'.'.
         at Microsoft.Extensions.Options.OptionsFactory`1.ThrowIfValidationFailed(String name, List`1 failures)
         at Microsoft.Extensions.Options.OptionsFactory`1.CreateAsync(String name, CancellationToken cancellationToken)
         at Microsoft.Extensions.DependencyInjection.OptionsBuilderExtensions.<>c__DisplayClass0_1`1.<<ValidateOnStart>b__4>d.MoveNext()
```

   The first line is an info line from Data Protection, not the failure; the first `fail:` line is. The message names two members, not one: MaxCartItems (0, outside 1 to 100) and AllowedRegions (empty; Chapter 2's [MinLength(1)]). Both come from appsettings.json, which was not read: the content root was the working folder.
3. `dotnet publish/AdventureWorks.dll --contentRoot . --urls http://localhost:5085` starts. A relative content root is resolved against the DLL's folder, and the log prints the path with the dot kept:

```text
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5085
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\unQbd-Repos\Book-Production\Full-Fixes-aspnet2\work\startup18\publish\.
warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]
      Failed to determine the https port for redirect.
```

   `/health/live` answered 200 "Healthy"; `/health/ready` answered 503 "Unhealthy", and the log names both checks:

```text
fail: Microsoft.Extensions.Diagnostics.HealthChecks.DefaultHealthCheckService[103]
      Health check database with status Unhealthy completed after 762.7072ms with message '(null)'
warn: Microsoft.Extensions.Diagnostics.HealthChecks.DefaultHealthCheckService[103]
      Health check inventory with status Degraded completed after 1022.729ms with message 'Inventory did not answer within a second.'
```

   The database check is Unhealthy because publish has no adventureworks.db (Production migrates nothing; the check does not create the file: none existed afterward). The inventory check is Degraded because InventoryService was not running (on Windows the refused connection outlasts the one-second timeout, Chapter 16). The overall status is the worst one, Unhealthy. If the chapter quotes only the database line, say that the inventory line appears too unless InventoryService runs.

## The store app and Native AOT

The store cannot be compiled ahead of time, and the build says why. From the working folder, on RC3:

```shell
dotnet build AdventureWorks -p:PublishAot=true
```

The build fails with 41 warnings and 50 errors. The warnings are the trim and AOT analysis of the app's own code: 25 IL2026 (members marked RequiresUnreferencedCode: DataAnnotations [MinLength] and [MaxLength] in Core and Identity, ApplyConfigurationsFromAssembly and the DbContext constructor in EF Core, AddIdentity, AddControllers, AddRazorPages, ValidateDataAnnotations, reflection-based JSON calls, CreatedAtRoute, link generation by route values), 6 IL3050 (RequiresDynamicCode, for example MigrateAsync in the Development seed), and 10 compiler warnings in generated code. The errors come from the Request Delegate Generator, which PublishAot turns on for every Minimal API endpoint: it emits code that does not compile for two of the app's endpoints, an anonymous type returned through TypedResults.Ok (`/api/orders/validate`, Chapter 7) and `typeof(string?)` for the nullable constructor parameter of the `[AsParameters]` SearchParameters record (Chapter 12). Say the conclusion plainly: MVC controllers, Razor Pages, Identity and EF Core's reflection-based model are outside Native AOT's support, so the store stays a JIT app in the chiseled ASP.NET image, and AOT is for small services built for it, like the Activity's feed (accuracy 18.19, 18.23). Do not suppress the warnings to get a green publish.

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| AdventureWorks.Deployment.csproj, Catalog.cs, catalog.json, Program.cs (CreateSlimBuilder; printed twice) | Removed | The deployment chapter ships the running app; the AOT-shaped sample becomes the Activity's AdventureWorks.CatalogFeed. |
| catalog.json and its `None Update` copy line (accuracy 18.1) | Removed | The app has no extra content file; the Web SDK already publishes appsettings*.json and wwwroot (the image listing shows them under app/store). |
| CatalogHealthCheck ("catalog", a file read) and /healthz/live, /healthz/ready | Replaced | Chapter 16's "database" and "inventory" checks on /health/live and /health/ready, so there is one check name per check and no duplicate (C16, C18). |
| `InvariantGlobalization` true | Removed | The store keeps ICU (the fr culture, design decision 12), so its image is aspnet:11.0-resolute-chiseled-extra. Only the Activity's feed is invariant. |
| Dockerfile (14 lines) | Replaced | Builds the store, the warehouse and the migrations bundle into one image; same multi-stage and non-root ideas. |
| .dockerignore (7 lines) | Kept, adapted | bin, obj, *.db*, BenchmarkDotNet.Artifacts, .vs, publish. |
| compose.yaml (8 lines) | Removed | The README's docker run commands (network, volume, bundle, warehouse, store) cover local runs; kubernetes.yaml covers orchestration. |
| kubernetes.yaml (53 lines, companion) | Replaced | 71 lines: the warehouse and the store, an init container for the migrations, Chapter 16's probes. |
| Dockerfile.aot (14 lines) | Kept, adapted | The Activity's feed on runtime-deps. |
| ci.yml (21 lines) | Removed | It ran another app's tests (C18-1); the companion's checks are the README commands and requests. Say how CI would run `dotnet test --project AdventureWorks.Tests` if the chapter keeps a CI section. |

## Notes for the writers

- Accuracy 18.1: no Content or None item is needed; appsettings.json and wwwroot are in the publish output by default.
- Accuracy 18.2: USER $APP_UID; the image's Config.User is 1654.
- Accuracy 18.9: the store needs ICU (fr), so its runtime image is chiseled-extra, which has ICU and tzdata; the plain chiseled image is invariant. The feed sets InvariantGlobalization and runs on runtime-deps.
- Accuracy 18.10: ShutdownTimeout 15 s is a deadline inside Kubernetes' 30-second termination grace period, not a promise that every request finishes.
- Accuracy 18.11 and 18.15: liveness runs no checks; a chiseled image has no shell or curl, so there is no Dockerfile HEALTHCHECK, and the orchestrator's HTTP probes call the app's routes.
- Accuracy 18.12: `-p:UseAppHost=false` in the Dockerfile's publish commands; the image starts the app with `dotnet AdventureWorks.dll`.
- Accuracy 18.13 and 18.14: 8080 is the images' default port; EXPOSE documents it and `-p 8080:8080` publishes it.
- Migrations: a bundle, run as a deployment step (docker run --entrypoint /app/efbundle, or the Kubernetes init container), not MigrateAsync at startup, which Development keeps for its seed. EF Core prints "Don't forget to copy appsettings.json alongside your bundle"; here the connection string comes from the image's environment.
- The database is SQLite in a volume, enough to run and probe the image; with two store replicas in kubernetes.yaml each pod has its own emptyDir database. A real deployment points ConnectionStrings__AdventureWorks at a database server (Chapter 10's Activity shows SQL Server). Say so beside the YAML.
- Data Protection keys live inside the container unless the app persists them (the warning above); a sign-in cookie does not survive a new container. A real deployment stores the keys in shared storage.
- Program.cs is 93 lines.
