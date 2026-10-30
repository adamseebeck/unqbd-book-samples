# ASP.NET Core Fundamentals, Second Edition: companion code

Code for *ASP.NET Core Fundamentals: Building Modern Web Applications with .NET 11* by Adam Seebeck (unQbd). Each chapter folder holds that chapter's complete example, plus the worked activity solutions the chapters mention.

You can read and copy any file here in your browser. To run the projects, clone this repository or download it as a ZIP (the green Code button on GitHub).

## What you need

- The .NET 11 SDK, version 11.0.100 or later, from the [official .NET 11 download page](https://dotnet.microsoft.com/download/dotnet/11.0). The `global.json` files select it.
- Projects target `net11.0` and list their exact package versions. Microsoft framework packages use 11.0.0; other libraries follow their own version numbers.
- A few exercises need more: an isolated SQL Server instance for Chapter 10's SQL Server steps, a Redis-compatible server for Chapter 12's shared cache, a Linux container engine for Chapter 18's images, a native compiler for Native AOT, and a browser passkey authenticator only if you try passkeys beyond Chapter 8's password example. The chapter tells you when.

## Run an example

```shell
cd Chapter01/FullExample
dotnet --version
dotnet run
```

`dotnet --version` should print 11.0.100 or a later 11.0 version. `dotnet run` uses the folder's launch profile (Properties/launchSettings.json, the one Chapter 1 builds) and prints the addresses after "Now listening on": https://localhost:7001 and http://localhost:5001. If the browser warns about the certificate, run `dotnet dev-certs https --trust` once. Each chapter README gives its own run command.

## Layout

| Chapters | Folders |
| --- | --- |
| 1-6 | `ChapterNN/FullExample` and `ChapterNN/Activity`. Chapter 2 keeps its separate Core, Infrastructure and Web projects. |
| 7-12 | `ChapterNN` holds each chapter's complete checkpoint and a README. Chapters 9 to 12 add a Development-only test sign-in (`DevSignIn.cs`), Chapters 10 and 11 add sample data (`DevSeed.cs`), and Chapter 12 adds `InventoryFixture`, the local inventory service. |
| 13-18 | `ChapterNN` holds the chapter's projects, plus any `Activity`, `Tests`, `Benchmarks` or `Checks` projects the chapter uses. |

Each folder focuses on one chapter. Follow the chapter's steps rather than merging every checkpoint into one project. The in-memory stores and test authentication handlers exist for local learning; they are not production persistence or authentication.

## Sending requests

A browser address bar sends only GET requests. Most chapters from Chapter 3 on also need POST, PUT and DELETE requests and custom headers, so these folders have a `requests.http` file that lists each request in order with the status you should see: `Chapter03/Activity`, `Chapter05/FullExample`, `Chapter07` to `Chapter13`, `Chapter16` and `Chapter17/Api`.

- **Visual Studio 2026:** open `requests.http` and select **Send Request** above a request.
- **VS Code:** install the REST Client extension, then select **Send Request**.
- **Any terminal:** each of those READMEs has the same requests as `curl.exe` commands. In Windows PowerShell, type `curl.exe`, not `curl`; in Windows PowerShell 5.1, `curl` is a different command.

Chapters 9 to 12 protect their routes with a sign-in cookie, but the book's examples have no login page. Those folders add `DevSignIn.cs`, which signs in fixed test users in Development only, with the host's real cookie handler. Each README shows the one or two lines it adds to Program.cs; nothing else differs from the book's listings.

## Corrections

These passages changed shortly before publication. If your copy reads differently, follow this list.

- **About This Book:** Chapters 1 to 6 extend one AdventureWorks web project, and each Full Example's Program.cs replaces the previous one, so earlier routes are not carried forward. From Chapter 7, each chapter starts its own small project in its own folder.
- **Chapter 1:** the launch profile is named `https` (Step 5), and the UseUrls example uses ports 5001 (HTTP) and 7001 (HTTPS). Run the Full Example with F5 or `dotnet run --launch-profile https`; trust the local certificate with `dotnet dev-certs https --trust`. / redirects to /api/status, which returns `{"status":"Operational","timestamp":"..."}`, and /health returns Healthy. Step 4 replaces everything after `var app = builder.Build();`. In Visual Studio, set Location to the folder that holds global.json. Download the SDK from dotnet.microsoft.com/download/dotnet/11.0.
- **Chapter 2:** the Concept Overview defines dependency injection. Step 1 creates the class libraries (`dotnet new classlib -n AdventureWorks.Core -f net11.0`, and the same for AdventureWorks.Infrastructure). The Full Example has no / route, so the browser shows 404 there; /api/config shows maxCartItems 30 from User Secrets. The Activity starts with `dotnet run -- --PaymentOptions:TimeoutSeconds=50`.
- **Chapter 3:** the Step 1 demonstration prints A and a, then B and b, so a normal request prints ABba, as Figure 3.1 shows. The Full Example's Program.cs replaces Chapter 2's. The Activity's POST requests are in `Chapter03/Activity/requests.http`.
- **Chapter 4:** an OpenAPI document is a JSON description of the endpoints, served at /openapi/v1.json. Run `dotnet add package` from the folder that contains AdventureWorks.csproj. The chapter has no tests; follow ReviewsLink to check a generated link. The endpoint log uses LogInformation, so it appears at the default level.
- **Chapter 5:** the Full Example's GET, POST, PUT and DELETE requests are in `Chapter05/FullExample/requests.http`.
- **Chapter 6:** its Program.cs replaces Chapter 5's, and ProductStore takes the place of ProductService.
- **Chapter 9:** sign in each test user with `DevSignIn.cs` (Prerequisites, Full Example, Activity).
- **Chapter 10:** add OrderQueries.cs (Step 3) and the Full Example's Program.cs before `dotnet ef migrations add InitialCreate`; without the AddDbContext registration the tools cannot create ApplicationDbContext. Both the synchronous and asynchronous save paths renew the version tokens. Figure 10.1's caption refers to the repositories Chapter 11 adds. Local requests use `DevSignIn.cs` and `DevSeed.cs`.
- **Chapter 11:** local requests use `DevSignIn.cs` and `DevSeed.cs`.
- **Chapter 12:** the Admin test user comes from `DevSignIn.cs`, and `InventoryFixture` is the loopback inventory service for the retry, timeout and circuit-breaker steps.
- **Chapter 17:** create the benchmark project with `dotnet new console -n AdventureWorks.Benchmarks -o Benchmarks -f net11.0`, so `--project Benchmarks` finds it.
- **Chapter 18:** the build command is `docker build -t adventureworks:local .` (the final dot is the build context). For forwarded headers, configure ForwardedHeadersOptions with your trusted proxies or networks and call UseForwardedHeaders before middleware that reads the scheme or client address.
- **Wording:** Chapters 2, 5, 6, 7, 15 and 18 no longer promise that later chapters upgrade one reference application, and a few repeated caveats in Chapters 1 to 4 are gone.

## About the book

The book explains every example and the decisions behind it. More programming books are at [unqbd.com](https://www.unqbd.com).
