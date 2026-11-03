# AdventureWorks: Chapter 7 Activity solution

This is a worked solution to the chapter's Activity, built in a copy of the Chapter 7 checkpoint. Try the Activity yourself first. The next chapter starts from `../Chapter07`, not from this folder.

It adds `HeaderOnlyForms`, a separate seven-line host on http://localhost:5107 with one form-bound endpoint, `POST /newsletter`, and no token antiforgery, so you can watch .NET 11's Sec-Fetch-Site and Origin check on its own. Its CORS policy trusts the store's origin, https://localhost:7001, with credentials.

## Run it

From this folder, the one that holds global.json:

```shell
cd AdventureWorks
dotnet user-secrets set FeatureFlags:MaxCartItems 30
cd ..
dotnet run --project AdventureWorks
dotnet run --project HeaderOnlyForms
```

Run the last two commands in two terminals. In Visual Studio 2026, open AdventureWorks.slnx, right-click the solution, choose Configure Startup Projects, select Multiple startup projects and start both, then press F5. The launch profile in AdventureWorks/Properties/launchSettings.json starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Stop the app with Ctrl+C.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
$forms = 'http://localhost:5107'

# An available SKU and a quantity in range. Expect 200 with
# {"sku":"HLM-1001","quantity":2}
'{ "sku": "HLM-1001", "quantity": 2 }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/orders/validate

# A well-formed SKU the catalog does not have: the asynchronous
# check rejects it. Expect 400 with "Sku is unavailable"
'{ "sku": "PEN-2020", "quantity": 1 }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/orders/validate

# Not the AAA-9999 pattern. Expect 400
'{ "sku": "NOTEBOOK-101", "quantity": 1 }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/orders/validate

# Quantity above 20. Expect 400
'{ "sku": "HLM-1001", "quantity": 25 }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/orders/validate

# A missing SKU fails Required. Expect 400
'{ "quantity": 1 }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/orders/validate

# AddValidation now validates Chapter 5's v2 products as well.
# Expect 400 with "errors":
@'
{ "name": "Kayak", "sku": "KYK-0006",
  "price": 450, "category": "boats" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v2/products

# A French request formats the range numbers the French way.
# Expect 400 with "0,01"
@'
{ "name": "Free Sample", "sku": "FRE-0004",
  "price": 0, "category": "camping" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    -H "Accept-Language: fr" --data-binary "@body.json" `
    https://localhost:7001/api/v2/products

# A valid v2 product still saves. Expect 201
@'
{ "name": "Trail Bottle", "sku": "BTL-5001",
  "price": 24.99, "category": "accessories" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v2/products

# The order form page. Expect 200
curl.exe -i -w "\n" https://localhost:7001/Orders/Create

# A form POST without the antiforgery token. Expect 400
@'
Input.ProductId=1&Input.Quantity=2&Input.Email=ada@example.com
'@ | Set-Content body.json
curl.exe -i -w "\n" `
    -H "Content-Type: application/x-www-form-urlencoded" `
    --data-binary "@body.json" `
    https://localhost:7001/Orders/Create

# The public stylesheet is a static file. Expect 200
curl.exe -i -w "\n" https://localhost:7001/css/site.css

# Uploads are stored outside wwwroot, so no URL serves them.
# Expect 404
curl.exe -i -w "\n" `
    https://localhost:7001/PrivateUploads/example.bin

# Earlier chapters' routes still answer. Expect 200
curl.exe -i -w "\n" https://localhost:7001/api/catalog/1

# Expect 200
curl.exe -i -w "\n" https://localhost:7001/Products

# The header-only host: a same-origin form post. Expect 200
'email=ada@example.com' | Set-Content body.json
curl.exe -i -w "\n" `
    -H "Content-Type: application/x-www-form-urlencoded" `
    -H "Sec-Fetch-Site: same-origin" --data-binary "@body.json" `
    "$forms/newsletter"

# An untrusted cross-site form post is rejected by the header
# check alone. Expect 400
'email=ada@example.com' | Set-Content body.json
curl.exe -i -w "\n" `
    -H "Content-Type: application/x-www-form-urlencoded" `
    -H "Sec-Fetch-Site: cross-site" `
    -H "Origin: https://evil.example" --data-binary "@body.json" `
    "$forms/newsletter"

# The store's origin is a trusted credentialed CORS origin, so the
# same cross-site post is accepted. Expect 200
'email=ada@example.com' | Set-Content body.json
curl.exe -i -w "\n" `
    -H "Content-Type: application/x-www-form-urlencoded" `
    -H "Sec-Fetch-Site: cross-site" `
    -H "Origin: https://localhost:7001" `
    --data-binary "@body.json" "$forms/newsletter"

# The form defense does not reject JSON by itself: cross-site
# metadata on the store's JSON route. Expect 200
'{ "sku": "HLM-1001", "quantity": 1 }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" `
    -H "Sec-Fetch-Site: cross-site" `
    -H "Origin: https://evil.example" --data-binary "@body.json" `
    https://localhost:7001/api/orders/validate
```
