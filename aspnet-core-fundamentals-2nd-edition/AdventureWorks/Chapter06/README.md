# AdventureWorks: Chapter 6 checkpoint

The AdventureWorks store app at the end of Chapter 6. It adds Razor Pages over the same product store the catalog and both product APIs use: a product list at `/Products`, an edit page at `/Products/Edit/{id}` with server validation, a version check against stale forms, Post-Redirect-Get with TempData and a named Delete handler, plus a layout, a prefixed form partial, a View Component and an `/Admin` area. Chapter 7 starts from this folder.

An edit made on the page shows up in `/api/catalog`, `/api/v1/products` and `/api/v2/products`, and an API update raises the version the page checks, because there is one `IProductService` singleton. The `/Admin` area is not protected yet; Chapter 9 does that.

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

The first command stores Chapter 2's harmless User Secret outside the project (Step 5); run it once, because every later checkpoint keeps the same UserSecretsId. In Visual Studio 2026, open AdventureWorks.slnx and press F5 instead. The launch profile in AdventureWorks/Properties/launchSettings.json starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Stop the app with Ctrl+C.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
# The product list page. Expect 200
curl.exe -i -w "\n" https://localhost:7001/Products

# The edit form for product 1. Expect 200
curl.exe -i -w "\n" https://localhost:7001/Products/Edit/1

# The route template needs an integer. Expect 404
curl.exe -i -w "\n" `
    https://localhost:7001/Products/Edit/not-an-int

# An integer with no product: the handler returns NotFound.
# Expect 404
curl.exe -i -w "\n" https://localhost:7001/Products/Edit/999

# The Admin area page (not yet protected). Expect 200
curl.exe -i -w "\n" https://localhost:7001/Admin

# A POST without the antiforgery token is rejected before the
# handler runs. Expect 400
@'
Input.Name=Trail+Helmet&Input.Price=29.99&Input.Version=1
'@ | Set-Content body.json
curl.exe -i -w "\n" `
    -H "Content-Type: application/x-www-form-urlencoded" `
    --data-binary "@body.json" `
    https://localhost:7001/Products/Edit/1

# The pages and the APIs share one store. Expect 200 with "sku":
# "HLM-1001"
curl.exe -i -w "\n" https://localhost:7001/api/v2/products/1

# Earlier chapters' routes still answer. Expect 200
curl.exe -i -w "\n" https://localhost:7001/api/catalog

# Expect 200 with "orderNumber": "AW-0001"
curl.exe -i -w "\n" https://localhost:7001/api/orders/1

# Expect 200
curl.exe -i -w "\n" https://localhost:7001/ping
```

### Form posts with the antiforgery token

A browser handles the antiforgery cookie and token for you: open https://localhost:7001/Products/Edit/1, submit a name shorter than three characters, save a valid edit, then use the browser's Back button and save the old form again to see the conflict message. To do the same from Windows PowerShell 5.1, fetch the form once, keep its cookie in cookies.txt, and copy the token out of the page. Run these after the commands above, in the same folder, while the app is running.

```powershell
# Fetch the edit form. -c saves the antiforgery cookie; the page
# holds the matching token.
Remove-Item cookies.txt -ErrorAction SilentlyContinue
$page = (curl.exe -s -c cookies.txt `
    https://localhost:7001/Products/Edit/1) -join "`n"
$pattern = 'VerificationToken" type="hidden" value="([^"]+)'
$token = [regex]::Match($page, $pattern).Groups[1].Value
$form = "Input.Version=1&__RequestVerificationToken=$token"
$show = @('-s', '-w', '\nHTTP/%{http_version} %{response_code}\n')

# A two-letter name redisplays the form with its error. Expect 200
curl.exe @show -b cookies.txt `
    -d "Input.Name=AB&Input.Price=31.99" `
    -d $form https://localhost:7001/Products/Edit/1 |
    Select-String 'HTTP/|field-validation-error'

# A valid edit redirects back to the form (Post-Redirect-Get).
# Expect 302
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -d "Input.Name=Trail+Helmet+II&Input.Price=31.99" `
    -d $form https://localhost:7001/Products/Edit/1

# The same form again still carries version 1, so the store
# reports a conflict and the page says so. Expect 200
curl.exe @show -b cookies.txt `
    -d "Input.Name=Trail+Helmet+III&Input.Price=32.99" `
    -d $form https://localhost:7001/Products/Edit/1 |
    Select-String 'HTTP/|product changed'

# The API sees the page's edit: version 2. Expect 200
curl.exe -i -w "\n" https://localhost:7001/api/v2/products/1

# The named Delete handler needs only the token. Expect 302
curl.exe -i -w "\n" -b cookies.txt `
    -d "__RequestVerificationToken=$token" `
    "https://localhost:7001/Products/Edit/1?handler=Delete"

# Gone from the API as well. Expect 404
curl.exe -i -w "\n" https://localhost:7001/api/v2/products/1
```

Restart the app to restore the starting products.
