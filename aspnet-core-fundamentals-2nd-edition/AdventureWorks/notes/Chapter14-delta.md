# Chapter 14 delta: OpenAPI, Swagger, and API Versioning

Generated November 3, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter14` and the operations that rebuild it from the Chapter 13 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 14 documents the app's API, including Chapter 5's two product API versions, without a separate project.

- `OpenApiDocuments.cs`: AddStoreOpenApi registers three documents. "store" uses the default rule (endpoints with no group name: the catalog, orders, accounts, Admin and Development routes); "v1" and "v2" include only their group. Document transformers set each title, version and description; an operation transformer marks every v1 operation deprecated.
- ProductsController gets `[ApiExplorerSettings(GroupName = "v1")]`; the v2 Minimal API group gets `.WithGroupName("v2")`.
- Program.cs: `AddOpenApi()` becomes `AddStoreOpenApi()`. Chapter 4's `MapOpenApi().AllowAnonymous()` already serves /openapi/{documentName}.json in Development. Still 88 lines.
- The documents are OpenAPI 3.2.0, the default of Microsoft.AspNetCore.OpenApi 11.

## Files added or changed

| File | Change |
| --- | --- |
| `AdventureWorks/OpenApiDocuments.cs` | added |
| `AdventureWorks/Program.cs` | changed |
| `AdventureWorks/Controllers/ProductsController.cs` | changed |
| `AdventureWorks/ProductEndpointsV2.cs` | changed |

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 86 of 86 files identical and no differences.

### Step 1: One document per audience

Chapter 4 registered one OpenAPI document with AddOpenApi(). The product API has had two versions since Chapter 5, in two styles: /api/v1/products (controllers) and /api/v2/products (Minimal APIs). Each version now gets its own document, and the unversioned store routes get a third. A document includes the endpoints its ShouldInclude rule accepts; the default rule takes endpoints without a group name, or with the document's own name. Transformers set each document's title, version and description, and an operation transformer marks every v1 operation deprecated.

Create `AdventureWorks/OpenApiDocuments.cs` (53 lines).

OpenApiDocuments.cs, part 1 of 3 (4 lines):

```csharp
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AdventureWorks;
```

OpenApiDocuments.cs, part 2 of 3 (36 lines):

```csharp
// Three OpenAPI documents at /openapi/{name}.json: the store's
// unversioned routes and the two versions of Chapter 5's product
// API. An endpoint's group name picks its document.
public static class OpenApiDocuments
{
    public static IServiceCollection AddStoreOpenApi(
        this IServiceCollection services)
    {
        // The default rule includes endpoints with no group name.
        services.AddOpenApi("store", options =>
            options.AddDocumentTransformer(Info(
                "AdventureWorks Store API", "1.0",
                "Catalog, orders, accounts and Admin routes.")));
        services.AddOpenApi("v1", options =>
        {
            options.ShouldInclude = api => api.GroupName == "v1";
            options.AddDocumentTransformer(Info(
                "AdventureWorks Products API", "1.0",
                "Controllers. Deprecated: use version 2."));
            // Runs once for each operation in this document.
            options.AddOperationTransformer(
                (operation, context, _) =>
                {
                    operation.Deprecated = true;
                    return Task.CompletedTask;
                });
        });
        services.AddOpenApi("v2", options =>
        {
            options.ShouldInclude = api => api.GroupName == "v2";
            options.AddDocumentTransformer(Info(
                "AdventureWorks Products API", "2.0",
                "Minimal APIs, the current version."));
        });
        return services;
    }
```

OpenApiDocuments.cs, part 3 of 3 (11 lines):

```csharp
    static Func<OpenApiDocument,
        OpenApiDocumentTransformerContext, CancellationToken,
        Task> Info(string title, string version,
        string description) => (document, _, _) =>
    {
        document.Info.Title = title;
        document.Info.Version = version;
        document.Info.Description = description;
        return Task.CompletedTask;
    };
}
```

In `AdventureWorks/Program.cs`, replace these lines.

```csharp
builder.Services.AddOpenApi();
```

With these:

```csharp
builder.Services.AddStoreOpenApi();
```

### Step 2: Put each version in its document

Controllers take their group name from ApiExplorerSettings; Minimal API endpoints from WithGroupName, here on the v2 group so every route in it follows. Chapter 4's `MapOpenApi()` already serves /openapi/{documentName}.json in Development, so the three documents are /openapi/store.json, /openapi/v1.json and /openapi/v2.json.

In `AdventureWorks/Controllers/ProductsController.cs`, add these lines after the line `[Route("api/v1/products")]`.

```csharp
[ApiExplorerSettings(GroupName = "v1")]
```

In `AdventureWorks/ProductEndpointsV2.cs`, replace these lines. In MapProductEndpointsV2.

```csharp
            .WithTags("Products V2")
```

With these:

```csharp
            .WithTags("Products V2").WithGroupName("v2")
```

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

In Visual Studio 2026, open `AdventureWorks.slnx` and press F5; the https profile starts the same app on https://localhost:7001. A headless Visual Studio 2026 rebuild of this checkpoint (devenv /rebuild Debug) rebuilt 4 project(s) with 0 warning(s) and 0 error(s).

## Requests and real responses

`requests.http` holds these requests in this order; 6 of 6 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `GET /openapi/store.json` returned **200**

  ```text
  {
    "openapi": "3.2.0",
    "info": {
      "title": "AdventureWorks Store API",
      "description": "Catalog, orders, accounts and Admin routes.",
      "version": "1.0"
    },
    "servers": [
      {
        "url": "https://localhos...
  ```

- `GET /openapi/v1.json` returned **200**

  ```text
  (the OpenAPI JSON document)
  ```

- `GET /openapi/v2.json` returned **200**

  ```text
  {
    "openapi": "3.2.0",
    "info": {
      "title": "AdventureWorks Products API",
      "description": "Minimal APIs, the current version.",
      "version": "2.0"
    },
    "servers": [
      {
        "url": "https://localhost:7001...
  ```

- `GET /openapi/v3.json` returned **404**

  ```text
  No OpenAPI document with the name 'v3' was found.
  ```

- `GET /api/v1/products/1` returned **200**

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1}
  ```

- `GET /api/v2/products/1` returned **200**

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1}
  ```

### Other runs

- Production: nothing maps /openapi outside Development, so under Chapter 9's fallback policy GET /openapi/v1.json answers 302 to the sign-in page (not 404), and /api/v2/products/1 still answers 200. Checked.

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks/OpenApiDocuments.cs` on line 29:

```csharp
operation.Deprecated = true;
```

Press F5 (the https profile), then send GET https://localhost:7001/openapi/v1.json. When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
context.DocumentName = v1 [String]
context.Description.HttpMethod = GET [String]
context.Description.RelativePath = api/v1/products [String]
context.Description.GroupName = v1 [String]
context.DocumentName = v1 [String]
context.Description.HttpMethod = POST [String]
context.Description.RelativePath = api/v1/products [String]
context.Description.GroupName = v1 [String]
context.DocumentName = v1 [String]
context.Description.HttpMethod = GET [String]
context.Description.RelativePath = api/v1/products/{id} [String]
context.Description.GroupName = v1 [String]
context.DocumentName = v1 [String]
context.Description.HttpMethod = PUT [String]
context.Description.RelativePath = api/v1/products/{id} [String]
context.Description.GroupName = v1 [String]
context.DocumentName = v1 [String]
context.Description.HttpMethod = DELETE [String]
context.Description.RelativePath = api/v1/products/{id} [String]
context.Description.GroupName = v1 [String]
```

The document is built when it is first requested, and the operation transformer runs once for each operation in it: five stops, one per action of the v1 controller, each with GroupName v1, the attribute that put the controller in this document. The v2 and store documents never reach this transformer, because it is registered for v1 only.

## What the Activity adds

The original Activity's checks (fetch both documents, compare their paths, switch the format, remove a group name, run in Production) are covered by the checkpoint's requests and the production run. This Activity adds the build-time document generation the original Step 6 only names: Microsoft.Extensions.ApiDescription.Server 11.0.0 runs the app's host during every build (the GenerateOpenApiDocuments target), asks it for each registered document and writes them to files, and `<OpenApiDocumentsDirectory>docs</OpenApiDocumentsDirectory>` puts them in AdventureWorks/docs instead of obj. The default document name, v1, is written as AdventureWorks.json; the others as AdventureWorks_store.json and AdventureWorks_v2.json.

Checked: two reader-path builds produced byte-identical files; the files carry no version or time stamp, so they diff cleanly in code review; the build-time store document has 20 paths and none of the Development-only routes (/dev, /examples), because the build does not run the host in the Development environment; the build and the Visual Studio rebuild stayed free of warnings.

The reader-path check rebuilt `AdventureWorks/Chapter14-Activity` from the Chapter 14 checkpoint: no differences.

### Activity: write the documents at build time

Work in a copy of the Chapter 14 checkpoint folder; Chapter 15 starts from the checkpoint. With Microsoft.Extensions.ApiDescription.Server, every build starts the app's host the way the ef tools do, asks it for each registered document, and writes the documents to files, so the contract can be committed and compared in code review. OpenApiDocumentsDirectory puts them in the project's docs folder instead of obj.

Run this from the `AdventureWorks` folder. The package name makes this command longer than the print line; type it as one line.

```shell
dotnet add package Microsoft.Extensions.ApiDescription.Server@11.0.0
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.Extensions.ApiDescription.Server' into project 'AdventureWorks\AdventureWorks.csproj'.
info : Restoring packages for AdventureWorks\AdventureWorks.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.11.42.14/vulnerability.update.json
info : Package 'Microsoft.Extensions.ApiDescription.Server' is compatible with all the specified frameworks in project 'AdventureWorks\AdventureWorks.csproj'.
info : PackageReference for package 'Microsoft.Extensions.ApiDescription.Server' version '11.0.0' added to file 'AdventureWorks\AdventureWorks.csproj'.
info : Generating MSBuild file AdventureWorks\obj\AdventureWorks.csproj.nuget.g.props.
(3 more lines)
```

In `AdventureWorks/AdventureWorks.csproj`, replace these lines. In the PropertyGroup.

```xml
    <UserSecretsId>494dd3b3-31fc-416b-a999-c0ad92a798c5</UserSecretsId>
```

With these:

```xml
    <UserSecretsId>494dd3b3-31fc-416b-a999-c0ad92a798c5</UserSecretsId>
    <OpenApiDocumentsDirectory>docs</OpenApiDocumentsDirectory>
```

Run this from the working folder. The build output lists the GenerateOpenApiDocuments step; the files are AdventureWorks/docs/AdventureWorks.json (the v1 document, the default name), AdventureWorks_store.json and AdventureWorks_v2.json.

```shell
dotnet build
```

### Activity requests and responses

6 of 6 requests in `Chapter14-Activity/requests.http` returned their commented status. They are the checkpoint's requests, with the same results; the Activity changes the build, not the app.

- `GET /openapi/store.json` returned **200**

  ```text
  {
    "openapi": "3.2.0",
    "info": {
      "title": "AdventureWorks Store API",
      "description": "Catalog, orders, accounts and Admin routes.",
      "version": "1.0"
    },
    "servers": [
      {
        "url": "https://localhos...
  ```

- `GET /openapi/v1.json` returned **200**

  ```text
  (the OpenAPI JSON document)
  ```

- `GET /openapi/v2.json` returned **200**

  ```text
  {
    "openapi": "3.2.0",
    "info": {
      "title": "AdventureWorks Products API",
      "description": "Minimal APIs, the current version.",
      "version": "2.0"
    },
    "servers": [
      {
        "url": "https://localhost:7001...
  ```

- `GET /openapi/v3.json` returned **404**

  ```text
  No OpenAPI document with the name 'v3' was found.
  ```

- `GET /api/v1/products/1` returned **200**

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1}
  ```

- `GET /api/v2/products/1` returned **200**

  ```text
  {"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1}
  ```

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| AdventureWorks.OpenApi.csproj | Removed | No new project; the app already references Microsoft.AspNetCore.OpenApi (Chapter 4). |
| Program.cs (Commerce Catalog API) | Replaced | `OpenApiDocuments.cs` in the app: three documents, with titles and versions; the products are the app's (Chapter 5's v1 and v2) instead of ProductV1 and ProductV2 records. |
| OpenApi:Use31 switch | Removed | The default is now OpenAPI 3.2.0. A tool that needs 3.1 or 3.0 can still get it with `options.OpenApiVersion` (one line per document); the text can name it without a listing. |
| Consumer/ (companion) | Removed | The Activity writes the documents at build time instead. |

## Notes for the writers

- Accuracy 14.1: Asp.Versioning ships separately; its newest is 10.2.3 for net10.0 and there is no 11.x yet. This app versions by URL segment (/api/v1, /api/v2) and needs no versioning package; say that the package adds version selection by header, query string or media type when an API needs it.
- Deprecation here is documentation only (`deprecated: true` on each v1 operation); the v1 responses do not carry a Deprecation or Sunset header.
- The store document also lists the Development-only routes when it is served in Development (/dev/sign-in, /examples), because they are mapped there. The build-time document (Activity) does not.
- A request for an unknown document name, /openapi/v3.json, answers 404 "No OpenAPI document with the name 'v3' was found." in Development.
- Program.cs is 88 lines.
