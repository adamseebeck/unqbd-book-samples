# Chapter 9: Authorization and Security Fundamentals

The complete code for Chapter 9's Full Example, including the archive Activity endpoint, matching the listings in the book. Like every chapter from Chapter 7, this is its own small project: start from this folder rather than adding it to an earlier chapter's project.

## What differs from the book

DevSignIn.cs and the line `app.MapDevelopmentSignIn();` before `app.Run();` in Program.cs are the only additions to the book's listing. They are a local test double, like Chapter 15's TestAuthHandler. In Development only, a POST to /dev/sign-in/{user} signs in one of four fixed users with this host's real cookie handler, and a request cannot choose its own claims. Outside Development the route does not exist. POST /dev/sign-out clears the cookie.

| User | NameIdentifier | Claims | What it shows |
| --- | --- | --- | --- |
| finance-owner | owner-1 | Department Finance, born 1990 | the archive succeeds (204); the age policy passes |
| owner | owner-1 | no Department, under 21 | the Finance policy and the age policy fail (403) |
| finance-nonowner | owner-2 | Department Finance | the ownership check fails (403) |
| admin | admin-1 | role Admin | the delete succeeds (204) |

To use it in your own Chapter 9 project, copy DevSignIn.cs, add `app.MapDevelopmentSignIn();` before `app.Run();` and run in Development.

## Run it

Run `dotnet run` from this folder. Properties/launchSettings.json starts the app in Development at https://localhost:42109. The cookie is Secure, so use the HTTPS address. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Restart the app to reset the document store.

## Sending requests

`requests.http` holds every request this chapter checks, in order, each with the status you should see. Visual Studio 2026 runs it directly (open the file and select Send Request above a request); VS Code needs the REST Client extension.

The sign-in request sets this host's cookie. VS Code REST Client keeps it for the requests that follow. If your client does not keep cookies, use the curl.exe commands below, which save them in cookies.txt.

In Windows PowerShell, type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. The commands below are the same requests. Run them from any folder; JSON bodies go to body.json first because Windows PowerShell removes double quotes from arguments it passes to curl.exe.

```powershell
Remove-Item cookies.txt -ErrorAction SilentlyContinue

# Public page. Expect 200: "Public AdventureWorks documentation"
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42109/

# Anonymous request to a protected document. Expect 401
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42109/api/documents/1

# Anonymous archive request. Expect 401
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt https://localhost:42109/api/documents/1/archive

# Sign in as owner: owner-1, no Department, under 21. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt https://localhost:42109/dev/sign-in/owner

# The age policy rejects this user. Expect 403
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42109/restricted-content

# The owner can read the document. Expect 200 with "archived": false
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42109/api/documents/1

# Antiforgery token for this user. Expect 200
$ownerToken = curl.exe -s -b cookies.txt -c cookies.txt https://localhost:42109/csrf | ConvertFrom-Json
$ownerToken

# Not in Finance, so the Department:Finance policy rejects the archive. Expect 403
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt -H "X-CSRF-TOKEN: $($ownerToken.token)" https://localhost:42109/api/documents/1/archive

# Sign in as finance-nonowner: owner-2, Department Finance. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt https://localhost:42109/dev/sign-in/finance-nonowner

# Expect 200
$nonOwnerToken = curl.exe -s -b cookies.txt -c cookies.txt https://localhost:42109/csrf | ConvertFrom-Json
$nonOwnerToken

# In Finance but not the owner, so the ownership check rejects it. Expect 403
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt -H "X-CSRF-TOKEN: $($nonOwnerToken.token)" https://localhost:42109/api/documents/1/archive

# Sign in as finance-owner: owner-1, Department Finance, born 1990. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt https://localhost:42109/dev/sign-in/finance-owner

# The age policy accepts this user. Expect 200: "Age policy accepted"
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42109/restricted-content

# Expect 200
$financeOwnerToken = curl.exe -s -b cookies.txt -c cookies.txt https://localhost:42109/csrf | ConvertFrom-Json
$financeOwnerToken

# Without the antiforgery token the archive is rejected. Expect 400
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt https://localhost:42109/api/documents/1/archive

# Finance membership and ownership both succeed. Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt -H "X-CSRF-TOKEN: $($financeOwnerToken.token)" https://localhost:42109/api/documents/1/archive

# The document is now archived. Expect 200 with "archived": true
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42109/api/documents/1

# Sign in as admin: role Admin. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt https://localhost:42109/dev/sign-in/admin

# Expect 200
$adminToken = curl.exe -s -b cookies.txt -c cookies.txt https://localhost:42109/csrf | ConvertFrom-Json
$adminToken

# Admins can delete. Expect 204
curl.exe -i -w "\n" -X DELETE -b cookies.txt -c cookies.txt -H "X-CSRF-TOKEN: $($adminToken.token)" https://localhost:42109/api/documents/1

# The document is gone. Expect 404
curl.exe -i -w "\n" -X DELETE -b cookies.txt -c cookies.txt -H "X-CSRF-TOKEN: $($adminToken.token)" https://localhost:42109/api/documents/1

# Unknown test users do not exist. Expect 404
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt https://localhost:42109/dev/sign-in/someone-else

# Sign out. Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt https://localhost:42109/dev/sign-out

```
