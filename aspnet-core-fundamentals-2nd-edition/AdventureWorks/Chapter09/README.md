# AdventureWorks: Chapter 9 checkpoint

The AdventureWorks store app at the end of Chapter 9. It adds authorization: a fallback policy that requires a signed-in user everywhere except the endpoints marked AllowAnonymous, the Admin role, an age policy, a policy provider for department policies, and resource-based rules on orders, which now have owners. Chapter 10 starts from this folder.

| Who | Account | Can |
| --- | --- | --- |
| Anyone | (none) | Browse the catalog, product reads, `/Products`, sign in |
| casey | customer, owns AW-0001 and AW-0003 | Read her orders; open `/Products/Restricted` |
| jordan | buyer in Finance, owns AW-0002 | Read and archive his order |
| morgan | Finance staff | Read any order; archive none |
| riley | born 2010 | Not `/Products/Restricted` |
| avery | Admin | Delete orders, write products, open `/Admin` |

Sign in with the test sign-in in requests.http, or at /Account/Login with the account's email (for example casey@adventure-works.test) and the Training:Password secret.

| Folder | Project |
| --- | --- |
| `AdventureWorks` | Web: hosting, endpoints and composition |
| `AdventureWorks.Core` | Domain types and contracts |
| `AdventureWorks.Infrastructure` | Implementations of the Core contracts |

`AdventureWorks.slnx` lists every project in this folder. Core references nothing; Infrastructure references Core; Web references both.

## Run it

From this folder, the one that holds global.json:

```powershell
dotnet user-secrets set FeatureFlags:MaxCartItems 30 `
    --project AdventureWorks
dotnet user-secrets set Training:Password "Practice-Pass-2026!" `
    --project AdventureWorks
dotnet run --project AdventureWorks
```

The two secrets are Chapter 2's harmless setting and Chapter 8's test-account password; set them once, because every later checkpoint keeps the same UserSecretsId. Any password with 12 or more characters, upper and lower case letters, a digit and a symbol works; requests.http uses this one. The first run creates adventureworks.db in the AdventureWorks folder and five test accounts: casey, jordan, morgan, riley and avery, each at @adventure-works.test. In Visual Studio 2026, open AdventureWorks.slnx and press F5 instead. The launch profile in AdventureWorks/Properties/launchSettings.json starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Stop the app with Ctrl+C.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
Remove-Item cookies.txt -ErrorAction SilentlyContinue

# Public endpoints stay public under the fallback policy.
# Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog

# Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/v1/products

# Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/Products

# Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/health

# Everything else needs a signed-in user: an order. Expect 401
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/orders/1

# A product write needs the Admin role. Expect 401
@'
{ "name": "Trail Bottle", "sku": "BTL-5001",
  "price": 24.99, "category": "accessories" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v1/products

# A page redirects to sign in instead. Expect 302
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/Admin

# Anonymous archive. Expect 401
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/orders/1/archive

# casey: a customer who owns orders 1 and 3. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/casey

# The owner reads her order. Expect 200 with "orderNumber":
# "AW-0001"
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/orders/1

# Order 2 is jordan's. Expect 403
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/orders/2

# casey's antiforgery token for API writes. Expect 200
curl.exe -s -D - -o caseyToken.json -b cookies.txt `
    -c cookies.txt https://localhost:7001/api/csrf
$caseyToken = Get-Content caseyToken.json | ConvertFrom-Json

# Owner, but not in Finance: the endpoint policy refuses.
# Expect 403
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    -H "X-CSRF-TOKEN: $($caseyToken.token)" `
    https://localhost:7001/api/orders/1/archive

# Born 1990: the age policy allows the restricted page. Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/Products/Restricted

# Not an Admin: no product writes. Expect 403
@'
{ "name": "Trail Bottle", "sku": "BTL-5001",
  "price": 24.99, "category": "accessories" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v2/products

# A page sends a signed-in user who fails its policy to the
# access-denied page. Expect 302
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/Products/Edit/1

# morgan: Finance staff who owns no orders. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/morgan

# Finance staff may read any order. Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/orders/1

# Expect 200
curl.exe -s -D - -o morganToken.json -b cookies.txt `
    -c cookies.txt https://localhost:7001/api/csrf
$morganToken = Get-Content morganToken.json | ConvertFrom-Json

# In Finance, but not the owner: the resource rule refuses.
# Expect 403
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    -H "X-CSRF-TOKEN: $($morganToken.token)" `
    https://localhost:7001/api/orders/1/archive

# jordan: a business buyer in Finance who owns order 2. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/jordan

# Expect 200
curl.exe -s -D - -o jordanToken.json -b cookies.txt `
    -c cookies.txt https://localhost:7001/api/csrf
$jordanToken = Get-Content jordanToken.json | ConvertFrom-Json

# Both rules pass, but the write has no antiforgery token.
# Expect 400
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/orders/2/archive

# Finance and owner, with the token. Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    -H "X-CSRF-TOKEN: $($jordanToken.token)" `
    https://localhost:7001/api/orders/2/archive

# The flag changed only on the allowed request. Expect 200 with
# "archived": true
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/orders/2

# riley: born 2010. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/riley

# Under 21: sent to the access-denied page. Expect 302
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/Products/Restricted

# avery: the Admin. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/avery

# Expect 200
curl.exe -s -D - -o averyToken.json -b cookies.txt `
    -c cookies.txt https://localhost:7001/api/csrf
$averyToken = Get-Content averyToken.json | ConvertFrom-Json

# Only the Admin role may delete an order. Expect 204
curl.exe -i -w "\n" -X DELETE -b cookies.txt -c cookies.txt `
    -H "X-CSRF-TOKEN: $($averyToken.token)" `
    https://localhost:7001/api/orders/3

# Gone. Expect 404
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/orders/3

# The Admin may write products. Expect 201
@'
{ "name": "Trail Bottle", "sku": "BTL-5001",
  "price": 24.99, "category": "accessories" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v2/products

# And open the Admin area and the product editor. Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/Admin

# Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/Products/Edit/1

# Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-out
```
