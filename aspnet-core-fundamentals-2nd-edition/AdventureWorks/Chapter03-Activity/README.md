# AdventureWorks: Chapter 3 Activity solution

For the Visual Studio steps, use Visual Studio with support for the .NET 11 SDK and `.slnx` solutions.

This is a worked solution to the chapter's Activity, built in a copy of the Chapter 3 checkpoint. Try the Activity yourself first. The next chapter starts from `../Chapter03`, not from this folder.

It adds `SecurityHeadersMiddleware` and `IdempotencyKeyMiddleware` (ActivityMiddleware.cs), a terminal `/webhooks` branch and a local `POST /api/payments` route behind an exercise guard. The `X-Exercise-Identity` header is a test double for later authentication, and echoing an idempotency key does not make a route durably idempotent. Never protect a deployed payment route this way.

## Run it

From this folder, the one that holds global.json:

```powershell
dotnet user-secrets set FeatureFlags:MaxCartItems 30 `
    --project AdventureWorks
dotnet run --project AdventureWorks
```

In Visual Studio, open AdventureWorks.slnx and press F5 instead. The launch profile in AdventureWorks/Properties/launchSettings.json starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Stop the app with Ctrl+C.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

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

# The Activity's main path without the exercise identity. The
# response carries X-Content-Type-Options: nosniff. Expect 401
curl.exe -i -w "\n" -X POST -H "Idempotency-Key: order-1001" `
    https://localhost:7001/api/payments

# The main path with the local test identity. Expect 200 with
# {"key":"order-1001"}
curl.exe -i -w "\n" -X POST -H "Idempotency-Key: order-1001" `
    -H "X-Exercise-Identity: developer" `
    https://localhost:7001/api/payments

# The webhook branch needs no exercise identity, and it also
# carries X-Frame-Options: DENY. Expect 200 with
# {"key":"hook-2001"}
curl.exe -i -w "\n" -X POST -H "Idempotency-Key: hook-2001" `
    https://localhost:7001/webhooks/demo

# A missing key fails before any route runs. Expect 400
curl.exe -i -w "\n" -X POST -H "X-Exercise-Identity: developer" `
    https://localhost:7001/api/payments

# A space and "!" are not allowed in a key. Expect 400
curl.exe -i -w "\n" -X POST -H "Idempotency-Key: bad key!" `
    -H "X-Exercise-Identity: developer" `
    https://localhost:7001/api/payments

# The branch accepts only POST /webhooks/demo. Expect 404
curl.exe -i -w "\n" https://localhost:7001/webhooks/demo
```
