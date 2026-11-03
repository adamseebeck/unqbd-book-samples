# AdventureWorks: the running project

One AdventureWorks outdoor-gear store app grows through the book. You create it in Chapter 1, and every chapter starts where the previous one ended. Each `ChapterNN` folder is a full snapshot of the app at the end of that chapter: the previous chapter's snapshot plus that chapter's change. It builds and runs on its own, so you can start any chapter from the folder before it.

This folder holds all 18 chapters. The `Chapter01` to `Chapter18` folders beside it belong to the current printing of the book and are unchanged.

## What you need

- The .NET 11 SDK, version 11.0.100 or later. Each checkpoint's `global.json` selects it.
- Visual Studio 2026, or VS Code with the C# Dev Kit extension. Every step also works from a terminal with the `dotnet` command.
- Chapter 10's Activity only: SQL Server LocalDB, which Visual Studio 2026 installs on Windows.
- Chapter 18 only: Docker, or another engine that runs Linux containers. Its Activity compiles the Native AOT image inside Docker, so your PC needs no native compiler.

## Folders

| Folder | What it is |
| --- | --- |
| `ChapterNN` | The checkpoint at the end of Chapter NN. The next chapter starts here. |
| `ChapterNN-Activity` | A copy of that checkpoint plus a worked solution to the chapter's Activity. Try the Activity yourself first. The next chapter does not start from it. |
| `Chapter15-DebugLab` | A copy of the Chapter 15 checkpoint with one deliberate bug and the test that fails because of it, for the chapter's Visual Studio debugging lab. The chapter shows how to find the bug; the folder keeps it. |
| `notes/ChapterNN-delta.md` | Notes for the book's writers: every file and line the chapter adds or changes, in order, with the commands, their output and the responses. |

Each checkpoint folder is your working folder: it holds `global.json` and, from Chapter 2, `AdventureWorks.slnx`. The solution grows with the book:

| Project | Holds | From |
| --- | --- | --- |
| `AdventureWorks` | Web: hosting, middleware, endpoints, pages and composition | Chapter 1 |
| `AdventureWorks.Core` | Domain types and contracts, with no ASP.NET Core dependency | Chapter 2 |
| `AdventureWorks.Infrastructure` | Implementations of the Core contracts: in memory, then EF Core on SQLite from Chapter 10 | Chapter 2 |
| `InventoryService` | A small stand-in for the warehouse's stock service, on http://127.0.0.1:42132 | Chapter 12 |
| `AdventureWorks.Tests` | Unit and integration tests on Microsoft.Testing.Platform | Chapter 15 |
| `AdventureWorks.Benchmarks` | BenchmarkDotNet benchmarks | Chapter 17 |
| `AdventureWorks.CatalogFeed` | A small Native AOT service (Chapter 18's Activity only) | Chapter 18 |

## Run a checkpoint

Open a terminal in the checkpoint folder, the one that holds `global.json`. Every command runs from there.

```powershell
dotnet user-secrets set FeatureFlags:MaxCartItems 30 `
    --project AdventureWorks
dotnet run --project AdventureWorks
```

The app listens on https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. In Visual Studio 2026, open `AdventureWorks.slnx` and press F5. In VS Code, open the folder, let C# Dev Kit load the solution, and press F5 or use the terminal commands.

Some chapters need one more step. Each checkpoint's README lists its own.

- **From Chapter 2:** the User Secret above, `FeatureFlags:MaxCartItems`. Set it once; every later checkpoint keeps the same UserSecretsId.
- **From Chapter 8:** the test accounts' password, `dotnet user-secrets set Training:Password "Practice-Pass-2026!" --project AdventureWorks`. Any password with 12 or more characters, upper and lower case letters, a digit and a symbol works.
- **From Chapter 10:** the app keeps its data in `AdventureWorks/adventureworks.db`. In Development the first run applies the migrations and adds sample data.
- **Chapter 10's Activity:** the `sqlserver` launch profile runs the same store on LocalDB: `dotnet run --project AdventureWorks --launch-profile sqlserver`.
- **From Chapter 12:** start InventoryService first, in a second terminal: `dotnet run --project InventoryService`. In Visual Studio, choose Configure Startup Projects and start both AdventureWorks and InventoryService.
- **From Chapter 15:** run the tests with `dotnet test --project AdventureWorks.Tests`, or from Test Explorer. They start the app in memory, so nothing else needs to run.
- **From Chapter 16:** the `telemetry` launch profile prints traces, metrics and JSON logs to the app's window: `dotnet run --project AdventureWorks --launch-profile telemetry`, or choose telemetry on Visual Studio's Start button.
- **Chapter 17:** run the benchmarks in Release: `dotnet run -c Release --project AdventureWorks.Benchmarks -- --filter "*PriceFeed*" --job short`.
- **Chapter 18:** build and run the containers with the Docker commands in its README.

## Sending requests

Every checkpoint has a `requests.http` file with each request in order and the status you should see. In Visual Studio 2026, open it and select **Send Request** above a request; in VS Code, install the REST Client extension first. Each README has the same requests as curl.exe commands for Windows PowerShell 5.1. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command.

## What each chapter adds

| Chapter | The checkpoint adds | The Activity adds |
| --- | --- | --- |
| 1 | The web host: Kestrel limits, request timing, `/`, `/api/status`, `/health` | Environment logging in Development and a diagnostic `/api/config` route |
| 2 | Core and Infrastructure projects, validated FeatureFlags options, three service lifetimes, `/api/orders/{id}`, User Secrets | Validated PaymentOptions and a Development-only `/api/payment/config` |
| 3 | Correlation IDs, Problem Details for domain errors, status-code pages, a `/ping` branch and an `/api` branch | Security-header and idempotency-key middleware, a `/webhooks` branch and `POST /api/payments` |
| 4 | The product catalog (`/api/catalog`), a custom route constraint, named endpoints and links, OpenAPI, feature files | A SKU-format route constraint and a Product Management route group |
| 5 | The product API as controllers (`/api/v1/products`) and Minimal APIs (`/api/v2/products`) over one store, with validation and filters | A price-quote group, `/api/quotes`, with typed results and a timing filter |
| 6 | Razor Pages over the same store: product list and edit pages with a version check, a View Component and an Admin area | A customer profile page, a RecentOrders View Component and a Deactivate handler |
| 7 | The order form with a private upload, `/api/orders/validate`, built-in Minimal API validation, an async SKU check, static files and en/fr localization | A separate host that shows .NET 11's header-only antiforgery check, with CORS |
| 8 | ASP.NET Core Identity on SQLite, sign-in and profile pages, JWT and refresh tokens for `/api/auth`, a Bearer-only loyalty API, sample accounts and a test sign-in | A Development-only route that changes a loyalty number, to watch the claim change |
| 9 | Authorization: a fallback policy, Admin, age, department and resource-based order policies, order delete and archive, the X-CSRF-TOKEN header | A Shipping account, an order hold behind a department policy, and a policy probe |
| 10 | EF Core: the store's tables in the same SQLite database, migrations, EF Core repositories, prices in cents, the order report, a version-checked notes update and a transactional stock transfer | The same store on SQL Server LocalDB, with its own migrations |
| 11 | An order service for placing and cancelling orders, narrow repositories, one unit of work, a logging decorator, and scope validation in every environment | An auditing decorator and an Admin audit route |
| 12 | Output caching with tag eviction, a partitioned rate limit, compression for the public catalog, and warehouse stock from InventoryService through a resilient, cached HttpClient | HybridCache, so concurrent misses make one call |
| 13 | The order API as REST resources: a paged collection with Link headers, ETags, PATCH with If-Match, DELETE as cancel, the Admin delete under /api/admin, and source-generated JSON | An order's lines as a paged sub-collection |
| 14 | Three OpenAPI documents: the store routes, and the product API's v1 (controllers, deprecated) and v2 (Minimal APIs) | The documents written to files at build time |
| 15 | AdventureWorks.Tests: two Moq unit tests for the order service, an architecture test, and three integration tests through one small WebApplicationFactory with in-memory SQLite and a test sign-in | A verified call, a cancelled request and a one-test override. The debugging lab is `Chapter15-DebugLab` |
| 16 | OpenTelemetry traces and metrics for the order flow, a telemetry launch profile, the correlation ID from the trace ID, and `/health/live` and `/health/ready` | A counter of cancelled orders, with its test |
| 17 | A price-feed import for the Admin (`/api/admin/products/import`), read as a bounded stream, and AdventureWorks.Benchmarks | A benchmark of new arrays against ArrayPool |
| 18 | One container image for the store, the warehouse and the database migrations, a .dockerignore, `kubernetes.yaml` with the health probes, and a 15-second graceful shutdown | A small Native AOT price feed and its container image |
