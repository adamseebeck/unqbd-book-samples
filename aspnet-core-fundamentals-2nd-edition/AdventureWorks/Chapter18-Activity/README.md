# AdventureWorks: Chapter 18 Activity solution

This is a worked solution to the chapter's Activity, built in a copy of the Chapter 18 checkpoint. Try the Activity yourself first. Chapter 18 is the last chapter; `../Chapter18` is the finished app.

It adds `AdventureWorks.CatalogFeed`, a small read-only price feed published with Native AOT, and `Dockerfile.aot`, which compiles it in the -aot SDK image and runs it on runtime-deps: `docker build -f Dockerfile.aot -t adventureworks-feed:ch18 .`, then `docker run --rm -p 8081:8080 adventureworks-feed:ch18` and http://localhost:8081/feed.

## Run it

From this folder, the one that holds global.json:

```powershell
dotnet user-secrets set FeatureFlags:MaxCartItems 30 `
    --project AdventureWorks
dotnet user-secrets set Training:Password "Practice-Pass-2026!" `
    --project AdventureWorks
dotnet run --project InventoryService
```

To run the app in containers instead (Docker, from this folder): `docker build -t adventureworks:ch18 .`, then `docker network create aw` and `docker volume create awdata`; apply the migrations with `docker run --rm -v awdata:/app/data --entrypoint /app/efbundle adventureworks:ch18`; start the warehouse with `docker run -d --name inventory --network aw -w /app/inventory adventureworks:ch18 InventoryService.dll` and the store with `docker run -d --name store --network aw -p 8080:8080 -v awdata:/app/data -e Inventory__BaseUrl=http://inventory:8080/ adventureworks:ch18`. It runs in Production on http://localhost:8080 (/health/live, /health/ready, /api/catalog), with an empty catalog: nothing seeds a Production database. Stop it with `docker stop -t 30 store`, which leaves room for the app's 15-second shutdown (docker stop waits only 10 seconds by default). `kubernetes.yaml` runs the same image on a cluster. Run the tests from this folder with `dotnet test --project AdventureWorks.Tests` (InventoryService and the app need not be running: the tests start the app in memory), or only the unit tests with `dotnet test --project AdventureWorks.Tests --filter-trait "Category=Unit"`. In Visual Studio, use Test Explorer. Then, in a second terminal in this folder, run `dotnet run --project AdventureWorks`. InventoryService is the warehouse stand-in on http://127.0.0.1:42132; the app calls it for stock. In Visual Studio 2026, open AdventureWorks.slnx, choose Configure Startup Projects, select Multiple startup projects, set AdventureWorks and InventoryService to Start, and press F5. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. The two secrets are Chapter 2's harmless setting and Chapter 8's test-account password; set them once, because every later checkpoint keeps the same UserSecretsId. Any password with 12 or more characters, upper and lower case letters, a digit and a symbol works; requests.http uses this one. The first run creates adventureworks.db in the AdventureWorks folder and five test accounts: casey, jordan, morgan, riley and avery, each at @adventure-works.test.

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
