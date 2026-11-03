# AdventureWorks: Chapter 15 Activity solution

This is a worked solution to the chapter's Activity, built in a copy of the Chapter 15 checkpoint. Try the Activity yourself first. The next chapter starts from `../Chapter15`, not from this folder.

`ActivityTests.cs` adds a Moq Verify of the catalog call and its cancellation token, a cancelled request that saves nothing, and a WithWebHostBuilder override whose stub fails stock calls for one test only (503 after one call and two retries).

## Run it

From this folder, the one that holds global.json:

```powershell
dotnet user-secrets set FeatureFlags:MaxCartItems 30 `
    --project AdventureWorks
dotnet user-secrets set Training:Password "Practice-Pass-2026!" `
    --project AdventureWorks
dotnet run --project InventoryService
```

Run the tests from this folder with `dotnet test --project AdventureWorks.Tests` (InventoryService and the app need not be running: the tests start the app in memory), or only the unit tests with `dotnet test --project AdventureWorks.Tests --filter-trait "Category=Unit"`. In Visual Studio, use Test Explorer. Then, in a second terminal in this folder, run `dotnet run --project AdventureWorks`. InventoryService is the warehouse stand-in on http://127.0.0.1:42132; the app calls it for stock. In Visual Studio 2026, open AdventureWorks.slnx, choose Configure Startup Projects, select Multiple startup projects, set AdventureWorks and InventoryService to Start, and press F5. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. The two secrets are Chapter 2's harmless setting and Chapter 8's test-account password; set them once, because every later checkpoint keeps the same UserSecretsId. Any password with 12 or more characters, upper and lower case letters, a digit and a symbol works; requests.http uses this one. The first run creates adventureworks.db in the AdventureWorks folder and five test accounts: casey, jordan, morgan, riley and avery, each at @adventure-works.test.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
# The unversioned store routes, in OpenAPI 3.2. Expect 200 with
# "openapi": "3.2.0", and the body lacks "/api/v1/products"
curl.exe -i -w "\n" https://localhost:7001/openapi/store.json

# Version 1 of the product API: the controllers, marked
# deprecated. Expect 200 with "deprecated": true, and the body
# lacks "/api/v2/products"
curl.exe -i -w "\n" https://localhost:7001/openapi/v1.json

# Version 2: the Minimal APIs. Expect 200 with "version": "2.0",
# and the body lacks "deprecated"
curl.exe -i -w "\n" https://localhost:7001/openapi/v2.json

# No such document. Expect 404
curl.exe -i -w "\n" https://localhost:7001/openapi/v3.json

# Both versions still answer. Expect 200 with "sku": "HLM-1001"
curl.exe -i -w "\n" https://localhost:7001/api/v1/products/1

# Expect 200 with "sku": "HLM-1001"
curl.exe -i -w "\n" https://localhost:7001/api/v2/products/1
```
