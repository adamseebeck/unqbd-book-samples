# AdventureWorks: Chapter 17 Activity solution

This is a worked solution to the chapter's Activity, built in a copy of the Chapter 17 checkpoint. Try the Activity yourself first. The next chapter starts from `../Chapter17`, not from this folder.

It adds `BufferBenchmarks`, which compares a new 1,000-character array per call with one rented from ArrayPool and returned. Run it with `--filter "*BufferBenchmarks*"`.

## Run it

From this folder, the one that holds global.json:

```powershell
dotnet user-secrets set FeatureFlags:MaxCartItems 30 `
    --project AdventureWorks
dotnet user-secrets set Training:Password "Practice-Pass-2026!" `
    --project AdventureWorks
dotnet run --project InventoryService
```

Run the benchmarks from this folder, always in Release: `dotnet run -c Release --project AdventureWorks.Benchmarks -- --filter "*PriceFeed*" --job short`. They take a minute or two and need neither the app nor InventoryService; BenchmarkDotNet writes its reports to BenchmarkDotNet.Artifacts in this folder. Run the tests from this folder with `dotnet test --project AdventureWorks.Tests` (InventoryService and the app need not be running: the tests start the app in memory), or only the unit tests with `dotnet test --project AdventureWorks.Tests --filter-trait "Category=Unit"`. In Visual Studio, use Test Explorer. Then, in a second terminal in this folder, run `dotnet run --project AdventureWorks`. InventoryService is the warehouse stand-in on http://127.0.0.1:42132; the app calls it for stock. In Visual Studio 2026, open AdventureWorks.slnx, choose Configure Startup Projects, select Multiple startup projects, set AdventureWorks and InventoryService to Start, and press F5. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. The two secrets are Chapter 2's harmless setting and Chapter 8's test-account password; set them once, because every later checkpoint keeps the same UserSecretsId. Any password with 12 or more characters, upper and lower case letters, a digit and a symbol works; requests.http uses this one. The first run creates adventureworks.db in the AdventureWorks folder and five test accounts: casey, jordan, morgan, riley and avery, each at @adventure-works.test.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
Remove-Item cookies.txt -ErrorAction SilentlyContinue

# The import is the Admin's. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/avery

# Expect 200
curl.exe -s -D - -o token.json -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/csrf
$token = Get-Content token.json | ConvertFrom-Json

# Three records, checked and counted, not saved. Expect 200 with
# {"records":3}
@'
HLM-1001,Trail Helmet,31.99
JRS-2001,Cycling Jersey,54.50
TNT-4001,Two-Person Tent,179.00
'@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: text/plain" `
    -H "X-CSRF-TOKEN: $($token.token)" `
    --data-binary "@body.json" `
    https://localhost:7001/api/admin/products/import

# A quoted field is refused, not parsed. Expect 400
'HLM-1001,"Trail Helmet",31.99' | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: text/plain" `
    -H "X-CSRF-TOKEN: $($token.token)" `
    --data-binary "@body.json" `
    https://localhost:7001/api/admin/products/import

# A price must be a number. Expect 400
'HLM-1001,Trail Helmet,free' | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: text/plain" `
    -H "X-CSRF-TOKEN: $($token.token)" `
    --data-binary "@body.json" `
    https://localhost:7001/api/admin/products/import

# casey is no Admin. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/casey

# Expect 200
curl.exe -s -D - -o caseyToken.json -b cookies.txt `
    -c cookies.txt https://localhost:7001/api/csrf
$caseyToken = Get-Content caseyToken.json | ConvertFrom-Json

# Expect 403
'HLM-1001,Trail Helmet,31.99' | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: text/plain" `
    -H "X-CSRF-TOKEN: $($caseyToken.token)" `
    --data-binary "@body.json" `
    https://localhost:7001/api/admin/products/import

# Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-out
```
