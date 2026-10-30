# Chapter 5: Full Example

The complete code for Chapter 5's Full Example, matching the listings in the book.

Run `dotnet run` from this folder. The launch profile in Properties/launchSettings.json is the one Chapter 1 builds: it starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once.

The `global.json` here selects the .NET 11 SDK (11.0.100 or later).

This project restores Microsoft.AspNetCore.OpenApi 11.0.0.

## Sending requests

`requests.http` holds every request this chapter checks, in order, each with the status you should see. Visual Studio 2026 runs it directly (open the file and select Send Request above a request); VS Code needs the REST Client extension.

In Windows PowerShell, type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. The commands below are the same requests. Run them from any folder; JSON bodies go to body.json first because Windows PowerShell removes double quotes from arguments it passes to curl.exe.

```powershell
# The v1 controller lists the starting product. Expect 200 with "id": 1
curl.exe -i -w "\n" https://localhost:7001/api/v1/products

# Create a product through v1. Expect 201 with "id": 2 and a Location header
'{ "name": "Trail Bottle", "price": 24.99, "sku": "TRL-0001" }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" --data-binary "@body.json" https://localhost:7001/api/v1/products

# The same singleton store serves v2. Expect 200 with "id": 2
curl.exe -i -w "\n" https://localhost:7001/api/v2/products/2

# Create a product through v2. Expect 201 with "id": 3
'{ "name": "Camp Mug", "price": 12.50, "sku": "CMP-0002" }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" --data-binary "@body.json" https://localhost:7001/api/v2/products

# v1 lists all three products. Expect 200
curl.exe -i -w "\n" https://localhost:7001/api/v1/products

# Update through v1. Expect 204
'{ "name": "Trail Bottle XL", "price": 27.99 }' | Set-Content body.json
curl.exe -i -w "\n" -X PUT -H "Content-Type: application/json" --data-binary "@body.json" https://localhost:7001/api/v1/products/2

# Update through v2. Expect 204
'{ "name": "Camp Mug Large", "price": 14.00 }' | Set-Content body.json
curl.exe -i -w "\n" -X PUT -H "Content-Type: application/json" --data-binary "@body.json" https://localhost:7001/api/v2/products/3

# A name shorter than 3 characters fails validation in v1. Expect 400
'{ "name": "AB", "price": 5, "sku": "ABC-0003" }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" --data-binary "@body.json" https://localhost:7001/api/v1/products

# A zero price fails validation in v2. Expect 400
'{ "name": "Free Sample", "price": 0, "sku": "FRE-0004" }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" --data-binary "@body.json" https://localhost:7001/api/v2/products

# A lowercase SKU fails the pattern in v1. Expect 400
'{ "name": "Lantern", "price": 30, "sku": "lan-0005" }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" --data-binary "@body.json" https://localhost:7001/api/v1/products

# The same lowercase SKU fails in v2. Expect 400
'{ "name": "Lantern", "price": 30, "sku": "lan-0005" }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" --data-binary "@body.json" https://localhost:7001/api/v2/products

# Nothing invalid was stored. Expect 200 with only ids 1, 2 and 3
curl.exe -i -w "\n" https://localhost:7001/api/v2/products

# Paging outside the allowed range. Expect 400
curl.exe -i -w "\n" https://localhost:7001/api/v2/products?page=0

# Delete through v1. Expect 204
curl.exe -i -w "\n" -X DELETE https://localhost:7001/api/v1/products/2

# Gone from v2 as well. Expect 404
curl.exe -i -w "\n" https://localhost:7001/api/v2/products/2

# Delete through v2. Expect 204
curl.exe -i -w "\n" -X DELETE https://localhost:7001/api/v2/products/3

# Deleting again finds nothing. Expect 404
curl.exe -i -w "\n" -X DELETE https://localhost:7001/api/v2/products/3

# The OpenAPI document (Development only). Expect 200
curl.exe -i -w "\n" https://localhost:7001/openapi/v1.json

```
