# AdventureWorks: Chapter 18 checkpoint

The AdventureWorks store app at the end of Chapter 18, the last checkpoint. It adds a Dockerfile that builds one image for the store, the warehouse service and an EF Core migrations bundle, a `.dockerignore`, a `kubernetes.yaml` that runs that image with Chapter 16's probes, and a 15-second graceful shutdown.

| Folder | Project |
| --- | --- |
| `AdventureWorks` | Web: hosting, endpoints and composition |
| `AdventureWorks.Core` | Domain types and contracts |
| `AdventureWorks.Infrastructure` | Implementations of the Core contracts |

`AdventureWorks.slnx` lists all three. Core references nothing; Infrastructure references Core; Web references both.

## Run it

From this folder, the one that holds global.json:

```powershell
dotnet user-secrets set FeatureFlags:MaxCartItems 30 `
    --project AdventureWorks
dotnet user-secrets set Training:Password "Practice-Pass-2026!" `
    --project AdventureWorks
dotnet run --project InventoryService
```

To run the app in containers instead (Docker, from this folder): `docker build -t adventureworks:ch18 .`, then `docker network create aw` and `docker volume create awdata`; apply the migrations with `docker run --rm -v awdata:/app/data --entrypoint /app/efbundle adventureworks:ch18`; start the warehouse with `docker run -d --name inventory --network aw -w /app/inventory adventureworks:ch18 InventoryService.dll` and the store with `docker run -d --name store --network aw -p 8080:8080 -v awdata:/app/data -e Inventory__BaseUrl=http://inventory:8080/ adventureworks:ch18`. It runs in Production on http://localhost:8080 (/health/live, /health/ready, /api/catalog), with an empty catalog: nothing seeds a Production database. Stop it with `docker stop -t 30 store`, which leaves room for the app's 15-second shutdown (docker stop waits only 10 seconds by default). `kubernetes.yaml` runs the same image on a cluster. Run the tests from this folder with `dotnet test --project AdventureWorks.Tests` (InventoryService and the app need not be running: the tests start the app in memory), or only the unit tests with `dotnet test --project AdventureWorks.Tests --filter-trait "Category=Unit"`. In Visual Studio, use Test Explorer. Then, in a second terminal in this folder, run `dotnet run --project AdventureWorks`. InventoryService is the warehouse stand-in on http://127.0.0.1:42132; the app calls it for stock. In Visual Studio 2026, open AdventureWorks.slnx, choose Configure Startup Projects, select Multiple startup projects, set AdventureWorks and InventoryService to Start, and press F5. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. The two secrets are Chapter 2's harmless setting and Chapter 8's test-account password; set them once, because every later checkpoint keeps the same UserSecretsId. Any password with 12 or more characters, upper and lower case letters, a digit and a symbol works; requests.http uses this one. The first run creates adventureworks.db in the AdventureWorks folder and five test accounts: casey, jordan, morgan, riley and avery, each at @adventure-works.test. From Chapter 10 the app keeps its catalog, orders and stock in adventureworks.db (with SQLite's -wal and -shm files beside it while it runs). In Development the first run applies the migrations and seeds the data; each later run keeps every change. To start over, stop the app and run `dotnet ef database drop --force -s ..\AdventureWorks` from the AdventureWorks.Infrastructure folder. The dotnet-ef tool is pinned in dotnet-tools.json; run `dotnet tool restore` once in this folder before using it.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
# The process answers. Expect 200
curl.exe -i -w "\n" https://localhost:7001/health/live

# The database and the warehouse answer. Expect 200
curl.exe -i -w "\n" https://localhost:7001/health/ready

# The public catalog. Expect 200 with "sku": "HLM-1001"
curl.exe -i -w "\n" https://localhost:7001/api/catalog
```
