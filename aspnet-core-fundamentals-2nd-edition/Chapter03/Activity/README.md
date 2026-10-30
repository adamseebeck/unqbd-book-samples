# Chapter 3: Activity solution

A worked solution to Chapter 3's activity. Try the activity yourself first. It uses fictional local data.

Run `dotnet run` from this folder. The launch profile in Properties/launchSettings.json is the one Chapter 1 builds: it starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once.

The `global.json` here selects the .NET 11 SDK (11.0.100 or later).

The `X-Exercise-Identity` header is a local test double, and echoing the idempotency key does not make the endpoint durably idempotent.

## Sending requests

`requests.http` holds every request this chapter checks, in order, each with the status you should see. Visual Studio 2026 runs it directly (open the file and select Send Request above a request); VS Code needs the REST Client extension.

In Windows PowerShell, type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. The commands below are the same requests. Run them from any folder; JSON bodies go to body.json first because Windows PowerShell removes double quotes from arguments it passes to curl.exe.

```powershell
# The main path without the exercise identity. Expect 401
curl.exe -i -w "\n" -X POST -H "Idempotency-Key: order-1001" https://localhost:7001/api/payments

# The main path with the local test identity. Expect 200 {"key":"order-1001"}
curl.exe -i -w "\n" -X POST -H "Idempotency-Key: order-1001" -H "X-Exercise-Identity: developer" https://localhost:7001/api/payments

# The mock webhook branch needs no exercise identity. Expect 200 {"key":"hook-2001"}
curl.exe -i -w "\n" -X POST -H "Idempotency-Key: hook-2001" https://localhost:7001/webhooks/demo

# A missing key fails before any route runs. Expect 400
curl.exe -i -w "\n" -X POST -H "X-Exercise-Identity: developer" https://localhost:7001/api/payments

# An invalid key (a space and "!" are not allowed). Expect 400
curl.exe -i -w "\n" -X POST -H "Idempotency-Key: bad key!" -H "X-Exercise-Identity: developer" https://localhost:7001/api/payments

# The branch accepts only POST /webhooks/demo. Expect 404
curl.exe -i -w "\n" https://localhost:7001/webhooks/demo

```
