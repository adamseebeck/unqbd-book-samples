# Chapter 11: Application Services and Architectural Patterns

The complete code for Chapter 11's Full Example, matching the listings in the book. Like every chapter from Chapter 7, this is its own small project: start from this folder rather than adding it to an earlier chapter's project.

## What differs from the book

DevSignIn.cs, DevSeed.cs and two lines before `app.Run();` in Program.cs are the only additions to the book's listing:

```csharp
app.MapDevelopmentSignIn();
await app.SeedDevelopmentDataAsync();
```

Both do nothing outside Development. DevSignIn.cs signs in owner-1 or owner-2 with this host's real cookie handler; a request cannot choose its own claims. DevSeed.cs adds one product to the empty, migrated database: product 1, Notebook, 1200 cents. An order line always stores that server-side price.

## Run it

Apply the Chapter 10 migration procedure to service-training.db first. From this folder:

```powershell
dotnet tool restore
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

The app starts in Development at https://localhost:42111. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. To start over, delete service-training.db, run `dotnet ef database update` again and restart.

## Sending requests

`requests.http` holds every request this chapter checks, in order, each with the status you should see. Visual Studio 2026 runs it directly (open the file and select Send Request above a request); VS Code needs the REST Client extension.

The sign-in request sets this host's cookie. VS Code REST Client keeps it for the requests that follow. If your client does not keep cookies, use the curl.exe commands below, which save them in cookies.txt.

In Windows PowerShell, type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. The commands below are the same requests. Run them from any folder; JSON bodies go to body.json first because Windows PowerShell removes double quotes from arguments it passes to curl.exe.

```powershell
Remove-Item cookies.txt -ErrorAction SilentlyContinue

# Sign in as owner-1. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt https://localhost:42111/dev/sign-in/owner-1

# Antiforgery token for this user. Expect 200
$ownerToken = curl.exe -s -b cookies.txt -c cookies.txt https://localhost:42111/csrf | ConvertFrom-Json
$ownerToken

# Create an order for two of product 1. Expect 201 with "succeeded": true and the new orderId
'{ "orderNumber": "AW-1001", "items": [ { "productId": 1, "quantity": 2 } ] }' | Set-Content body.json
$created = curl.exe -s -b cookies.txt -c cookies.txt -H "Content-Type: application/json" -H "X-CSRF-TOKEN: $($ownerToken.token)" --data-binary "@body.json" https://localhost:42111/api/orders | ConvertFrom-Json
$created

# Read it back. Expect 200 with unitPriceCents 1200, the stored product price
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42111/api/orders/$($created.orderId)

# The same order number again. Expect 400 "The order could not be created with that number."
'{ "orderNumber": "AW-1001", "items": [ { "productId": 1, "quantity": 1 } ] }' | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt -H "Content-Type: application/json" -H "X-CSRF-TOKEN: $($ownerToken.token)" --data-binary "@body.json" https://localhost:42111/api/orders

# No lines. Expect 400 "Use 1-20 lines with quantities from 1 to 20."
'{ "orderNumber": "AW-EMPTY", "items": [] }' | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt -H "Content-Type: application/json" -H "X-CSRF-TOKEN: $($ownerToken.token)" --data-binary "@body.json" https://localhost:42111/api/orders

# A quantity of zero. Expect 400
'{ "orderNumber": "AW-ZERO", "items": [ { "productId": 1, "quantity": 0 } ] }' | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt -H "Content-Type: application/json" -H "X-CSRF-TOKEN: $($ownerToken.token)" --data-binary "@body.json" https://localhost:42111/api/orders

# Duplicate product lines. Expect 400 "Combine duplicate product lines."
'{ "orderNumber": "AW-DUP", "items": [ { "productId": 1, "quantity": 1 }, { "productId": 1, "quantity": 2 } ] }' | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt -H "Content-Type: application/json" -H "X-CSRF-TOKEN: $($ownerToken.token)" --data-binary "@body.json" https://localhost:42111/api/orders

# An unknown product. Expect 400 "A selected product is unavailable."
'{ "orderNumber": "AW-UNKNOWN", "items": [ { "productId": 99, "quantity": 1 } ] }' | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt -H "Content-Type: application/json" -H "X-CSRF-TOKEN: $($ownerToken.token)" --data-binary "@body.json" https://localhost:42111/api/orders

# Without the antiforgery token. Expect 400
'{ "orderNumber": "AW-NOTOKEN", "items": [ { "productId": 1, "quantity": 1 } ] }' | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt -H "Content-Type: application/json" --data-binary "@body.json" https://localhost:42111/api/orders

# Sign in as owner-2. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt https://localhost:42111/dev/sign-in/owner-2

# Another owner cannot load owner-1's order. Expect 404
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42111/api/orders/$($created.orderId)

```
