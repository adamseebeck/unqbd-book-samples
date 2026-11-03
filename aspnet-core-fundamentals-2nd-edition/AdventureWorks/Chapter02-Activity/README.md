# AdventureWorks: Chapter 2 Activity solution

This is a worked solution to the chapter's Activity, built in a copy of the Chapter 2 checkpoint. Try the Activity yourself first. The next chapter starts from `../Chapter02`, not from this folder.

It adds validated `PaymentOptions` (Core), their appsettings.json section, and a Development-only `/api/payment/config` route. The endpoint URL is reserved example data; nothing sends requests to it.

## Run it

From this folder, the one that holds global.json:

```powershell
dotnet user-secrets set FeatureFlags:MaxCartItems 30 `
    --project AdventureWorks
dotnet user-secrets set PaymentOptions:TimeoutSeconds 45 `
    --project AdventureWorks
dotnet run --project AdventureWorks
```

In Visual Studio 2026, open AdventureWorks.slnx and press F5 instead. The launch profile in AdventureWorks/Properties/launchSettings.json starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Stop the app with Ctrl+C.

Then check precedence and validation, from this folder too:

```powershell
dotnet run --project AdventureWorks -- `
    --PaymentOptions:TimeoutSeconds=50
dotnet user-secrets set PaymentOptions:TimeoutSeconds 100 `
    --project AdventureWorks
dotnet run --project AdventureWorks
dotnet user-secrets set PaymentOptions:TimeoutSeconds 30 `
    --project AdventureWorks
dotnet run --project AdventureWorks -- --environment Production
```

The first run reports 50. With 100 stored, startup stops with an OptionsValidationException naming TimeoutSeconds. With 30 restored it starts again, and in Production `/api/payment/config` returns 404. The checkpoints share one UserSecretsId, so remove the Activity's value when you finish: `dotnet user-secrets remove PaymentOptions:TimeoutSeconds --project AdventureWorks`.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
# Chapter 1's routes still answer. Expect 302
curl.exe -i -w "\n" https://localhost:7001/

# Expect 200 with "status": "Operational"
curl.exe -i -w "\n" https://localhost:7001/api/status

# Expect 200
curl.exe -i -w "\n" https://localhost:7001/health

# Order 1 from the repository. Expect 200 with "orderNumber":
# "AW-0001"
curl.exe -i -w "\n" https://localhost:7001/api/orders/1

# The repository has no order 2. Expect 404
curl.exe -i -w "\n" https://localhost:7001/api/orders/2

# Not an integer, so the {id:int} route does not match. Expect 404
curl.exe -i -w "\n" https://localhost:7001/api/orders/abc

# The bound options (Development only). The User Secret overrides
# appsettings.json. Expect 200 with "maxCartItems": 30
curl.exe -i -w "\n" https://localhost:7001/api/config

# A plain address is valid. Expect 200 with "isValid": true
curl.exe -i -w "\n" `
    https://localhost:7001/api/email-check?address=ada@example.com

# A display-name form, A<a@b.co>, is rejected. Expect 200 with
# "isValid": false
curl.exe -i -w "\n" `
    https://localhost:7001/api/email-check?address=A%3Ca@b.co%3E

# The address query value is required. Expect 400
curl.exe -i -w "\n" https://localhost:7001/api/email-check

# The payment settings (Development only). The User Secret 45
# overrides the 30 in appsettings.json. Expect 200 with
# "timeoutSeconds": 45
curl.exe -i -w "\n" https://localhost:7001/api/payment/config
```
