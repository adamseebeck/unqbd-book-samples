# Chapter 12: Caching, Rate Limiting, and Resilience

The complete code for Chapter 12's Full Example, matching the listings in the book. Like every chapter from Chapter 7, this is its own small project: start from this folder rather than adding it to an earlier chapter's project.

## What differs from the book

- DevSignIn.cs and the line `app.MapDevelopmentSignIn();` before `app.Run();`: in Development only, POST /dev/sign-in/admin signs in an Admin test user for the protected cache-invalidation webhook.
- InventoryFixture/ is the loopback inventory service InventoryClient calls (http://127.0.0.1:42132, the client's default address). GET /stock/1 returns {"quantity":7}. Its control routes queue failures (POST /control/fail/{count}), delay every response (POST /control/delay/{milliseconds}), return one status until reset (POST /control/status/{code}), report how many stock requests arrived (GET /control/attempts) and reset (POST /control/reset). Chapter12.csproj has one extra line so it does not compile the fixture's files.

## Run it

Use two terminals. In the first, run `dotnet run` in the InventoryFixture folder. In the second, run `dotnet run` in this folder; Properties/launchSettings.json starts it in Development at https://localhost:42112. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Without a Redis connection string the cache uses memory; set ConnectionStrings__Redis to a disposable local server for the shared-cache steps.

## Sending requests

`requests.http` holds every request this chapter checks, in order, each with the status you should see. Visual Studio 2026 runs it directly (open the file and select Send Request above a request); VS Code needs the REST Client extension.

The sign-in request sets this host's cookie. VS Code REST Client keeps it for the requests that follow. If your client does not keep cookies, use the curl.exe commands below, which save them in cookies.txt.

In Windows PowerShell, type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. The commands below are the same requests. Run them from any folder; JSON bodies go to body.json first because Windows PowerShell removes double quotes from arguments it passes to curl.exe. The resilience checks wait between steps because the circuit breaker counts failures over a 10-second window.

```powershell
Remove-Item cookies.txt -ErrorAction SilentlyContinue

# The first read fills the cache. Expect 200 with "generation": 1
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/api/products/1

# A cache hit returns the same generation. Expect 200 with "generation": 1
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/api/products/1

# Only product 1 exists. Expect 404
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/api/products/2

# The "public" limiter allows 3 requests a minute per client (wait a minute before you repeat these). Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/limited

# Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/limited

# Expect 200
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/limited

# The fourth request in the same minute is rejected. Expect 429
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/limited

# Resilience checks. The circuit breaker counts failures over a 10-second window, so the checks below say when to wait; without the waits, failures from one check carry into the next. A healthy upstream. Expect 200 {"quantity":7}
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/api/stock

# Retry: reset the fixture, then queue two failures (500). Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt http://127.0.0.1:42132/control/reset

# Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt http://127.0.0.1:42132/control/fail/2

# Two retries absorb the failures. Expect 200 {"quantity":7}
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/api/stock

# Three attempts reached the fixture. Expect 200 {"attempts":3}
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt http://127.0.0.1:42132/control/attempts

# No retry for a client error. Wait 15 seconds, then reset. Expect 204
Start-Sleep -Seconds 15
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt http://127.0.0.1:42132/control/reset

# A permanent 400. Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt http://127.0.0.1:42132/control/status/400

# The 400 is not transient, so the call fails at once. Expect 503
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/api/stock

# One attempt only. Expect 200 {"attempts":1}
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt http://127.0.0.1:42132/control/attempts

# Timeouts. Wait 15 seconds, then reset. Expect 204
Start-Sleep -Seconds 15
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt http://127.0.0.1:42132/control/reset

# Delay every response past the 1-second attempt timeout. Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt http://127.0.0.1:42132/control/delay/1500

# Each attempt times out and is retried. Expect 503 after about 3 seconds
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/api/stock

# Three attempts reached the fixture. Expect 200 {"attempts":3}
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt http://127.0.0.1:42132/control/attempts

# Circuit breaker. Wait 15 seconds, then reset. Expect 204
Start-Sleep -Seconds 15
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt http://127.0.0.1:42132/control/reset

# Keep failing. Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt http://127.0.0.1:42132/control/fail/1000

# Three failed attempts. Expect 503
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/api/stock

# Two more failed attempts reach MinimumThroughput (5) and open the breaker. Expect 503
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/api/stock

# The open breaker rejects the call without reaching the fixture. Expect 503
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/api/stock

# The count stopped at 5 while the breaker was open. Expect 200 {"attempts":5}
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt http://127.0.0.1:42132/control/attempts

# Clear the failures. The breaker closes again after its 5-second break. Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt http://127.0.0.1:42132/control/reset

# Sign in as the Admin test user. Expect 200
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt https://localhost:42112/dev/sign-in/admin

# Expect 200 {"name":"Admin"}; authenticated responses are not output-cached
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/profile

# Antiforgery token for this user. Expect 200
$adminToken = curl.exe -s -b cookies.txt -c cookies.txt https://localhost:42112/csrf | ConvertFrom-Json
$adminToken

# Invalidate the product cache as Admin. Expect 204
curl.exe -i -w "\n" -X POST -b cookies.txt -c cookies.txt -H "X-CSRF-TOKEN: $($adminToken.token)" https://localhost:42112/api/inventory/webhook

# The next read rebuilds the entry. Expect 200 with "generation": 2
curl.exe -i -w "\n" -b cookies.txt -c cookies.txt https://localhost:42112/api/products/1

```
