# Chapter 16: Observability and Diagnostics in Production Systems

The complete code for Chapter 16's Full Example, matching the listings in the book; Activity/ holds the activity solution. Like every chapter from Chapter 7, this is its own small project: start from this folder rather than adding it to an earlier chapter's project.

## Run it

Run `dotnet run --urls http://127.0.0.1:42148` from this folder. The app's own /catalog route stands in for the downstream catalog, so the order route calls the same host. The console shows JSON logs, console traces and metrics.

## Sending requests

`requests.http` holds every request this chapter checks, in order, each with the status you should see. Visual Studio 2026 runs it directly (open the file and select Send Request above a request); VS Code needs the REST Client extension.

In Windows PowerShell, type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. The commands below are the same requests. Run them from any folder; JSON bodies go to body.json first because Windows PowerShell removes double quotes from arguments it passes to curl.exe.

```powershell
# Liveness only. Expect 200 "Healthy"
curl.exe -i -w "\n" http://127.0.0.1:42148/health/live

# Readiness includes the catalog check. Expect 200 "Healthy"
curl.exe -i -w "\n" http://127.0.0.1:42148/health/ready

# Create an order. Expect 201 and the same X-Correlation-ID response header
'{ "region": "east" }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" -H "X-Correlation-ID: order-lab-1" --data-binary "@body.json" http://127.0.0.1:42148/api/orders

# An unsafe correlation ID is replaced with the trace ID. Expect 201 and a different X-Correlation-ID
'{ "region": "west" }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" -H "X-Correlation-ID: bad id with spaces" --data-binary "@body.json" http://127.0.0.1:42148/api/orders

# Regions other than east and west are rejected. Expect 400
'{ "region": "north" }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" --data-binary "@body.json" http://127.0.0.1:42148/api/orders

# The stand-in catalog can fail on request. Expect 503
curl.exe -i -w "\n" http://127.0.0.1:42148/catalog?status=503

```
