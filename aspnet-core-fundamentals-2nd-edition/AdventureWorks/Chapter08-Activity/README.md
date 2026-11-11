# AdventureWorks: Chapter 8 Activity solution

For the Visual Studio steps, use Visual Studio with support for the .NET 11 SDK and `.slnx` solutions.

This is a worked solution to the chapter's Activity, built in a copy of the Chapter 8 checkpoint. Try the Activity yourself first. The next chapter starts from `../Chapter08`, not from this folder.

It adds `LoyaltyEditor.cs`, a Development-only `PUT /dev/loyalty/{name}` that changes a stored loyalty number, so you can watch the claim change after you sign in again, exactly once.

`RefreshRace.ps1` sends two refreshes of the same refresh token at the same moment, five times, and prints each pair of statuses: one 200 and one 401, the request that lost at the [ConcurrencyCheck] on ConsumedByHash. Run it from this folder in Windows PowerShell 5.1 while the app runs (`.\RefreshRace.ps1`), or paste its lines into the window if scripts are disabled. If Training:Password differs from the script's default, pass the same value with `-Password`, for example `.\RefreshRace.ps1 -Password 'YourChosenPassword!12'`.

## Run it

From this folder, the one that holds global.json:

```powershell
dotnet user-secrets set FeatureFlags:MaxCartItems 30 `
    --project AdventureWorks
dotnet user-secrets set Training:Password "Practice-Pass-2026!" `
    --project AdventureWorks
dotnet run --project AdventureWorks
```

The two secrets are Chapter 2's harmless setting and Chapter 8's test-account password; set them once, because every later checkpoint keeps the same UserSecretsId. Any password with 12 or more characters, upper and lower case letters, a digit and a symbol works; requests.http uses this one. The first run creates adventureworks.db in the AdventureWorks folder and five test accounts: casey, jordan, morgan, riley and avery, each at @adventure-works.test. In Visual Studio, open AdventureWorks.slnx and press F5 instead. The launch profile in AdventureWorks/Properties/launchSettings.json starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Stop the app with Ctrl+C.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
Remove-Item cookies.txt -ErrorAction SilentlyContinue
$password = 'Practice-Pass-2026!'

# The token API checks the password and returns an access token
# and a refresh token. Expect 200
@"
{ "email": "casey@adventure-works.test",
  "password": "$password" }
"@ | Set-Content body.json
curl.exe -s -D - -o login.json -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/auth/login
$login = Get-Content login.json | ConvertFrom-Json

# A wrong password. Expect 401
@'
{ "email": "casey@adventure-works.test",
  "password": "not-the-password-1A!" }
'@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/auth/login

# The Bearer-only API with the access token. The claims
# transformation adds the stored loyalty number. Expect 200 with
# "AW-001"
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Authorization: Bearer $($login.accessToken)" `
    https://localhost:7001/api/account/loyalty

# The same API without a token. Expect 401
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/account/loyalty

# Rotate the refresh token: the old one is consumed. Expect 200
@"
{ "token": "$($login.refreshToken)" }
"@ | Set-Content body.json
curl.exe -s -D - -o rotated.json -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/auth/refresh
$rotated = Get-Content rotated.json | ConvertFrom-Json

# Replaying the consumed refresh token fails. Expect 401
@"
{ "token": "$($login.refreshToken)" }
"@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/auth/refresh

# The replacement works once. Expect 200
@"
{ "token": "$($rotated.refreshToken)" }
"@ | Set-Content body.json
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/api/auth/refresh

# An API that uses the cookie scheme answers 401 without a cookie;
# it does not redirect. Expect 401
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/account/me

# A page redirects to the sign-in page instead. Expect 302
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/Account/Profile

# The sign-in page. Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/Account/Login

# Development only: sign in casey with the real Identity cookie.
# Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/casey

# The cookie now reaches the profile page. Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/Account/Profile

# And the cookie API, with the loyalty number listed once.
# Expect 200 with "scheme": "Identity.Application"
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/account/me

# The Bearer-only API names its scheme, so the cookie alone is not
# enough. Expect 401
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/account/loyalty

# Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-out

# Signed out again. Expect 401
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/account/me

# Earlier chapters' routes still answer. Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/catalog

# Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/Products

# The Activity: sign casey in and read the claim. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/casey

# Expect 200 with {"name":"casey@adventure-works.test",
# "scheme":"Identity.Application","loyaltyNumbers":["AW-001"]}
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/account/me

# Change the stored loyalty number (Development only). Expect 204
'{ "number": "AW-901" }' | Set-Content body.json
curl.exe -i -w "\n" -X PUT -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/dev/loyalty/casey

# Sign out and in again. Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-out

# Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt `
    https://localhost:7001/dev/sign-in/casey

# The new value, exactly once. Expect 200 with
# {"name":"casey@adventure-works.test",
# "scheme":"Identity.Application","loyaltyNumbers":["AW-901"]}
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt `
    https://localhost:7001/api/account/me

# Put the original value back. Expect 204
'{ "number": "AW-001" }' | Set-Content body.json
curl.exe -i -w "\n" -X PUT -b cookies.txt -c cookies.txt `
    -H "Content-Type: application/json" `
    --data-binary "@body.json" `
    https://localhost:7001/dev/loyalty/casey
```
