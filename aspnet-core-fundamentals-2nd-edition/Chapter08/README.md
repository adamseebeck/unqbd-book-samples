# Chapter 8: Authentication and Identity

The complete code for Chapter 8's Full Example, matching the listings in the book. Like every chapter from Chapter 7, this is its own small project: start from this folder rather than adding it to an earlier chapter's project.

## Run it

The example creates a disposable local training database, seeds the account learner@example.test and refuses to start outside Development. Set a temporary password for that account in the same terminal, then start the app:

```powershell
$env:Training__Password = 'Learner-Pass-2026!'
dotnet run
```

The password needs at least 12 characters with an uppercase letter, a lowercase letter, a digit and a symbol. requests.http uses the same value; change both together. Properties/launchSettings.json starts the app in Development at https://localhost:42108. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Open https://localhost:42108/Account/Login to sign in with the Identity cookie; the API routes below use Bearer tokens instead.

## Sending requests

`requests.http` holds every request this chapter checks, in order, each with the status you should see. Visual Studio 2026 runs it directly (open the file and select Send Request above a request); VS Code needs the REST Client extension.

In Windows PowerShell, type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. The commands below are the same requests. Run them from any folder; JSON bodies go to body.json first because Windows PowerShell removes double quotes from arguments it passes to curl.exe.

```powershell
# The API requires a Bearer token. Expect 401
curl.exe -i -w "\n" https://localhost:42108/api/books/available

# A wrong password. Expect 401
'{ "email": "learner@example.test", "password": "not-the-password" }' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" --data-binary "@body.json" https://localhost:42108/api/auth/login

# Sign in for tokens. Expect 200 with accessToken and refreshToken
'{ "email": "learner@example.test", "password": "Learner-Pass-2026!" }' | Set-Content body.json
$login = curl.exe -s -H "Content-Type: application/json" --data-binary "@body.json" https://localhost:42108/api/auth/login | ConvertFrom-Json
$login

# Call the API with the access token. Expect 200
curl.exe -i -w "\n" -H "Authorization: Bearer $($login.accessToken)" https://localhost:42108/api/books/available

# Exchange the refresh token for a new pair. Expect 200
@"
{ "token": "$($login.refreshToken)" }
"@ | Set-Content body.json
$refreshed = curl.exe -s -H "Content-Type: application/json" --data-binary "@body.json" https://localhost:42108/api/auth/refresh | ConvertFrom-Json
$refreshed

# The first refresh token is now consumed. Expect 401
@"
{ "token": "$($login.refreshToken)" }
"@ | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: application/json" --data-binary "@body.json" https://localhost:42108/api/auth/refresh

# The new access token works. Expect 200
curl.exe -i -w "\n" -H "Authorization: Bearer $($refreshed.accessToken)" https://localhost:42108/api/books/available

```
