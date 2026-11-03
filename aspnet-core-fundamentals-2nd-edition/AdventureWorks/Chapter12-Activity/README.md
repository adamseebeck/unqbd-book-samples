# AdventureWorks: Chapter 12 Activity solution

This is a worked solution to the chapter's Activity, built in a copy of the Chapter 12 checkpoint. Try the Activity yourself first. The next chapter starts from `../Chapter12`, not from this folder.

It replaces the cache-aside read of warehouse stock with HybridCache, so concurrent misses for the same SKU make one call to InventoryService instead of one each. requests.http is the checkpoint's; the stampede check needs concurrent requests, so it is not in the file.

## Run it

From this folder, the one that holds global.json:

```shell
cd AdventureWorks
dotnet user-secrets set FeatureFlags:MaxCartItems 30
dotnet user-secrets set Training:Password "Practice-Pass-2026!"
cd ..
dotnet run --project InventoryService
```

Then, in a second terminal in this folder, run `dotnet run --project AdventureWorks`. InventoryService is the warehouse stand-in on http://127.0.0.1:42132; the app calls it for stock. In Visual Studio 2026, open AdventureWorks.slnx, choose Configure Startup Projects, select Multiple startup projects, set AdventureWorks and InventoryService to Start, and press F5. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. The two secrets are Chapter 2's harmless setting and Chapter 8's test-account password; set them once, because every later checkpoint keeps the same UserSecretsId. Any password with 12 or more characters, upper and lower case letters, a digit and a symbol works; requests.http uses this one. The first run creates adventureworks.db in the AdventureWorks folder and five test accounts: casey, jordan, morgan, riley and avery, each at @adventure-works.test.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
Remove-Item cookies.txt -ErrorAction SilentlyContinue
$inventory = 'http://127.0.0.1:42132'

# Start the warehouse stand-in clean. Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    "$inventory/control/reset"

# Output caching. The first read runs the endpoint. Expect 200,
# and it has no Age header
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/1

# The second comes from the output cache. Expect 200, and it has
# an Age header
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/1

# The public catalog is compressed. Expect 200, and it carries
# Content-Encoding: br
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Accept-Encoding: br" https://localhost:7001/api/catalog

# Other routes are not. Expect 200, and it has no Content-Encoding
# header
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Accept-Encoding: br" https://localhost:7001/api/status

# Warehouse stock through the distributed cache: a miss, so one
# call to InventoryService. Expect 200 with "available": 42
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/1/availability

# Expect 200 with {"attempts":1}
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    "$inventory/control/attempts"

# A hit: no call. Expect 200 with "available": 42
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/1/availability

# Still one attempt. Expect 200 with {"attempts":1}
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    "$inventory/control/attempts"

# The next two stock calls fail with 503. Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    "$inventory/control/fail/2"

# Two retries absorb both failures. Expect 200 with "available":
# 17
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/2/availability

# One plus three attempts. Expect 200 with {"attempts":4}
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    "$inventory/control/attempts"

# A 400 is permanent. Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    "$inventory/control/status/400"

# Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    "$inventory/control/fail/1"

# No retry for a 400. Expect 503
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/3/availability

# One more attempt only. Expect 200 with {"attempts":5}
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    "$inventory/control/attempts"

# Now every call fails with 503. Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    "$inventory/control/reset"

# Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    "$inventory/control/fail/100"

# This failure makes three of the last six attempts (half) fail,
# so the circuit breaker opens, and the retries fail fast.
# Expect 503
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/4/availability

# One attempt reached the service. Expect 200 with {"attempts":1}
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    "$inventory/control/attempts"

# While the breaker is open, no call reaches the service.
# Expect 503
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/4/availability

# Expect 200 with {"attempts":1}
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    "$inventory/control/attempts"

# Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    "$inventory/control/reset"

# Rate limiting: five searches per ten seconds for each client.
# Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/search?q=trail

# Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/search?q=trail

# Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/search?q=trail

# Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/search?q=trail

# Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/search?q=trail

# The sixth is refused. Expect 429, and it has a Retry-After
# header
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/search?q=trail

# A signed-in user has a partition of her own. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/casey

# Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/search?q=trail

# Eviction: the Admin changes a price. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/avery

# Expect 204
@'
{ "name": "Trail Helmet", "price": 31.99 }
'@ | Set-Content body.json
curl.exe -i -w "\n" -X PUT -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v2/products/1

# Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-out

# The write evicted the "catalog" tag, so this read runs the
# endpoint again. Expect 200 with "price": 31.99, and it has no
# Age header
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/1

# Put the price back. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/avery

# Expect 204
@'
{ "name": "Trail Helmet", "price": 29.99 }
'@ | Set-Content body.json
curl.exe -i -w "\n" -X PUT -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/v2/products/1

# Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-out

# Expect 200 with "price": 29.99
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog/1
```
