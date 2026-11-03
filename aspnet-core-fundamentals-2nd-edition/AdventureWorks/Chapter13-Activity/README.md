# AdventureWorks: Chapter 13 Activity solution

This is a worked solution to the chapter's Activity, built in a copy of the Chapter 13 checkpoint. Try the Activity yourself first. The next chapter starts from `../Chapter13`, not from this folder.

It adds `GET /api/orders/{id}/items`, an order's lines as a paged sub-collection with the same bounds and Link header as the orders collection, behind Chapter 9's read rule.

## Run it

From this folder, the one that holds global.json:

```shell
cd AdventureWorks
dotnet user-secrets set FeatureFlags:MaxCartItems 30
dotnet user-secrets set Training:Password "Practice-Pass-2026!"
cd ..
dotnet run --project InventoryService
```

Then, in a second terminal in this folder, run `dotnet run --project AdventureWorks`. InventoryService is the warehouse stand-in on http://127.0.0.1:42132; the app calls it for stock. In Visual Studio 2026, open AdventureWorks.slnx, choose Configure Startup Projects, select Multiple startup projects, set AdventureWorks and InventoryService to Start, and press F5. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. The two secrets are Chapter 2's harmless setting and Chapter 8's test-account password; set them once, because every later checkpoint keeps the same UserSecretsId. Any password with 12 or more characters, upper and lower case letters, a digit and a symbol works; requests.http uses this one. The first run creates adventureworks.db in the AdventureWorks folder and five test accounts: casey, jordan, morgan, riley and avery, each at @adventure-works.test.

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

# Activity: an order with three lines. Expect 201
@'
{ "items": [ { "productId": 1, "quantity": 1 },
    { "productId": 2, "quantity": 1 },
    { "productId": 4, "quantity": 1 } ] }
'@ | Set-Content body.json
curl.exe -s -D - -o three.json -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    -H "X-CSRF-TOKEN: $($token.token)" `
    --data-binary "@body.json" https://localhost:7001/api/orders
$three = Get-Content three.json | ConvertFrom-Json

# Two lines per page; the Link header points to page 2. Expect 200
# with "totalCount": 3, and it has a Link header
$lines = "https://localhost:7001/api/orders/$($three.id)/items"
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    "$($lines)?pageSize=2"

# The second page holds the third line. Expect 200 with
# "productId": 4, and it has no Link header
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    "$($lines)?page=2&pageSize=2"

# Expect 400
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    "$($lines)?pageSize=0"

# No such order. Expect 404
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/orders/9999/items

# Expect 204
curl.exe -i -w "\n" -X DELETE -b cookies.txt -c cookies.txt `
    -H "X-CSRF-TOKEN: $($token.token)" `
    "https://localhost:7001/api/orders/$($three.id)"

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
curl.exe -i -w "\n" -X DELETE -b cookies.txt -c cookies.txt `
    -H "X-CSRF-TOKEN: $($averyToken.token)" `
    "https://localhost:7001/api/admin/orders/$($three.id)"

# Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-out
```
