# AdventureWorks: Chapter 2 checkpoint

The AdventureWorks store app at the end of Chapter 2: the Chapter 1 host split into three projects, with typed and validated FeatureFlags options, three service lifetimes, an order route and two Development-only diagnostics. Chapter 3 starts from this folder.

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

To see the 503 branch, start with checkout switched off; to see startup validation stop the host, start with an invalid value. Run these from this folder too:

```powershell
dotnet run --project AdventureWorks -- `
    --FeatureFlags:EnableNewCheckout=false
dotnet run --project AdventureWorks -- `
    --FeatureFlags:MaxCartItems=0
```

The second command ends with an OptionsValidationException that names MaxCartItems.

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
```
