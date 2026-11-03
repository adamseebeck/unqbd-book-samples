# AdventureWorks: Chapter 10 checkpoint

The AdventureWorks store app at the end of Chapter 10. The catalog, orders and stock move from memory into the SQLite database that Chapter 8 created for Identity, behind the same Core interfaces, so the catalog, the product APIs and the pages keep their code. Migrations (in `AdventureWorks.Infrastructure/Migrations`) create the schema. The tables store prices as whole cents; Core and the API keep decimal. New routes: `GET /api/orders/report` (the signed-in owner's orders, ten per page), `PUT /api/orders/{id}` (notes, with the order's Version as a concurrency check), and the Admin's stock API under `/api/admin/inventory`, whose transfer runs two saves in one transaction. Chapter 11 starts from this folder.

| Who | Account | Can |
| --- | --- | --- |
| Anyone | (none) | Browse the catalog, product reads, `/Products`, sign in |
| casey | customer, owns AW-0001 and AW-0003 to AW-0012 | Read and annotate her orders; her report has two pages |
| jordan | buyer in Finance, owns AW-0002 | Read and archive his order |
| morgan | Finance staff | Read any order; archive none |
| riley | born 2010 | Not `/Products/Restricted` |
| avery | Admin | Stock and transfers, delete orders, write products, open `/Admin` |

| Folder | Project |
| --- | --- |
| `AdventureWorks` | Web: hosting, endpoints and composition |
| `AdventureWorks.Core` | Domain types and contracts |
| `AdventureWorks.Infrastructure` | Implementations of the Core contracts |

`AdventureWorks.slnx` lists all three. Core references nothing; Infrastructure references Core; Web references both.

## Run it

From this folder, the one that holds global.json:

```shell
cd AdventureWorks
dotnet user-secrets set FeatureFlags:MaxCartItems 30
dotnet user-secrets set Training:Password "Practice-Pass-2026!"
cd ..
dotnet run --project AdventureWorks
```

The two secrets are Chapter 2's harmless setting and Chapter 8's test-account password; set them once, because every later checkpoint keeps the same UserSecretsId. Any password with 12 or more characters, upper and lower case letters, a digit and a symbol works; requests.http uses this one. The first run creates adventureworks.db in the AdventureWorks folder, the four products, twelve orders and two stock locations, and five test accounts: casey, jordan, morgan, riley and avery, each at @adventure-works.test. In Visual Studio 2026, open AdventureWorks.slnx and press F5 instead. The launch profile in AdventureWorks/Properties/launchSettings.json starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Stop the app with Ctrl+C. From Chapter 10 the app keeps its catalog, orders and stock in adventureworks.db (with SQLite's -wal and -shm files beside it while it runs). In Development the first run applies the migrations and seeds the data; each later run keeps every change. To start over, stop the app and run `dotnet ef database drop --force -s ..\AdventureWorks` from the AdventureWorks.Infrastructure folder. The dotnet-ef tool is pinned in dotnet-tools.json; run `dotnet tool restore` once in this folder before using it.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
Remove-Item cookies.txt -ErrorAction SilentlyContinue

# The catalog now comes from the database. Expect 200 with "sku":
# "HLM-1001"
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog

# The search runs as SQL. Expect 200 with "name": "Trail Helmet"
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/search?q=helmet

# Decimal prices again, from whole cents in the table. Expect 200
# with "price": 1299.00
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/v2/products/3

# casey: a customer with eleven orders. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/casey

# The order with its items and its current version. Expect 200
# with "orderNumber": "AW-0001"
curl.exe -s -D - -o order.json -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/orders/1
$order = Get-Content order.json | ConvertFrom-Json

# casey's orders, newest first, ten per page; SQL sums each total.
# Expect 200 with "orderNumber": "AW-0012"
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/orders/report

# The eleventh order is on page 2. Expect 200 with "orderNumber":
# "AW-0001"
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/orders/report?page=2

# Pages start at 1. Expect 400
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/orders/report?page=0

# Expect 200
curl.exe -s -D - -o caseyToken.json -b cookies.txt `
    -c cookies.txt https://localhost:7001/api/csrf
$caseyToken = Get-Content caseyToken.json | ConvertFrom-Json

# Send back the version that was read. Expect 200 with "notes":
# "Leave it at the side door."
@"
{ "notes": "Leave it at the side door.",
  "version": "$($order.version)" }
"@ | Set-Content body.json
curl.exe -s -D - -o updated.json -X PUT -b cookies.txt `
    -c cookies.txt -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($caseyToken.token)" `
    --data-binary "@body.json" `
    https://localhost:7001/api/orders/1
$updated = Get-Content updated.json | ConvertFrom-Json

# The same version again is stale now: someone saved since.
# Expect 409
@"
{ "notes": "Ring the bell.",
  "version": "$($order.version)" }
"@ | Set-Content body.json
curl.exe -i -w "\n" -X PUT -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($caseyToken.token)" `
    --data-binary "@body.json" `
    https://localhost:7001/api/orders/1

# With the current version the update works, and it clears the
# notes again. Expect 200 with "notes": null
@"
{ "notes": null,
  "version": "$($updated.version)" }
"@ | Set-Content body.json
curl.exe -i -w "\n" -X PUT -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($caseyToken.token)" `
    --data-binary "@body.json" `
    https://localhost:7001/api/orders/1

# Order 2 is jordan's: the owner rule refuses. Expect 403
@'
{ "notes": "Mine now?",
  "version": "00000000-0000-0000-0000-000000000000" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -X PUT -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($caseyToken.token)" `
    --data-binary "@body.json" `
    https://localhost:7001/api/orders/2

# Stock is for the Admin. Expect 403
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/admin/inventory/1

# avery: the Admin. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/avery

# Expect 200
curl.exe -s -D - -o averyToken.json -b cookies.txt `
    -c cookies.txt https://localhost:7001/api/csrf
$averyToken = Get-Content averyToken.json | ConvertFrom-Json

# The helmet's two locations. Expect 200 with "quantity": 10
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/admin/inventory/1

# Move 3 helmets; both saves commit together. Expect 200 with
# {"location":"Overflow","quantity":3}
@'
{ "productId": 1, "from": "Main", "to": "Overflow",
  "quantity": 3 }
'@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($averyToken.token)" `
    --data-binary "@body.json" `
    https://localhost:7001/api/admin/inventory/transfer

# Overflow holds only 3. The first save adds 50 to Main, the
# second fails the check constraint, and the rollback undoes the
# first. Expect 409
@'
{ "productId": 1, "from": "Overflow", "to": "Main",
  "quantity": 50 }
'@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($averyToken.token)" `
    --data-binary "@body.json" `
    https://localhost:7001/api/admin/inventory/transfer

# Unchanged. Expect 200 with {"location":"Main","quantity":7}
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/admin/inventory/1

# Move them back. Expect 200 with
# {"location":"Main","quantity":10}
@'
{ "productId": 1, "from": "Overflow", "to": "Main",
  "quantity": 3 }
'@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($averyToken.token)" `
    --data-binary "@body.json" `
    https://localhost:7001/api/admin/inventory/transfer

# The unique index refuses a second HLM-1001. Expect 400 with
# "title": "Invalid request"
@'
{ "name": "Trail Helmet", "sku": "HLM-1001",
  "price": 29.99, "category": "accessories" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v2/products

# A new product. Expect 201
@'
{ "name": "Trail Bottle", "sku": "BTL-5001",
  "price": 24.99, "category": "accessories" }
'@ | Set-Content body.json
curl.exe -s -D - -o bottle.json -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v2/products
$bottle = Get-Content bottle.json | ConvertFrom-Json

# Expect 204
curl.exe -i -w "\n" -X DELETE -b cookies.txt -c cookies.txt `
    "https://localhost:7001/api/v2/products/$($bottle.id)"

# The helmet is on orders, so the foreign key keeps it. Expect 400
curl.exe -i -w "\n" -X DELETE -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/v2/products/1

# Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-out
```
