# AdventureWorks: Chapter 3 checkpoint

The AdventureWorks store app at the end of Chapter 3. Chapter 2's projects, options and routes are unchanged; the pipeline now has bounded correlation IDs (`CorrelationIdMiddleware`), Problem Details for the two domain exceptions (`DomainExceptionMiddleware`, with the exceptions in Core), status-code pages, a terminal `/ping` branch and a `UseWhen` branch that marks `/api` responses. Chapter 4 starts from this folder.

The pipeline runs in this order: exception handler (outside Development) and HSTS, HTTPS redirection, the `/ping` branch, status-code pages, correlation, request timing, domain exceptions, routing, the `/api` branch, then the endpoints.

Order 500 makes the in-memory repository throw a simulated storage failure, so you can see an unexpected error: Development shows the developer exception page, and Production returns a generic 500 without the exception text.

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
dotnet run --project AdventureWorks
```

The first command stores Chapter 2's harmless User Secret outside the project (Step 5); run it once, because every later checkpoint keeps the same UserSecretsId. In Visual Studio 2026, open AdventureWorks.slnx and press F5 instead. The launch profile in AdventureWorks/Properties/launchSettings.json starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Stop the app with Ctrl+C.

To compare the unexpected-error response, run in Production from this folder with `dotnet run --project AdventureWorks -- --environment Production` and request `/api/orders/500` again.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
# The terminal MapWhen branch answers before the correlation
# middleware, so the response has no X-Correlation-ID header.
# Expect 200
curl.exe -i -w "\n" https://localhost:7001/ping

# A valid correlation ID is echoed, and the UseWhen branch marks
# /api responses. The response carries X-Correlation-ID:
# checkout-42 and carries X-API-Pipeline: true. Expect 200
curl.exe -i -w "\n" -H "X-Correlation-ID: checkout-42" `
    https://localhost:7001/api/status

# Order 1. Expect 200 with "orderNumber": "AW-0001"
curl.exe -i -w "\n" https://localhost:7001/api/orders/1

# DomainValidationException becomes Problem Details. Expect 400
# with "title": "Invalid request"
curl.exe -i -w "\n" https://localhost:7001/api/orders/0

# ResourceNotFoundException becomes Problem Details with the
# correlation ID. Expect 404 with "title": "Resource not found"
curl.exe -i -w "\n" https://localhost:7001/api/orders/7

# The repository's simulated storage failure. Development shows
# the developer exception page. Expect 500
curl.exe -i -w "\n" https://localhost:7001/api/orders/500

# Status-code pages fill an empty 404. Expect 404
curl.exe -i -w "\n" https://localhost:7001/missing

# Chapter 1 and 2 routes still answer. Expect 302
curl.exe -i -w "\n" https://localhost:7001/

# Expect 200
curl.exe -i -w "\n" https://localhost:7001/health

# Expect 200 with "maxCartItems": 30
curl.exe -i -w "\n" https://localhost:7001/api/config

# Expect 200 with "isValid": true
curl.exe -i -w "\n" `
    https://localhost:7001/api/email-check?address=ada@example.com
```
