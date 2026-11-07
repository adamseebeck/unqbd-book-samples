# AdventureWorks: Chapter 13 checkpoint

The AdventureWorks store app at the end of Chapter 13. The order API is a REST collection of the signed-in customer's orders on Chapter 11's service: `GET /api/orders` (one bounded page, Link header to the neighbors), `GET /api/orders/{id}` (an OrderDto without the owner's ID, its version as the ETag), `POST /api/orders` (201 with Location and ETag), `PATCH /api/orders/{id}` (the notes, with If-Match: 428 without it, 412 when stale) and `DELETE /api/orders/{id}` (cancels). The Admin's permanent delete is `DELETE /api/admin/orders/{id}`. The order API's JSON types are serialized by generated code (StoreJsonContext). Chapter 14 starts from this folder.

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
dotnet user-secrets set Training:Password "Practice-Pass-2026!" `
    --project AdventureWorks
dotnet run --project InventoryService
```

Then, in a second terminal in this folder, run `dotnet run --project AdventureWorks`. InventoryService is the warehouse stand-in on http://127.0.0.1:42132; the app calls it for stock. In Visual Studio 2026, open AdventureWorks.slnx, choose Configure Startup Projects, select Multiple startup projects, set AdventureWorks and InventoryService to Start, and press F5. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. The two secrets are Chapter 2's harmless setting and Chapter 8's test-account password; set them once, because every later checkpoint keeps the same UserSecretsId. Any password with 12 or more characters, upper and lower case letters, a digit and a symbol works; requests.http uses this one. The first run creates adventureworks.db in the AdventureWorks folder and five test accounts: casey, jordan, morgan, riley and avery, each at @adventure-works.test. From Chapter 10 the app keeps its catalog, orders and stock in adventureworks.db (with SQLite's -wal and -shm files beside it while it runs). In Development the first run applies the migrations and seeds the data; each later run keeps every change. To start over, stop the app and run `dotnet ef database drop --force -s ..\AdventureWorks` from the AdventureWorks.Infrastructure folder. The dotnet-ef tool is pinned in dotnet-tools.json; run `dotnet tool restore` once in this folder before using it.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
Remove-Item cookies.txt -ErrorAction SilentlyContinue

# The orders collection belongs to a signed-in customer.
# Expect 401
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/orders

# casey. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/casey

# Expect 200
curl.exe -s -D - -o token.json -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/csrf
$token = Get-Content token.json | ConvertFrom-Json

# Page 1 of casey's eleven orders, five per page. The Link header
# points to the next page. Expect 200 with "totalPages": 3, and it
# has a Link header
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    "https://localhost:7001/api/orders?page=1&pageSize=5"

# The last page links back only. Expect 200 with "page": 3, and it
# has a Link header
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    "https://localhost:7001/api/orders?page=3&pageSize=5"

# Pages hold 1 to 50 orders. Expect 400
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/orders?pageSize=500

# One order: an OrderDto with no owner ID, and its version as the
# ETag. Expect 200 with "orderNumber": "AW-0001", and it has an
# ETag header, and the body lacks "ownerId"
curl.exe -s -D order.txt -o order.json -b cookies.txt `
    -c cookies.txt https://localhost:7001/api/orders/1
Get-Content order.txt
$orderETag = (Get-Content order.txt) -match '^ETag:'
$orderETag = $orderETag -replace '^ETag: '
$order = Get-Content order.json | ConvertFrom-Json

# A change without If-Match. Expect 428
@'
{ "notes": "Leave it at the side door." }
'@ | Set-Content body.json
curl.exe -i -w "\n" -X PATCH -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($token.token)" `
    --data-binary "@body.json" `
    https://localhost:7001/api/orders/1

# With the ETag just read. Expect 200 with "notes": "Leave it at
# the side door."
@'
{ "notes": "Leave it at the side door." }
'@ | Set-Content body.json
curl.exe -s -D patched.txt -o patched.json -X PATCH `
    -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "If-Match: $orderETag" -H "X-CSRF-TOKEN: $($token.token)" `
    --data-binary "@body.json" `
    https://localhost:7001/api/orders/1
Get-Content patched.txt
$patchedETag = (Get-Content patched.txt) -match '^ETag:'
$patchedETag = $patchedETag -replace '^ETag: '
$patched = Get-Content patched.json | ConvertFrom-Json

# The same ETag again is stale now. Expect 412
'{ "notes": "Ring the bell." }' | Set-Content body.json
curl.exe -i -w "\n" -X PATCH -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "If-Match: $orderETag" -H "X-CSRF-TOKEN: $($token.token)" `
    --data-binary "@body.json" `
    https://localhost:7001/api/orders/1

# With the new ETag, clear the notes again. Expect 200 with
# "notes": null
'{ "notes": null }' | Set-Content body.json
curl.exe -i -w "\n" -X PATCH -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "If-Match: $patchedETag" `
    -H "X-CSRF-TOKEN: $($token.token)" `
    --data-binary "@body.json" `
    https://localhost:7001/api/orders/1

# Create: 201 with a Location and an ETag. Expect 201, and it has
# a Location header
@'
{ "items": [ { "productId": 2, "quantity": 1 } ] }
'@ | Set-Content body.json
curl.exe -s -D - -o created.json -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($token.token)" `
    --data-binary "@body.json" https://localhost:7001/api/orders
$created = Get-Content created.json | ConvertFrom-Json

# DELETE cancels. Expect 204
curl.exe -i -w "\n" -X DELETE -b cookies.txt -c cookies.txt `
    -H "X-CSRF-TOKEN: $($token.token)" `
    "https://localhost:7001/api/orders/$($created.id)"

# A second DELETE meets a cancelled order. Expect 409
curl.exe -i -w "\n" -X DELETE -b cookies.txt -c cookies.txt `
    -H "X-CSRF-TOKEN: $($token.token)" `
    "https://localhost:7001/api/orders/$($created.id)"

# The order is still there. Expect 200 with "status": "Cancelled"
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    "https://localhost:7001/api/orders/$($created.id)"

# No such route: status-code pages fill the empty 404 with Problem
# Details for a JSON client. Expect 404, and it carries
# Content-Type: application/problem+json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Accept: application/json" `
    https://localhost:7001/api/nope

# A browser gets plain text instead. Expect 404, and it carries
# Content-Type: text/plain
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Accept: text/html" https://localhost:7001/api/nope

# The Admin removes the cancelled order for good. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/avery

# Expect 200
curl.exe -s -D - -o averyToken.json -b cookies.txt `
    -c cookies.txt https://localhost:7001/api/csrf
$averyToken = Get-Content averyToken.json | ConvertFrom-Json

# Expect 204
curl.exe -i -w "\n" -X DELETE -b cookies.txt -c cookies.txt `
    -H "X-CSRF-TOKEN: $($averyToken.token)" `
    "https://localhost:7001/api/admin/orders/$($created.id)"

# Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-out
```
