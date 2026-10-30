# ASP.NET Core APIs Fundamentals, Second Edition: companion code

Code for *ASP.NET Core APIs Fundamentals, Second Edition: Building Modern Web APIs with .NET 11* by Adam Seebeck (unQbd). Each chapter folder holds the Warehouse API as it stands at the end of that chapter, plus the worked solution to the chapter's activity.

You can read and copy any file here in your browser. To run the projects, clone the repository or download it as a ZIP with the green **Code** button on GitHub.

## What you need

- The .NET 11 SDK, version 11.0.100 or later, from the [.NET 11 download page](https://dotnet.microsoft.com/download/dotnet/11.0). The `global.json` file in this folder selects it for every project below it and accepts later .NET 11 SDKs.
- Visual Studio 2026 with the ASP.NET and web development workload, or Visual Studio Code with C# Dev Kit. Every step also works from the command line.
- Projects target `net11.0` and list exact package versions. Packages that ship with each .NET release, such as ASP.NET Core and Entity Framework Core, use 11.0.0; other libraries follow their own version numbers, including Microsoft ones such as Microsoft.Extensions.Http.Resilience, the test SDK and the Azure libraries.
- Some later chapters need more: SQL Server Express LocalDB 2022 on Windows (or a SQL Server 2022 container) for the data chapters, Docker with Linux containers for the deployment chapter, and the `dotnet user-jwts` tool, which ships with the SDK, for the security chapters. The chapter tells you when.

## Run a chapter

```shell
cd Chapter01/WarehouseApi
dotnet --version
dotnet run
```

`dotnet --version` should print 11.0.100 or a later 11.0 version. `dotnet run` uses the project's `http` launch profile, which sets the Development environment and listens on `http://localhost:5080`. Stop the app with Ctrl+C. To use another port, run `dotnet run --urls http://localhost:5199`.

Each project folder has an `.http` file with the chapter's requests. Visual Studio 2026 runs it directly; in Visual Studio Code, use an extension that runs `.http` files, such as REST Client. You can also use curl:

```shell
curl -i http://localhost:5080/api/warehouses/MAIN
```

In Windows PowerShell, type `curl.exe` so the command runs curl rather than the `Invoke-WebRequest` alias.

The Chapter 2 activity reads its notification email from user secrets, which live in your user profile rather than in the project folder:

```shell
cd Chapter02/Activity
dotnet user-secrets set "InventoryThresholds:NotificationEmail" "stock-alerts@example.com"
dotnet run
```

Without that secret, the application stops at startup and names the missing setting. That is the options validation from Chapter 2 at work.

## Chapter map

| Chapter | Folder | What it contains | Try this request |
| --- | --- | --- | --- |
| 1 | `Chapter01/WarehouseApi` | The first Minimal API endpoint: a warehouse summary by code | `GET /api/warehouses/MAIN` |
| 1 | `Chapter01/Activity` | A product summary by SKU with case-insensitive lookup and a 404 path | `GET /api/products/SKU-001` |
| 2 | `Chapter02/WarehouseApi` | The Chapter 1 project organized into folders, with options classes bound to configuration and validated at startup | `GET /api/health/config` |
| 2 | `Chapter02/Activity` | Inventory thresholds, with the notification email kept in user secrets | `GET /api/inventory/stock-status?sku=SKU-001&quantity=3` |
| 3 | `Chapter03/WarehouseApi` | Singleton, Scoped and factory registrations with scope validation, and a Scoped service that logs when its request ends | `GET /api/inventory/SKU-001?warehouseCode=MAIN` |
| 3 | `Chapter03/Activity` | A low-stock alert service registered with a lifetime that matches its dependencies | `GET /api/alerts/low-stock?threshold=10` |
| 4 | `Chapter04/WarehouseApi` | Minimal API endpoints for products, stock levels, adjustments and health, organized in extension methods | `GET /api/products` |
| 4 | `Chapter04/Activity` | Low-stock alerts for one warehouse | `GET /api/warehouses/WH-001/alerts/low-stock` |
| 5 | `Chapter05/WarehouseApi` | Product and warehouse contracts, a custom SKU route constraint and `[AsParameters]` query binding | `GET /api/products?searchTerm=caster&pageNumber=1` |
| 5 | `Chapter05/Activity` | Stock adjustment records behind a warehouse code route constraint, filtered from the query string | `GET /api/warehouses/WH-001/stock-adjustments?reasonCode=Damage` |
| 6 | `Chapter06/WarehouseApi` | Paged, filtered and sorted stock in a route group, with idempotent stock adjustments | `GET /api/warehouses/WH-001/stock?pageSize=1` |
| 6 | `Chapter06/Activity` | An idempotent POST that creates a stock item and replays its 201 response | `POST /api/warehouses/WH-002/stock` |
| 7 | `Chapter07/WarehouseApi` | An orders controller beside the Minimal API endpoints, with a shared order store | `POST /api/orders` |
| 7 | `Chapter07/Activity` | Order search with filters and a paged result | `GET /api/orders/search?status=Pending` |
| 8 | `Chapter08/WarehouseApi` | Validation for controllers and Minimal APIs, exception handling, correlation IDs and CORS | `POST /api/products` with the SKU `WIDGET` |
| 8 | `Chapter08/Activity` | A warehouse code attribute, request timing middleware and a body-based adjustment endpoint | `POST /api/stock-adjustments` |
| 9 | `Chapter09/WarehouseApi` | Services behind interfaces over one in-memory store, with order workflows that allocate, fulfill and cancel | `POST /api/orders/{id}/allocate` |
| 9 | `Chapter09/Activity` | The orders controller rewritten as a Minimal API route group | `GET /api/orders?status=Pending` |
| 10 | `Chapter10/WarehouseApi` | EF Core with SQL Server: Fluent API mappings, row version concurrency tokens, migrations and stored idempotency results | `GET /api/warehouses/WH-001/stock` |
| 10 | `Chapter10/Activity` | Low-stock items for one warehouse, read from an index that includes the stock levels | `GET /api/warehouses/WH-001/low-stock` |
| 11 | `Chapter11/WarehouseApi` | Projections, transactions inside the execution strategy, bounded retries for reservations and OpenTelemetry tracing | `GET /api/stock?belowReorderThreshold=true` |
| 11 | `Chapter11/Activity` | A release of part of an order line's reservation, in one transaction, with an idempotency key | `POST /api/releases` |
| 12 | `Chapter12/WarehouseApi` | Domain exceptions, an `IExceptionHandler` and Problem Details for the API's error responses | `POST /api/orders/{id}/allocate` for more units than WH-001 holds |
| 12 | `Chapter12/Activity` | Concurrency conflicts as Problem Details that name the changed order or stock item, and integration tests in `Chapter12/Activity.Tests` | `dotnet test` in `Chapter12/Activity.Tests` |
| 13 | `Chapter13/WarehouseApi` | JWT bearer authentication with development tokens, role and warehouse policies on every stock route and the stock list, orders that only their creator, their warehouse's staff and supervisors read, CORS origins from configuration, Problem Details for 401 and 403, HTTPS outside Development | `GET /api/warehouses/WH-001/stock` with a WH-001 staff token |
| 13 | `Chapter13/Activity` | Stock history for supervisors and for inventory auditors with a clearance claim | `GET /api/stock-adjustments/audit` |
| 14 | `Chapter14/WarehouseApi` | URL segment versions 1.0 (deprecated, with a sunset date) and 2.0, the Chapter 13 URLs still answering as 1.0, one OpenAPI document per version, Swagger UI, and XML from the orders controller | `GET /api/v2/warehouses/WH-001` |
| 14 | `Chapter14/Activity` | Versioned stock adjustment history with V1 and V2 contracts in JSON or XML | `GET /api/v2/stock-adjustments?warehouseCode=WH-001` |
| 15 | `Chapter15/WarehouseApi` | Shipment requests that commit with the reservation, a bounded queue, a background processor that resumes pending work at startup and sweeps for it while it runs, and a typed ERP client with the standard resilience handler | `POST /api/v2/orders/{id}/shipment`, then `GET /api/v2/orders/{id}/shipment` |
| 15 | `Chapter15/FakeErp` | A local stand-in for the ERP shipment API that fails, loses responses or slows down when you ask it to | `GET http://localhost:5090/api/shipments` |
| 15 | `Chapter15/Activity` | Failed shipment requests in a dead-letter queue, retried on a schedule, with a health check and a status endpoint for supervisors | `GET /api/v2/admin/dead-letter/status` |
| 16 | `Chapter16/WarehouseApi` | Liveness and readiness health checks, output caching for the catalog, rate limiting for order creation, forwarded headers and Azure Key Vault configuration | `GET /health` |
| 16 | `Chapter16/WarehouseApi.Tests` | Unit tests, and integration tests with `WebApplicationFactory` on a LocalDB database per test class | `dotnet test` in `Chapter16/WarehouseApi.Tests` |
| 16 | `Chapter16/Activity` | A health check for the ERP at `/health/dependencies`, with its tests in `Chapter16/Activity.Tests` | `GET /health/dependencies` |

Each `WarehouseApi` folder is a complete checkpoint: the previous chapter's project plus that chapter's changes, so the same type names appear in several chapters. When a chapter replaces or deletes a file, the chapter text says which one and why; Chapter 4, for example, removes the demonstration code of Chapters 1 to 3. Open or run one folder at a time; do not copy files from different chapters into one project. The in-memory stores in the early chapters stand in for the database that the Entity Framework Core chapters add.

From Chapter 6 on, stock adjustments, from Chapter 7 on, new orders, and in the Chapter 11 activity, stock releases need an `Idempotency-Key` header with a value the client generates, such as a new GUID. The `.http` files include one: send a request twice with the same key, and the second response replays the first without changing the stock again or creating a second order. The Chapter 7 `.http` file, and most of the ones from Chapter 9 on, use an `@orderId` variable; paste the ID from the `Location` header of the order you create before you send the requests that use it.

Chapter 3's `Program.cs` contains one commented registration, the captive dependency from the chapter. Remove the comment marker and run the project to watch scope validation stop the application at startup.

## The data chapters

From Chapter 10 on, the Warehouse API stores its data in SQL Server through Entity Framework Core. The `appsettings.Development.json` file of each project connects to SQL Server Express LocalDB with your Windows account and uses a database named `WarehouseDb`. Restore the tools once, from this folder, then create the database from the project folder:

```shell
dotnet tool restore
cd Chapter10/WarehouseApi
dotnet ef database update
dotnet run
```

In the Development environment, the application also applies missing migrations at startup and adds the sample warehouses, products and stock when the tables are empty. To use another SQL Server, set its connection string in the `ConnectionStrings__WarehouseDb` environment variable. Outside Development, the application stops at startup when no connection string is configured. For SQL Server 2019 or an older LocalDB, Chapter 10 shows the one-line change that targets it.

Each checkpoint keeps the previous chapter's migrations, and Chapter 11 adds one, so Chapters 10, 11 and 12 can share one database. The Chapter 10 activity adds a migration that the later chapters do not have. To start again from an empty database, run `dotnet ef database drop` in a project folder; the next `dotnet ef database update` or Development run creates it again.

From Chapter 11 on, the console exporter of OpenTelemetry writes every finished trace span to the console, so a block of `Activity.` lines follows each request in the output of `dotnet run`.

The Chapter 12 activity comes with a test project. Run `dotnet test` in `Chapter12/Activity.Tests`. The tests run the API in the Production environment with a test double in place of the order service, so they need no database.

## The security and versioning chapters

From Chapter 13 on, every endpoint under `/api` needs a bearer token; only `/health` stays open. Create development tokens with `dotnet user-jwts` in the project folder:

```shell
cd Chapter13/WarehouseApi
dotnet user-jwts create --role Supervisor --output token
dotnet user-jwts create --role WarehouseStaff --claim WarehouseId=WH-001 --output token
dotnet user-jwts create --name atlanta-storefront --role IntegrationClient --claim WarehouseId=WH-002 --output token
```

Each command prints a token. Paste it into the token variables at the top of the project's `.http` file, or send it in an `Authorization: Bearer` header. The tool keeps its signing key in user secrets in your user profile, never in the project folder. The projects from Chapter 13 on share one `UserSecretsId`, so a token you create in one of them works in the others. Tokens expire after three months; `dotnet user-jwts list` shows them and `dotnet user-jwts clear` removes them. The command also writes the issuer and the audiences into `appsettings.Development.json`; the file already contains them, so only its formatting may change. Do not pass `--audience` for these projects: it replaces the audiences the API accepts. `--name` sets the token's subject; an integration client reads the orders it created, so give each client token its own name.

Outside Development, the application stops at startup until an identity provider is configured, for example with the `Authentication__Schemes__Bearer__Authority` and `Authentication__Schemes__Bearer__ValidAudiences__0` environment variables, and it redirects plain HTTP requests to HTTPS when it knows the HTTPS port.

From Chapter 14 on, routes under `/api` name their version: `/api/v1/...` still works and its responses announce the retirement date in `Deprecation` and `Sunset` headers, and `/api/v2/...` is the current version. The Chapter 13 URLs without a version segment answer as version 1.0 until its sunset. In Development, the OpenAPI documents are at `/openapi/v1.json` and `/openapi/v2.json`, and Swagger UI at `http://localhost:5080/swagger` lists the operations of each version and marks those that need a token. The orders controller answers in XML when a request sends `Accept: application/xml`.

## The background processing and production chapters

Chapter 15 sends a shipment request to an ERP system after an order is allocated, and `Chapter15/FakeErp` plays that system on your machine. The API needs an ERP key at startup; keep it in user secrets:

```shell
cd Chapter15/WarehouseApi
dotnet user-secrets set "ExternalShipmentService:ApiKey" "local-development-key"
```

Then, from the `Chapter15` folder, start the fake ERP with `dotnet run --project FakeErp` in one terminal and the API with `dotnet run --project WarehouseApi` in another. The fake ERP listens on `http://localhost:5090`, the address in the API's `appsettings.Development.json`, and accepts any key. Without the key, the API stops at startup and names the missing setting; the shared `UserSecretsId` makes the same key work for Chapter 16 and the activities.

`POST /api/v2/orders/{id}/shipment` answers 202 Accepted with a `Location` header that points to the shipment status at the same address; the allocate endpoint still answers 200 with the order in both versions, and it queues the shipment as well. To watch the retries, start the fake ERP with `--FakeErp:FailFirstRequests=1 --FakeErp:LoseFirstResponses=1`, or send `POST http://localhost:5090/fake/outage?requests=3` while it runs. The ERP client's retry, timeout and circuit breaker settings are in the `ExternalShipmentService:Resilience` section of the API's `appsettings.json`.

The Chapter 15 checkpoint adds a migration for shipments. Orders that are already allocated in your database get a pending shipment, which the processor sends at its next start, so start the fake ERP first. While it runs, the processor also sends every pending shipment that waited longer than `Fulfillment:SweepIntervalSeconds`, 60 seconds by default. The Chapter 15 activity adds a migration that Chapter 16 does not have.

From Chapter 16 on, `/health/live` answers as long as the application runs, and `/health` reports the database and the fulfillment queue as JSON; neither needs a token. Order creation accepts 10 requests per client per minute and answers the next one with 429 and a `Retry-After` header, and the product catalog responses are cached for five minutes.

The Chapter 16 test projects use SQL Server Express LocalDB. Each test class creates a database with a random name and drops it when it finishes, so the tests never touch `WarehouseDb`. To run them against another SQL Server, set the `WAREHOUSE_TEST_SQLSERVER` environment variable to its connection string without a database name.

```shell
cd Chapter16/WarehouseApi.Tests
dotnet test
```

To build the container image, run Docker with Linux containers from this folder, which holds `global.json`:

```shell
docker build -f Chapter16/Dockerfile -t warehouse-api:local .
```

The image runs in the Production environment as a non-root user and listens on port 8080. Step 11 of the chapter shows the `docker run` command with the settings it needs, including a SQL Server it can reach over the network, because LocalDB accepts only local connections, and the schema applied before the container starts. The `.dockerignore` file in this folder keeps build output, local settings and secret files out of the image.

`Chapter16/ci/dotnet.yml` is the chapter's GitHub Actions workflow; copy it to `.github/workflows/dotnet.yml` in a repository whose root holds `global.json` and the `Chapter16` folder. Its test job needs no setup. The deployment jobs run only for pushes to `main` and need Azure resources and repository variables first: `Chapter16/deploy/README.md` lists the Azure CLI commands that create them, including HTTPS Only, the slot settings that keep the staging slot's fulfillment processor off and give the slot its own token audience, the container stop time and the swap warm-up, the steps of a release, and the commands that remove the resources. Azure charges for these resources while they exist.

## Tools and secrets

The local tool manifest in `.config/dotnet-tools.json` pins the Entity Framework Core command-line tool, `dotnet-ef`, at 11.0.0 for the data chapters. Run `dotnet tool restore` from this folder before using `dotnet ef`; nothing is installed machine-wide.

Keep connection strings with passwords, API keys and tokens out of these folders. Use user secrets during development and environment variables or a secret store in production. The `.http` files contain placeholders where the tokens go; do not commit real tokens. The `.gitignore` file excludes build output, local databases and common secret files.

## About the book

The book explains every example and the decisions behind it. More programming books are at [unqbd.com](https://www.unqbd.com).
