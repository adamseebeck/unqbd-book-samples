# AdventureWorks: Chapter 4 Activity solution

This is a worked solution to the chapter's Activity, built in a copy of the Chapter 4 checkpoint. Try the Activity yourself first. The next chapter starts from `../Chapter04`, not from this folder.

It adds `SkuFormatConstraint` (three ASCII uppercase letters, a hyphen and four ASCII digits, checked ordinally), a `/inventory/{sku:skuFormat}` route, and a `/api/products` group tagged Product Management with uniquely named endpoints, a mock POST and generated related-product links (ActivityEndpoints.cs). Chapter 5 uses `/api/v1/products` and `/api/v2/products` for the real product API, so this group stays in the Activity copy.

## Run it

From this folder, the one that holds global.json:

```shell
cd AdventureWorks
dotnet user-secrets set FeatureFlags:MaxCartItems 30
cd ..
dotnet run --project AdventureWorks
```

In Visual Studio 2026, open AdventureWorks.slnx and press F5 instead. The launch profile in AdventureWorks/Properties/launchSettings.json starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Stop the app with Ctrl+C.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
$reviewId = '3f2504e0-4f89-11d3-9a0c-0305e82c3301'

# The catalog group lists products and links the featured one.
# Expect 200 with "featured": "/api/catalog/3"
curl.exe -i -w "\n" https://localhost:7001/api/catalog

# One product with a generated ReviewsLink to follow next.
# Expect 200 with "sku": "HLM-1001"
curl.exe -i -w "\n" https://localhost:7001/api/catalog/1

# Following ReviewsLink. Expect 200 with "productId": 1
curl.exe -i -w "\n" https://localhost:7001/api/catalog/1/reviews

# A matched integer that fails the handler's rule. Expect 400
curl.exe -i -w "\n" https://localhost:7001/api/catalog/0

# A matched integer with no product. Expect 404
curl.exe -i -w "\n" https://localhost:7001/api/catalog/99

# Not an integer: no route matches. Expect 404
curl.exe -i -w "\n" https://localhost:7001/api/catalog/abc

# The literal search route, not {id:int}. Expect 200
curl.exe -i -w "\n" `
    https://localhost:7001/api/catalog/search?q=trail

# The custom constraint accepts a known category. Expect 200 with
# "sku": "BIK-3001"
curl.exe -i -w "\n" `
    https://localhost:7001/api/catalog/category/bikes

# The constraint ignores case. Expect 200
curl.exe -i -w "\n" `
    https://localhost:7001/api/catalog/category/BIKES

# An unknown category selects no route. Expect 404
curl.exe -i -w "\n" `
    https://localhost:7001/api/catalog/category/toys

# Optional segment absent. Expect 200 with {"page":1}
curl.exe -i -w "\n" https://localhost:7001/examples/orders

# Optional segment present. Expect 200 with {"page":3}
curl.exe -i -w "\n" https://localhost:7001/examples/orders/3

# The default value. Expect 200 with {"status":"active"}
curl.exe -i -w "\n" https://localhost:7001/examples/status

# The alpha constraint rejects digits. Expect 404
curl.exe -i -w "\n" https://localhost:7001/examples/status/123

# The catch-all keeps the slash. Expect 200 with
# {"path":"manuals/tent.pdf"}
curl.exe -i -w "\n" `
    https://localhost:7001/examples/files/manuals/tent.pdf

# No constraint: text reaches binding and fails there. Expect 400
curl.exe -i -w "\n" `
    https://localhost:7001/examples/unconstrained/abc

# min(1) rejects 0 during route selection. Expect 404
curl.exe -i -w "\n" `
    "https://localhost:7001/examples/reviews/0/$reviewId"

# Stacked constraints match. Expect 200
curl.exe -i -w "\n" `
    "https://localhost:7001/examples/reviews/1/$reviewId"

# The OpenAPI document (Development only). Expect 200
curl.exe -i -w "\n" https://localhost:7001/openapi/v1.json

# Earlier chapters' routes still answer. Expect 200
curl.exe -i -w "\n" https://localhost:7001/ping

# Expect 200 with "orderNumber": "AW-0001"
curl.exe -i -w "\n" https://localhost:7001/api/orders/1

# Expect 400 with "title": "Invalid request"
curl.exe -i -w "\n" https://localhost:7001/api/orders/0

# Expect 200
curl.exe -i -w "\n" https://localhost:7001/health

# Expect 200 with "maxCartItems": 30
curl.exe -i -w "\n" https://localhost:7001/api/config

# The Activity's SKU constraint accepts AAA-9999. Expect 200 with
# {"sku":"ABC-1234"}
curl.exe -i -w "\n" https://localhost:7001/inventory/ABC-1234

# Lowercase letters fail the ordinal check. Expect 404
curl.exe -i -w "\n" https://localhost:7001/inventory/abc-1234

# Expect 404
curl.exe -i -w "\n" https://localhost:7001/inventory/invalid

# The product-management group lists IDs. Expect 200
curl.exe -i -w "\n" https://localhost:7001/api/products

# Expect 200 with "sku": "HLM-1001"
curl.exe -i -w "\n" https://localhost:7001/api/products/1

# The mock POST returns a Location for product 3. Expect 201
curl.exe -i -w "\n" -X POST https://localhost:7001/api/products

# Generated relative links to two related products. Expect 200
# with "/api/products/2"
curl.exe -i -w "\n" `
    https://localhost:7001/api/products/1/related

# The catalog's literal search route still wins. Expect 200
curl.exe -i -w "\n" `
    https://localhost:7001/api/catalog/search?q=tent
```
