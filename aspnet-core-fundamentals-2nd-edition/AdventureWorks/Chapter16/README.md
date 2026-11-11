# AdventureWorks: Chapter 16 checkpoint

For the Visual Studio steps, use Visual Studio with support for the .NET 11 SDK and `.slnx` solutions.

The AdventureWorks store app at the end of Chapter 16. It adds OpenTelemetry traces and metrics for the order flow (the AdventureWorks.Orders source and meter: the orders.created counter and the orders.processing.duration histogram), a correlation ID that falls back to the request's trace ID, and two health routes: `/health/live` runs no checks, and `/health/ready` checks the database and InventoryService, answering 503 when either is slow or down. Chapter 17 starts from this folder.

| Folder | Project |
| --- | --- |
| `AdventureWorks` | Web: hosting, endpoints and composition |
| `AdventureWorks.Core` | Domain types and contracts |
| `AdventureWorks.Infrastructure` | Implementations of the Core contracts |

`AdventureWorks.slnx` lists every project in this folder. Core references nothing; Infrastructure references Core; Web references both.

## Run it

From this folder, the one that holds global.json:

```powershell
dotnet user-secrets set FeatureFlags:MaxCartItems 30 `
    --project AdventureWorks
dotnet user-secrets set Training:Password "Practice-Pass-2026!" `
    --project AdventureWorks
dotnet run --project InventoryService
```

To watch the telemetry in the app's window, use the telemetry launch profile: in Visual Studio, choose telemetry on the Start button's list and press F5; from this folder, run `dotnet run --project AdventureWorks --launch-profile telemetry`. It prints the spans for each request and order call and JSON log lines that carry the CorrelationId scope, and every 30 seconds the orders.created counter, the orders.processing.duration histogram and the framework's http.server.request.duration. Run the tests from this folder with `dotnet test --project AdventureWorks.Tests` (InventoryService and the app need not be running: the tests start the app in memory), or only the unit tests with `dotnet test --project AdventureWorks.Tests --filter-trait "Category=Unit"`. In Visual Studio, use Test Explorer. Then, in a second terminal in this folder, run `dotnet run --project AdventureWorks`. InventoryService is the warehouse stand-in on http://127.0.0.1:42132; the app calls it for stock. In Visual Studio, open AdventureWorks.slnx, choose Configure Startup Projects, select Multiple startup projects, set AdventureWorks and InventoryService to Start, and press F5. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. The two secrets are Chapter 2's harmless setting and Chapter 8's test-account password; set them once, because every later checkpoint keeps the same UserSecretsId. Any password with 12 or more characters, upper and lower case letters, a digit and a symbol works; requests.http uses this one. The first run creates adventureworks.db in the AdventureWorks folder and five test accounts: casey, jordan, morgan, riley and avery, each at @adventure-works.test. From Chapter 10 the app keeps its catalog, orders and stock in adventureworks.db (with SQLite's -wal and -shm files beside it while it runs). In Development the first run applies the migrations and seeds the data; each later run keeps every change. To start over, stop the app and run `dotnet ef database drop --force -s ..\AdventureWorks` from the AdventureWorks.Infrastructure folder. The dotnet-ef tool is pinned in dotnet-tools.json; run `dotnet tool restore` once in this folder before using it.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
Remove-Item cookies.txt -ErrorAction SilentlyContinue

# Live: the process answers; no checks run. Expect 200 with
# "Healthy"
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/health/live

# Ready: the database and the warehouse both answer. Expect 200
# with "Healthy"
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/health/ready

# No X-Correlation-ID: the response carries the request's trace ID
# instead. Expect 200, and it has an X-Correlation-ID header
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog

# A usable ID comes back unchanged. Expect 200, and it carries
# X-Correlation-ID: order-lab-1
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "X-Correlation-ID: order-lab-1" `
    https://localhost:7001/api/catalog

# A space makes the value unusable: replaced by the trace ID.
# Expect 200, and it has an X-Correlation-ID header
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "X-Correlation-ID: bad value" `
    https://localhost:7001/api/catalog

# Slow the warehouse down: 1.5 seconds for every call, its /health
# too. Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    http://127.0.0.1:42132/control/delay/1500

# Ready gives up after one second: Degraded, which this app maps
# to 503. Expect 503
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/health/ready

# Live runs no checks, so it still answers. Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/health/live

# Back to normal. Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    http://127.0.0.1:42132/control/reset

# Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/health/ready

# An order, so orders.created and the duration have data.
# Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/casey

# Expect 200
curl.exe -s -D - -o token.json -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/csrf
$token = Get-Content token.json | ConvertFrom-Json

# Expect 201
@'
{ "items": [ { "productId": 2, "quantity": 1 } ] }
'@ | Set-Content body.json
curl.exe -s -D - -o created.json -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($token.token)" `
    --data-binary "@body.json" https://localhost:7001/api/orders
$created = Get-Content created.json | ConvertFrom-Json

# Cancel it. Expect 204
curl.exe -i -w "\n" -X DELETE -b cookies.txt -c cookies.txt `
    -H "X-CSRF-TOKEN: $($token.token)" `
    "https://localhost:7001/api/orders/$($created.id)"

# The Admin removes it, so the file can run again. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/avery

# Expect 200
curl.exe -s -D - -o averyToken.json -b cookies.txt `
    -c cookies.txt https://localhost:7001/api/csrf
$averyToken = Get-Content averyToken.json | ConvertFrom-Json

# Expect 204
curl.exe -i -w "\n" -X DELETE -b cookies.txt -c cookies.txt `
    -H "X-CSRF-TOKEN: $($averyToken.token)" `
    "https://localhost:7001/api/admin/orders/$($created.id)"

# Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-out
```
