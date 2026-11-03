# AdventureWorks: Chapter 10 Activity solution

This is a worked solution to the chapter's Activity, built in a copy of the Chapter 10 checkpoint. Try the Activity yourself first. The next chapter starts from `../Chapter10`, not from this folder.

It runs the same store on SQL Server LocalDB, which Visual Studio 2026 installs, and keeps SQLite as the default. `AdventureWorks.SqlServerMigrations` holds the SQL Server migrations, because migrations are provider-specific; `Database:Provider` set to SqlServer (the `sqlserver` launch profile sets it) selects that provider and that migrations assembly. The database is `AdventureWorksStore` on `(localdb)\MSSQLLocalDB`, unless ConnectionStrings:SqlServer names another. `StoreErrors` maps the constraint errors of both providers, and the seed turns on IDENTITY_INSERT on SQL Server for its fixed IDs.

## Run it

From this folder, the one that holds global.json:

```powershell
dotnet user-secrets set FeatureFlags:MaxCartItems 30 `
    --project AdventureWorks
dotnet user-secrets set Training:Password "Practice-Pass-2026!" `
    --project AdventureWorks
dotnet run --project AdventureWorks --launch-profile sqlserver
```

The first run in Development creates AdventureWorksStore, applies the SQL Server migration and seeds it. To use the dotnet ef tools against SQL Server, run `dotnet tool restore` once, set `$env:Database__Provider = "SqlServer"` in the PowerShell session, and run them from the AdventureWorks.SqlServerMigrations folder with `-s ..\AdventureWorks`. In Visual Studio 2026, open AdventureWorks.slnx and press F5 instead. The launch profile in AdventureWorks/Properties/launchSettings.json starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Stop the app with Ctrl+C.

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
