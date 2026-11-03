# AdventureWorks: Chapter 5 checkpoint

The AdventureWorks store app at the end of Chapter 5. It adds the product management API in two styles over the one `IProductService` from Chapter 4: `/api/v1/products` as an MVC controller and `/api/v2/products` as Minimal APIs. They are two versions of the same API, one per style, and Chapter 14 documents them as versions. The request types are in Core; the manual `ValidationFilter<T>` and two logging filters are in Web. A route, query and header binding demonstration joins the Development-only examples at `/examples/binding/{id:int}`. Chapter 6 starts from this folder.

A product created, changed or deleted through either version shows up in the other and in Chapter 4's `/api/catalog`, because all three use the same in-memory singleton. The write routes have no authentication yet; Chapter 9 requires the Admin role for them.

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
dotnet run --project AdventureWorks
```

The first command stores Chapter 2's harmless User Secret outside the project (Step 5); run it once, because every later checkpoint keeps the same UserSecretsId. In Visual Studio 2026, open AdventureWorks.slnx and press F5 instead. The launch profile in AdventureWorks/Properties/launchSettings.json starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Stop the app with Ctrl+C. The two logging filters log each action and endpoint at the Information level.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
# The v1 controller lists the catalog. Expect 200 with "sku":
# "HLM-1001"
curl.exe -i -w "\n" https://localhost:7001/api/v1/products

# Create a product through v1, the controller version. Expect 201
# with "id": 5 and a Location header
@'
{ "name": "Trail Bottle", "sku": "BTL-5001",
  "price": 24.99, "category": "accessories" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v1/products

# The same singleton store serves v2. Expect 200 with "sku":
# "BTL-5001"
curl.exe -i -w "\n" https://localhost:7001/api/v2/products/5

# And Chapter 4's catalog, too. Expect 200 with "sku": "BTL-5001"
curl.exe -i -w "\n" https://localhost:7001/api/catalog/5

# Create a product through v2, the Minimal API version. Expect 201
# with "id": 6
@'
{ "name": "Camp Mug", "sku": "MUG-6001",
  "price": 12.50, "category": "camping" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v2/products

# Search by name through v1. Expect 200 with "sku": "MUG-6001"
curl.exe -i -w "\n" https://localhost:7001/api/v1/products?q=mug

# Update through v1. Expect 204
@'
{ "name": "Trail Bottle XL", "price": 27.99 }
'@ | Set-Content body.json
curl.exe -i -w "\n" -X PUT -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v1/products/5

# Update through v2. Expect 204
@'
{ "name": "Camp Mug Large", "price": 14.00 }
'@ | Set-Content body.json
curl.exe -i -w "\n" -X PUT -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v2/products/6

# Each update raises the version. Expect 200 with "version": 2
curl.exe -i -w "\n" https://localhost:7001/api/v2/products/5

# A name shorter than 3 characters fails in v1. Expect 400
@'
{ "name": "AB", "sku": "ABC-0003",
  "price": 5, "category": "camping" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v1/products

# A zero price fails in v2. Expect 400
@'
{ "name": "Free Sample", "sku": "FRE-0004",
  "price": 0, "category": "camping" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v2/products

# A lowercase SKU fails the pattern in v1. Expect 400
@'
{ "name": "Lantern", "sku": "lan-0005",
  "price": 30, "category": "camping" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v1/products

# The same lowercase SKU fails in v2. Expect 400
@'
{ "name": "Lantern", "sku": "lan-0005",
  "price": 30, "category": "camping" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v2/products

# The object-level rule rejects an unknown category in v1.
# Expect 400 with "Unknown category."
@'
{ "name": "Kayak", "sku": "KYK-0006",
  "price": 450, "category": "boats" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v1/products

# The same rule in v2. Expect 400
@'
{ "name": "Kayak", "sku": "KYK-0006",
  "price": 450, "category": "boats" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v2/products

# Nothing invalid was stored: ids 1 to 6 only. Expect 200
curl.exe -i -w "\n" https://localhost:7001/api/v2/products

# Paging outside the allowed range in v2. Expect 400
curl.exe -i -w "\n" `
    https://localhost:7001/api/v2/products?page=0

# The same bound in v1, from the Range attribute. Expect 400
curl.exe -i -w "\n" `
    https://localhost:7001/api/v1/products?pageSize=500

# Updating a product that does not exist. Expect 404
'{ "name": "Ghost", "price": 1 }' | Set-Content body.json
curl.exe -i -w "\n" -X PUT -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v1/products/99

# Delete through v1. Expect 204
curl.exe -i -w "\n" -X DELETE `
    https://localhost:7001/api/v1/products/5

# Gone from v2 as well. Expect 404
curl.exe -i -w "\n" https://localhost:7001/api/v2/products/5

# Delete through v2. Expect 204
curl.exe -i -w "\n" -X DELETE `
    https://localhost:7001/api/v2/products/6

# Deleting again finds nothing. Expect 404
curl.exe -i -w "\n" -X DELETE `
    https://localhost:7001/api/v2/products/6

# Route, query and header binding (Development only). Expect 200
# with {"id":7,"q":"tent","version":"2"}
curl.exe -i -w "\n" -H "X-Api-Version: 2" `
    https://localhost:7001/examples/binding/7?q=tent

# The header value is required. Expect 400
curl.exe -i -w "\n" `
    https://localhost:7001/examples/binding/7?q=tent

# The OpenAPI document lists both versions. Expect 200
curl.exe -i -w "\n" https://localhost:7001/openapi/v1.json

# Earlier chapters' routes still answer. Expect 200
curl.exe -i -w "\n" `
    https://localhost:7001/api/catalog/category/camping

# Expect 200 with "orderNumber": "AW-0001"
curl.exe -i -w "\n" https://localhost:7001/api/orders/1

# Expect 200
curl.exe -i -w "\n" https://localhost:7001/ping
```
