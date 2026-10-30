# Chapter 7: Forms, Model Binding, and Validation

The complete code for Chapter 7's Full Example, matching the listings in the book. Like every chapter from Chapter 7, this is its own small project: start from this folder rather than adding it to an earlier chapter's project.

## Run it

Run `dotnet run --urls http://127.0.0.1:42107` from this folder. Open http://127.0.0.1:42107/Orders/Create in your browser to try the order form; the JSON validation route needs a request tool.

## Sending requests

`requests.http` holds every request this chapter checks, in order, each with the status you should see. Visual Studio 2026 runs it directly (open the file and select Send Request above a request); VS Code needs the REST Client extension.

In Windows PowerShell, type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. The commands below are the same requests. Run them from any folder; JSON bodies go to body.json first because Windows PowerShell removes double quotes from arguments it passes to curl.exe.

```powershell
# The one available SKU and a quantity in range. Expect 200 {"sku":"NOTEBOOK-101","quantity":2}
'{ "sku": "NOTEBOOK-101", "quantity": 2 }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" --data-binary "@body.json" http://127.0.0.1:42107/api/orders/validate

# The asynchronous availability check rejects an unknown SKU. Expect 400 with a validation problem
'{ "sku": "PEN-202", "quantity": 1 }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" --data-binary "@body.json" http://127.0.0.1:42107/api/orders/validate

# Quantity above 20. Expect 400
'{ "sku": "NOTEBOOK-101", "quantity": 25 }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" --data-binary "@body.json" http://127.0.0.1:42107/api/orders/validate

# A missing SKU fails Required. Expect 400
'{ "quantity": 1 }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" --data-binary "@body.json" http://127.0.0.1:42107/api/orders/validate

```
