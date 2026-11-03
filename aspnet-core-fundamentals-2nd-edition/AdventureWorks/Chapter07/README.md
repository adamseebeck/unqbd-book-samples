# AdventureWorks: Chapter 7 checkpoint

The AdventureWorks store app at the end of Chapter 7. It adds order input: dedicated input models, an asynchronous SKU check against the real catalog (`AvailableSkuAttribute` and `CatalogAvailability`), the built-in Minimal API validation (`AddValidation`) for `POST /api/orders/validate` and Chapter 5's v2 product endpoints, French and English request cultures, a public stylesheet under wwwroot, and an order form at `/Orders/Create` that validates input and stores an optional upload privately. Chapter 8 starts from this folder.

The order form and the API validate input only; no order is placed.

| Folder | Project |
| --- | --- |
| `AdventureWorks` | Web: hosting, endpoints and composition |
| `AdventureWorks.Core` | Domain types and contracts |
| `AdventureWorks.Infrastructure` | Implementations of the Core contracts |

`AdventureWorks.slnx` lists all three. Core references nothing; Infrastructure references Core; Web references both.

## Run it

From this folder, the one that holds global.json:

```shell
cd AdventureWorks
dotnet user-secrets set FeatureFlags:MaxCartItems 30
cd ..
dotnet run --project AdventureWorks
```

The first three lines store Chapter 2's harmless User Secret outside the project (Step 5); run them once, because every later checkpoint keeps the same UserSecretsId. In Visual Studio 2026, open AdventureWorks.slnx and press F5 instead. The launch profile in AdventureWorks/Properties/launchSettings.json starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Stop the app with Ctrl+C.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
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
```

### The order form with its antiforgery token

In a browser, open https://localhost:7001/Orders/Create. From Windows PowerShell 5.1, fetch the form once, keep its cookie in cookies.txt and copy the token out of the page, then post it with a small upload. Run these in a folder where you can create files.

```powershell
Remove-Item cookies.txt -ErrorAction SilentlyContinue
$page = (curl.exe -s -c cookies.txt `
    https://localhost:7001/Orders/Create) -join "`n"
$pattern = 'VerificationToken" type="hidden" value="([^"]+)'
$token = [regex]::Match($page, $pattern).Groups[1].Value
'test upload bytes' | Set-Content note.txt
$show = @('-s', '-w', '\nHTTP/%{http_version} %{response_code}\n')

# A valid submission with an upload. Expect 200
curl.exe @show -b cookies.txt `
    -F "__RequestVerificationToken=$token" `
    -F "Input.ProductId=1" -F "Input.Quantity=2" `
    -F "Input.Email=ada@example.com" -F "Upload=@note.txt" `
    https://localhost:7001/Orders/Create |
    Select-String 'HTTP/|Input validated'
```

The page reports "Input validated for 2 x Trail Helmet. No order has been placed." The file is now in AdventureWorks/PrivateUploads under a generated .bin name; no URL serves it. Delete it when you finish.
