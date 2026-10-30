# Chapter 13: Designing RESTful APIs

The complete code for Chapter 13's Full Example, matching the listings in the book; Activity/ holds the line-item activity solution. Like every chapter from Chapter 7, this is its own small project: start from this folder rather than adding it to an earlier chapter's project.

## Run it

Run `dotnet run --urls http://127.0.0.1:42141` from this folder. Restarting the process clears the sample data.

## Sending requests

`requests.http` holds every request this chapter checks, in order, each with the status you should see. Visual Studio 2026 runs it directly (open the file and select Send Request above a request); VS Code needs the REST Client extension.

In Windows PowerShell, type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. The commands below are the same requests. Run them from any folder; JSON bodies go to body.json first because Windows PowerShell removes double quotes from arguments it passes to curl.exe.

```powershell
# Create an order. Expect 201 with a Location header pointing at the new order
'{ "customerName": "Ada Lovelace", "items": [ { "productId": 1, "quantity": 2 } ] }' | Set-Content body.json
$created = curl.exe -s -H "Content-Type: application/json" --data-binary "@body.json" http://127.0.0.1:42141/api/orders | ConvertFrom-Json
$created

# Follow the Location header. Expect 200
curl.exe -i -w "\n" http://127.0.0.1:42141/api/orders/$($created.id)

# A second order, so paging has two pages. Expect 201
'{ "customerName": "Grace Hopper", "items": [ { "productId": 2, "quantity": 1 } ] }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" --data-binary "@body.json" http://127.0.0.1:42141/api/orders

# One order per page. Expect 200 with X-Total-Count: 2 and a Link header to the next page
curl.exe -i -w "\n" http://127.0.0.1:42141/api/orders?pageSize=1

# A page size above 100. Expect 400 "Invalid pagination"
curl.exe -i -w "\n" http://127.0.0.1:42141/api/orders?pageSize=500

# Change the customer name. Expect 204
'{ "customerName": "Ada King" }' | Set-Content body.json
curl.exe -i -w "\n" -X PATCH -H "Content-Type: application/json" --data-binary "@body.json" http://127.0.0.1:42141/api/orders/$($created.id)

# A blank customer name. Expect 400
'{ "customerName": "  " }' | Set-Content body.json
curl.exe -i -w "\n" -X PATCH -H "Content-Type: application/json" --data-binary "@body.json" http://127.0.0.1:42141/api/orders/$($created.id)

# An order with no items. Expect 400 with a validation problem
'{ "customerName": "Empty", "items": [] }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" --data-binary "@body.json" http://127.0.0.1:42141/api/orders

# Delete the first order. Expect 204
curl.exe -i -w "\n" -X DELETE http://127.0.0.1:42141/api/orders/$($created.id)

# It is gone. Expect 404 "Order not found"
curl.exe -i -w "\n" http://127.0.0.1:42141/api/orders/$($created.id)

```
