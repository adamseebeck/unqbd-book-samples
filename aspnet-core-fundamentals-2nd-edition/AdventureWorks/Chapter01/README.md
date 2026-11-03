# AdventureWorks: Chapter 1 checkpoint

The AdventureWorks store app as it stands at the end of Chapter 1 of *ASP.NET Core Fundamentals, Second Edition*: a .NET 11 web host with a status endpoint, a health check and request timing. Chapter 2 starts from this folder.

The `AdventureWorks` folder is the web project, and `AdventureWorks.slnx` is the solution that opens it in Visual Studio 2026. `global.json` selects the .NET 11 SDK (11.0.100 or later) for this folder and everything below it.

## Run it

From this folder, the one that holds global.json:

```shell
dotnet run --project AdventureWorks
```

In Visual Studio 2026, open AdventureWorks.slnx and press F5 instead. The launch profile in AdventureWorks/Properties/launchSettings.json starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. Stop the app with Ctrl+C.

## Sending requests

`requests.http` lists every request this checkpoint answers, in order, with the status you should see. Visual Studio 2026 runs it directly: open the file and select **Send Request** above a request. VS Code needs the REST Client extension.

The same requests as curl.exe commands for Windows PowerShell 5.1 follow. Type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. A line that ends in a backtick (`` ` ``) continues on the next line. JSON bodies go to body.json first, because Windows PowerShell removes double quotes from the arguments it passes to curl.exe. Run the commands in order, from any folder, while the app is running.

```powershell
# The root redirects to the status endpoint. Expect 302
curl.exe -i -w "\n" https://localhost:7001/

# The status endpoint. Expect 200 with "status": "Operational"
curl.exe -i -w "\n" https://localhost:7001/api/status

# The health endpoint answers Healthy. Expect 200
curl.exe -i -w "\n" https://localhost:7001/health

# Plain HTTP is redirected to HTTPS. Expect 307
curl.exe -i -w "\n" http://localhost:5001/api/status

# Nothing is mapped here. Expect 404
curl.exe -i -w "\n" https://localhost:7001/missing
```
