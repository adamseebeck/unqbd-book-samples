# AdventureWorks: the running project

One AdventureWorks outdoor-gear store app that grows through the book. The reader creates it in Chapter 1, and every chapter starts where the previous one ended. Each `ChapterNN` folder is a full snapshot of the app at the end of that chapter: the previous chapter's snapshot plus that chapter's change. It builds and runs on its own.

This folder holds Chapters 1 to 18. The `Chapter01` to `Chapter18` folders beside it belong to the current printing of the book and are unchanged.

## Folders

| Folder | What it is |
| --- | --- |
| `ChapterNN` | The checkpoint at the end of Chapter NN. The next chapter starts here. |
| `ChapterNN-Activity` | A copy of that checkpoint plus a worked solution to the chapter's Activity. The next chapter does not start from it. |
| `Chapter15-DebugLab` | A copy of the Chapter 15 checkpoint with one deliberate bug and the failing test that finds it, for the chapter's Visual Studio debugging lab. |
| `notes/ChapterNN-delta.md` | For the book's writers: every file and line the chapter adds or changes, in order, with the commands, their output and the real responses. |

Each checkpoint folder is the reader's working folder: it holds `global.json` and, from Chapter 2, `AdventureWorks.slnx` with three projects (from Chapter 12 also `InventoryService`, a companion tool project that stands in for the warehouse's stock service):

| Project | Holds |
| --- | --- |
| `AdventureWorks` | Web: hosting, middleware, endpoints, pages and composition |
| `AdventureWorks.Core` | Domain types and contracts, with no ASP.NET Core dependency |
| `AdventureWorks.Infrastructure` | Implementations of the Core contracts: in memory until Chapter 10, then EF Core on SQLite |

## Run a checkpoint

You need the .NET 11 SDK, version 11.0.100 or later; `global.json` selects it. From a checkpoint folder:

```shell
dotnet run --project AdventureWorks
```

The app listens on https://localhost:7001 and http://localhost:5001 in Development. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. From Chapter 2 the checkpoints read one harmless User Secret (`FeatureFlags:MaxCartItems` = 30); each README shows the command that sets it.

Every checkpoint has a `requests.http` file with each request and the status you should see, and a README with the same requests as curl.exe commands for Windows PowerShell 5.1.

## What each chapter adds

| Chapter | Adds |
| --- | --- |
| 1 | The web host: Kestrel limits, request timing, `/`, `/api/status`, `/health` |
| 2 | Core and Infrastructure projects, validated FeatureFlags options, three service lifetimes, `/api/orders/{id}`, User Secrets |
| 3 | Correlation IDs, Problem Details for domain errors, status-code pages, a `/ping` branch and an `/api` branch |
| 4 | The product catalog (`/api/catalog`), a custom route constraint, named endpoints and links, OpenAPI, feature files |
| 5 | The product API as controllers (`/api/v1/products`) and Minimal APIs (`/api/v2/products`) over one store, with validation and filters |
| 6 | Razor Pages over the same store: product list and edit pages with a version check, a View Component and an Admin area |
| 7 | The order form with a private upload, `/api/orders/validate`, built-in Minimal API validation, an async SKU check, static files and en/fr request localization |
| 8 | ASP.NET Core Identity on SQLite (`adventureworks.db`), sign-in and profile pages, JWT and refresh tokens for `/api/auth`, a Bearer-only loyalty API, a Development seed and test sign-in |
| 9 | Authorization: a fallback policy with every public endpoint marked, Admin, age, department and resource-based order policies, order delete and archive, the X-CSRF-TOKEN header |
| 10 | EF Core: the store's tables in the same SQLite database, migrations (dotnet-ef pinned in dotnet-tools.json), EF Core implementations behind the same interfaces, prices in cents, the order report, a version-checked notes update and a transactional stock transfer. The Activity runs it on SQL Server LocalDB |
| 11 | An application service for placing and cancelling orders (server-side prices, owner from the sign-in), narrow repositories, one unit of work, a logging decorator, and scope validation in every environment |
| 12 | Output caching of the anonymous catalog with tag eviction on every product write, a partitioned rate limit on search, compression for the public catalog only, and warehouse stock from the InventoryService tool project through a resilient, cached HttpClient |
| 13 | The order API as REST resources on the same service: a paged collection with Link headers, OrderDto with an ETag, PATCH with If-Match, DELETE as cancel, the Admin delete under /api/admin, and source-generated JSON |
| 14 | Three OpenAPI documents: the store routes, and the product API's v1 (controllers, marked deprecated) and v2 (Minimal APIs), chosen by group name. The Activity writes them at build time |
| 15 | AdventureWorks.Tests on Microsoft.Testing.Platform: two Moq unit tests for the order service, an architecture test, and three integration tests through one small WebApplicationFactory with in-memory SQLite and a Test sign-in scheme. Chapter15-DebugLab holds the debugging lab: one deliberately failing test |
| 16 | OpenTelemetry traces and metrics for the order flow (orders.created, orders.processing.duration), the console or OTLP exporter by configuration, the correlation ID from the trace ID, and `/health/live` and `/health/ready` with database and InventoryService checks |
| 17 | A price-feed import for the Admin (`/api/admin/products/import`), read as a bounded stream, and AdventureWorks.Benchmarks (BenchmarkDotNet) comparing Split with spans. The Activity benchmarks ArrayPool |
| 18 | One container image for the store, the warehouse and an EF Core migrations bundle, a .dockerignore, kubernetes.yaml with the health probes, and a 15-second graceful shutdown. The Activity adds a small Native AOT price feed and its image |
