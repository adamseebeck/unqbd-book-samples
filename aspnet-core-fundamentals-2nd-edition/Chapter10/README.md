# Chapter 10: Entity Framework Core Integration and Data Access Patterns

The complete code for Chapter 10's Full Example, matching the listings in the book. Like every chapter from Chapter 7, this is its own small project: start from this folder rather than adding it to an earlier chapter's project.

## What differs from the book

DevSignIn.cs, DevSeed.cs and two lines before `app.Run();` in Program.cs are the only additions to the book's listing:

```csharp
app.MapDevelopmentSignIn();
await app.SeedDevelopmentDataAsync();
```

Both do nothing outside Development. DevSignIn.cs signs in owner-1 or owner-2 with this host's real cookie handler (POST /dev/sign-in/owner-1); a request cannot choose its own claims. DevSeed.cs fills an empty, migrated database with two products, two inventory positions for the Notebook (for TransferAsync), 25 orders for owner-1 (AW-0001 to AW-0025, newest first, ids 10000000-0000-0000-0000-000000000001 to ...025, versions a0000000-0000-0000-0000-000000000001 to ...025) and one order for owner-2 (AW-0026). It never migrates, and it leaves a database that already has products unchanged. To use them in your own project, copy both files and add the two lines.

## Run it

Create and apply the migration before the first run. The EF Core tools find ApplicationDbContext through the AddDbContext registration in Program.cs, so Program.cs and OrderQueries.cs must be in the project before `dotnet ef migrations add` (in your own project, add them first). From this folder:

```powershell
dotnet tool restore
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

The app starts in Development at https://localhost:42110, and its log says "DevSeed added 2 products, 2 inventory positions and 26 orders." If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. To start over, delete orders-training.db, run `dotnet ef database update` again and restart the app. The SQL Server variant (Database__Provider=SqlServer) needs its own migration and a disposable database, as the chapter explains; it was not run for this companion.

## Sending requests

`requests.http` holds every request this chapter checks, in order, each with the status you should see. Visual Studio 2026 runs it directly (open the file and select Send Request above a request); VS Code needs the REST Client extension.

The sign-in request sets this host's cookie. VS Code REST Client keeps it for the requests that follow. If your client does not keep cookies, use the curl.exe commands below, which save them in cookies.txt.

In Windows PowerShell, type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. The commands below are the same requests. Run them from any folder; JSON bodies go to body.json first because Windows PowerShell removes double quotes from arguments it passes to curl.exe.

```powershell
Remove-Item cookies.txt -ErrorAction SilentlyContinue

# Sign in as owner-1, who has 25 seeded orders. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt https://localhost:42110/dev/sign-in/owner-1

# Report page 1. Expect 200 with 20 orders, newest first: AW-0001 to AW-0020
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42110/api/orders/report

# Report page 2. Expect 200 with the last 5 orders: AW-0021 to AW-0025
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42110/api/orders/report?page=2

# Pages outside 1 to 10000 are rejected. Expect 400
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42110/api/orders/report?page=0

# Antiforgery token for this user. Expect 200
$ownerToken = curl.exe -s -b cookies.txt -c cookies.txt https://localhost:42110/csrf | ConvertFrom-Json
$ownerToken

# Update AW-0001 with the version the client last saw. Expect 200 with a new version
'{ "notes": "Leave at the front desk.", "version": "a0000000-0000-0000-0000-000000000001" }' | Set-Content body.json
$firstUpdate = curl.exe -s -X PUT -b cookies.txt -c cookies.txt -H "Content-Type: application/json" -H "X-CSRF-TOKEN: $($ownerToken.token)" --data-binary "@body.json" https://localhost:42110/api/orders/10000000-0000-0000-0000-000000000001 | ConvertFrom-Json
$firstUpdate

# The same old version again: someone else's save won. Expect 409 "Reload the order before retrying."
'{ "notes": "A stale edit.", "version": "a0000000-0000-0000-0000-000000000001" }' | Set-Content body.json
curl.exe -i -w "\n" -X PUT -b cookies.txt -c cookies.txt -H "Content-Type: application/json" -H "X-CSRF-TOKEN: $($ownerToken.token)" --data-binary "@body.json" https://localhost:42110/api/orders/10000000-0000-0000-0000-000000000001

# With the version returned by the first update. Expect 200
@"
{ "notes": "Ring the bell.", "version": "$($firstUpdate.version)" }
"@ | Set-Content body.json
curl.exe -i -w "\n" -X PUT -b cookies.txt -c cookies.txt -H "Content-Type: application/json" -H "X-CSRF-TOKEN: $($ownerToken.token)" --data-binary "@body.json" https://localhost:42110/api/orders/10000000-0000-0000-0000-000000000001

# Without the antiforgery token. Expect 400
'{ "notes": "No token.", "version": "a0000000-0000-0000-0000-000000000001" }' | Set-Content body.json
curl.exe -i -w "\n" -X PUT -b cookies.txt -c cookies.txt -H "Content-Type: application/json" --data-binary "@body.json" https://localhost:42110/api/orders/10000000-0000-0000-0000-000000000001

# Sign in as owner-2, who has one order. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt https://localhost:42110/dev/sign-in/owner-2

# The report shows only owner-2's order. Expect 200 with AW-0026 only
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42110/api/orders/report

# Expect 200
$otherToken = curl.exe -s -b cookies.txt -c cookies.txt https://localhost:42110/csrf | ConvertFrom-Json
$otherToken

# owner-2 cannot update owner-1's order: the ownership filter finds nothing. Expect 404
'{ "notes": "Not my order.", "version": "a0000000-0000-0000-0000-000000000001" }' | Set-Content body.json
curl.exe -i -w "\n" -X PUT -b cookies.txt -c cookies.txt -H "Content-Type: application/json" -H "X-CSRF-TOKEN: $($otherToken.token)" --data-binary "@body.json" https://localhost:42110/api/orders/10000000-0000-0000-0000-000000000001

```
