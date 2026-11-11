# AdventureWorks: Chapter 6 Activity solution

For the Visual Studio steps, use Visual Studio with support for the .NET 11 SDK and `.slnx` solutions.

This is a worked solution to the chapter's Activity, built in a copy of the Chapter 6 checkpoint. Try the Activity yourself first. The next chapter starts from `../Chapter06`, not from this folder.

It adds a customer profile page at `/Customer/Profile` over one fixed, fictional customer (`CustomerProfileStore`), a `RecentOrders` View Component that lists Core `Order` records, and a named Deactivate handler that redirects to `/Customer/Deactivated`. Open the profile page in a browser to save an edit, see the TempData message once, and deactivate. Before adapting this to real customers, take the customer from the signed-in user (Chapter 8), never from request input.

## Run it

From this folder, the one that holds global.json:

```powershell
dotnet user-secrets set FeatureFlags:MaxCartItems 30 `
    --project AdventureWorks
dotnet run --project AdventureWorks
```

In Visual Studio, open AdventureWorks.slnx and press F5 instead. The launch profile in AdventureWorks/Properties/launchSettings.json starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Stop the app with Ctrl+C.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

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

# The Activity's profile page with its recent-orders component.
# Expect 200
curl.exe -i -w "\n" https://localhost:7001/Customer/Profile

# Deactivate without the form's cookie and token. Expect 400
curl.exe -i -w "\n" -X POST `
    https://localhost:7001/Customer/Profile?handler=Deactivate

# The confirmation page. Expect 200
curl.exe -i -w "\n" https://localhost:7001/Customer/Deactivated
```
