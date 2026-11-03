# AdventureWorks: Chapter 11 checkpoint

The AdventureWorks store app at the end of Chapter 11. Orders are placed and cancelled through an application service, `OrderService` in Core, which sets prices from the catalog, takes the owner from the sign-in and numbers each order. It stages its changes through `IOrderWriter` and saves them once through `IUnitOfWork`; a logging decorator wraps it, and scope validation now runs in every environment. New routes: `POST /api/orders` (201 with a Location) and `POST /api/orders/{id}/cancel`. Chapter 12 starts from this folder.

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
dotnet run --project AdventureWorks
```

The two secrets are Chapter 2's harmless setting and Chapter 8's test-account password; set them once, because every later checkpoint keeps the same UserSecretsId. Any password with 12 or more characters, upper and lower case letters, a digit and a symbol works; requests.http uses this one. The first run creates adventureworks.db in the AdventureWorks folder, the four products, twelve orders and two stock locations, and five test accounts: casey, jordan, morgan, riley and avery, each at @adventure-works.test. In Visual Studio 2026, open AdventureWorks.slnx and press F5 instead. The launch profile in AdventureWorks/Properties/launchSettings.json starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Stop the app with Ctrl+C. From Chapter 10 the app keeps its catalog, orders and stock in adventureworks.db (with SQLite's -wal and -shm files beside it while it runs). In Development the first run applies the migrations and seeds the data; each later run keeps every change. To start over, stop the app and run `dotnet ef database drop --force -s ..\AdventureWorks` from the AdventureWorks.Infrastructure folder. The dotnet-ef tool is pinned in dotnet-tools.json; run `dotnet tool restore` once in this folder before using it.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
Remove-Item cookies.txt -ErrorAction SilentlyContinue

# Placing an order needs a signed-in user. Expect 401
@'
{ "items": [ { "productId": 1, "quantity": 2 } ] }
'@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" https://localhost:7001/api/orders

# casey. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/casey

# Expect 200
curl.exe -s -D - -o token.json -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/csrf
$token = Get-Content token.json | ConvertFrom-Json

# Two helmets and a tent. The unitPrice in the request is ignored:
# the catalog sets each price, and the total is 2 x 29.99 +
# 189.00. Expect 201 with "total": 248.98
@'
{ "items": [
    { "productId": 1, "quantity": 2, "unitPrice": 0.01 },
    { "productId": 4, "quantity": 1 } ] }
'@ | Set-Content body.json
curl.exe -s -D - -o created.json -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($token.token)" `
    --data-binary "@body.json" https://localhost:7001/api/orders
$created = Get-Content created.json | ConvertFrom-Json

# The new order, at its Location. Expect 200 with "status":
# "Pending"
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    "https://localhost:7001/api/orders/$($created.id)"

# No lines. Expect 400
'{ "items": [] }' | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($token.token)" `
    --data-binary "@body.json" https://localhost:7001/api/orders

# A quantity of zero. Expect 400
@'
{ "items": [ { "productId": 1, "quantity": 0 } ] }
'@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($token.token)" `
    --data-binary "@body.json" https://localhost:7001/api/orders

# The same product twice. Expect 400 with "detail": "Use
@'
{ "items": [ { "productId": 1, "quantity": 1 },
    { "productId": 1, "quantity": 2 } ] }
'@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($token.token)" `
    --data-binary "@body.json" https://localhost:7001/api/orders

# A product the store does not sell. Expect 400
@'
{ "items": [ { "productId": 99, "quantity": 1 } ] }
'@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($token.token)" `
    --data-binary "@body.json" https://localhost:7001/api/orders

# A write without the antiforgery header. Expect 400
@'
{ "items": [ { "productId": 1, "quantity": 1 } ] }
'@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" https://localhost:7001/api/orders

# jordan may not cancel casey's order. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/jordan

# Expect 200
curl.exe -s -D - -o jordanToken.json -b cookies.txt `
    -c cookies.txt https://localhost:7001/api/csrf
$jordanToken = Get-Content jordanToken.json | ConvertFrom-Json

# Expect 403
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    -H "X-CSRF-TOKEN: $($jordanToken.token)" `
    "https://localhost:7001/api/orders/$($created.id)/cancel"

# casey again. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/casey

# Expect 200
curl.exe -s -D - -o caseyToken.json -b cookies.txt `
    -c cookies.txt https://localhost:7001/api/csrf
$caseyToken = Get-Content caseyToken.json | ConvertFrom-Json

# Expect 200 with "status": "Cancelled"
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    -H "X-CSRF-TOKEN: $($caseyToken.token)" `
    "https://localhost:7001/api/orders/$($created.id)/cancel"

# Expect 400 with "detail": "The order is already cancelled."
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    -H "X-CSRF-TOKEN: $($caseyToken.token)" `
    "https://localhost:7001/api/orders/$($created.id)/cancel"

# Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-out
```
