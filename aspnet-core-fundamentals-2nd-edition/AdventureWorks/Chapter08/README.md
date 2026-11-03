# AdventureWorks: Chapter 8 checkpoint

The AdventureWorks store app at the end of Chapter 8. It adds ASP.NET Core Identity on a SQLite database (`AdventureWorksDbContext`, the app's one context, in Infrastructure), sign-in pages at `/Account/Login` and `/Account/Profile`, a token API for a mobile client (`POST /api/auth/login` and `POST /api/auth/refresh`, with hashed, single-use refresh tokens), a Bearer-only loyalty API, a claims transformation that adds each customer's stored loyalty number, and Development-only test accounts with a test sign-in (`DevSignIn.cs`). Chapter 9 starts from this folder.

Identity's cookie is the only cookie scheme. Pages redirect to the sign-in page; every `/api` route answers 401 or 403 instead.

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
```
