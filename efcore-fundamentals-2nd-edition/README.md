# Entity Framework Core Fundamentals, Second Edition: companion code

Companion code for *Entity Framework Core Fundamentals, Second Edition* by Adam Seebeck. The book builds one application, **Retail**, from Chapter 1 to Chapter 16 with .NET 11 and EF Core 11. Each `ChapterNN` folder holds the Retail project as it stands at the end of that chapter.

## What you need

- The .NET 11 SDK, version `11.0.100` or later. `dotnet --version` should print it. `global.json` in this folder accepts any later .NET 11 SDK.
- SQL Server Express LocalDB on Windows. Visual Studio 2026 installs it with the ASP.NET and web development workload; with VS Code, install SQL Server Express LocalDB from Microsoft's SQL Server downloads. Run `SqlLocalDB info` in a terminal; the list should include `MSSQLLocalDB`.
- Visual Studio 2026 is the main Windows path. VS Code with C# Dev Kit and its terminal is the alternative.

The EF Core packages and the local `dotnet-ef` tool use version `11.0.0`. The tool is listed in `.config/dotnet-tools.json`; run `dotnet tool restore` once in this folder.

## Open and run in Visual Studio

1. Choose File > Open > Project/Solution and open `ChapterNN/EfCoreBook.slnx`. Open only the checkpoint you are using.
2. Set Retail as the startup project and build with Ctrl+Shift+B. From Chapter 12 on, the same solution includes Retail.Tests.
3. In Chapters 1-9, Ctrl+F5 runs the chapter's demonstration. From Chapter 10 on, an empty application argument box starts the web API instead.
4. Choose Debug > Retail Debug Properties to edit the `Retail` profile. Enter `--lab N` to run a lab, or append `--ShowSql true` to print commands. Enter application arguments only, without `dotnet run --`. There is no lab 12 because that chapter adds tests.
5. Use F9 to set a breakpoint and F5 to run under the debugger; F10 steps over a statement and Shift+F5 stops the debug session. Chapters 1, 2, 5 and 8 explain what to inspect and what debugger evaluation can change.
6. Use Test > Test Explorer to run or debug tests. A CLI build alone is not a test run.

For migrations, use View > Terminal and check that PowerShell is in Retail beside Retail.csproj. Run `dotnet tool restore` once, then the book's pinned `dotnet ef` commands. This terminal is distinct from NuGet's Package Manager Console.

Every Activities/ChapterNN folder also has an EfCoreBook.slnx for the finished exercise. Keep your own exercises in separate Activities/ChapterNN folders and preserve earlier attempts. Activity databases use EfCoreBook_ActivityNN names, and Chapter 12 copies both app and test projects. The Experiments folders have solutions of their own.

## How the folders work

```text
global.json                 SDK version for every chapter
.config/dotnet-tools.json   the dotnet-ef tool (Chapter 4 on)
Chapter01/EfCoreBook.slnx   open the checkpoint in Visual Studio
Chapter01/Retail            the project at the end of Chapter 1
...
Chapter12/Retail            the project at the end of Chapter 12
Chapter12/Retail.Tests      its tests (Chapter 12 on)
...
Chapter16/Retail            the finished project
Activities/ChapterNN        solutions to each chapter's Activity
Experiments                 a SQLite comparison and a complex-type check
```

To start a chapter from here instead of your own project, copy the **previous** chapter's folder: for Chapter 7, copy `Chapter06`. To see a chapter's finished code, open its own folder.

Run a checkpoint from its project folder:

```powershell
cd Chapter06/Retail
dotnet run
dotnet run -- --lab 5
dotnet run -- --ShowSql true
```

Each chapter adds a lab, a short class in `Labs/` that exercises what the chapter built. In Chapters 2 to 9 `dotnet run` runs the newest lab and `--lab N` reruns an earlier one. From Chapter 10 the project is a web app: `dotnet run` serves it on http://127.0.0.1:5072 and `dotnet run -- --lab 11` starts it, runs that lab and stops. `--ShowSql true` (Chapter 6 on) prints every SQL command the app sends.

Tests (Chapter 12 on) run from the chapter folder:

```powershell
cd Chapter12
dotnet test Retail.Tests
```

## The database

Every checkpoint uses one LocalDB database, `EfCoreBook_Retail`, named in `Retail/appsettings.Development.json`. From Chapter 3, labs other than Chapter 4 drop and rebuild that database (using migrations from Chapter 4 onward), so every run starts from the same data; Chapter 4 applies migrations to the database as it stands. Chapter 15's lab also drops and recreates a second database, `EfCoreBook_Deploy`, to test its deployment script. The database stays after a run so you can open it in Visual Studio's SQL Server Object Explorer. The reset refuses any server other than LocalDB and any database whose name does not start with `EfCoreBook_`. Reserve all EfCoreBook_ names for disposable book data: the prefix guard cannot tell whether a database contains valuable work. Tests create their own `EfCoreBook_Test_...` database per test and drop it.

The migration history grows with the project: `InitialRetail` (Chapter 4), `AddPlacementsAndProfiles` (Chapter 7), `AddProductRowVersion` (Chapter 8) and `AddAddressPaymentsAndAudit` (Chapter 14). Chapter 15 deploys that history with an idempotent script.

Re-running an earlier lab preserves that chapter's demonstration. In particular, Chapter 13's bulk update bypasses `SaveChanges` and does not populate the audit stamp introduced in Chapter 14. Chapter 14 explains this limitation and its activity shows how to set `LastModifiedUtc` in the same bulk statement. Do not assume a `SaveChanges` audit hook applies to direct bulk updates.

## If something goes wrong

- **"Unable to retrieve project metadata"** from a `dotnet ef` command: the project has not been restored yet. Run `dotnet build`, then the command again.
- **"Set ConnectionStrings:DefaultConnection."**: the app did not start in the Development environment, so `appsettings.Development.json` was not read. Use the Retail launch profile in Visual Studio, or `dotnet run` from Retail. From Chapter10, keep both DOTNET_ENVIRONMENT and ASPNETCORE_ENVIRONMENT set to Development for local work.
- **A connection error naming the Local Database Runtime**: LocalDB is not installed or not started. Check `SqlLocalDB info`. On macOS and Linux the error is "LocalDB is not supported on this platform": LocalDB runs only on Windows.
- **"There is already an object named 'Categories' in the database"** in Chapter 4: an earlier checkpoint created `EfCoreBook_Retail` with `EnsureCreated`, which records no migration history. In Chapter04/Retail run `dotnet ef database drop --force`, or `dotnet run -- --lab 3` to rebuild it with migrations, then run Chapter 4 again.
- **Warnings NU1902 or NU1903 about System.Security.Cryptography.Pkcs**: the SQL Server package brings in version 9.0.18, which has known vulnerabilities. Chapters 1 to 9 and the test project add version 9.0.21 to replace it. The web SDK used from Chapter 10 already includes a patched copy.

## Notes

The projects keep NuGet's normal vulnerability auditing on. When you update packages, check the whole dependency graph again.

`Experiments/ProviderComparison` runs the same kind of concurrency check against SQLite with an application-managed token, to show how a different provider behaves. `Experiments/ComplexTypes` tries EF Core 11 complex types against SQL Server (Chapter 14's activity).

The API examples bind to the loopback address and are for learning. A deployed service also needs authentication, authorization, HTTPS, protected configuration and a separate migration process (Chapter 15).
