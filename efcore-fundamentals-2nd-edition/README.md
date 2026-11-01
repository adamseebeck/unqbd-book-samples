# Entity Framework Core Fundamentals, Second Edition: companion code

Companion code for *Entity Framework Core Fundamentals, Second Edition* by Adam Seebeck. The book builds one application, **Retail**, from Chapter 1 to Chapter 16 with .NET 11 and EF Core 11. Each `ChapterNN` folder holds the Retail project as it stands at the end of that chapter.

## What you need

- The .NET 11 SDK, version `11.0.100` or later. `dotnet --version` should print it. `global.json` in this folder accepts any later .NET 11 SDK.
- SQL Server Express LocalDB on Windows. Visual Studio 2026 installs it with the ASP.NET and web development workload; with VS Code, install SQL Server Express LocalDB from Microsoft's SQL Server downloads. Run `SqlLocalDB info` in a terminal; the list should include `MSSQLLocalDB`.
- Visual Studio 2026 or VS Code with C# Dev Kit.

The EF Core packages and the local `dotnet-ef` tool use version `11.0.0`. The tool is listed in `.config/dotnet-tools.json`; run `dotnet tool restore` once in this folder.

## How the folders work

```text
global.json                 SDK version for every chapter
.config/dotnet-tools.json   the dotnet-ef tool (Chapter 4 on)
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

Every checkpoint uses one LocalDB database, `EfCoreBook_Retail`, named in `Retail/appsettings.Development.json`. From Chapter 3 each lab starts by dropping and rebuilding that database (from Chapter 4, from the migrations), so every run starts from the same data. The database stays after a run so you can open it in Visual Studio's SQL Server Object Explorer. The reset refuses any server other than LocalDB and any database whose name does not start with `EfCoreBook_`. Tests create their own `EfCoreBook_Test_...` database per test and drop it.

The migration history grows with the project: `InitialRetail` (Chapter 4), `AddPlacementsAndProfiles` (Chapter 7), `AddProductRowVersion` (Chapter 8) and `AddAddressPaymentsAndAudit` (Chapter 14). Chapter 15 deploys that history with an idempotent script.

Re-running an earlier lab preserves that chapter's demonstration. In particular, Chapter 13's bulk update bypasses `SaveChanges` and does not populate the audit stamp introduced in Chapter 14. Chapter 14 explains this limitation and its activity shows how to set `LastModifiedUtc` in the same bulk statement. Do not assume a `SaveChanges` audit hook applies to direct bulk updates.

## If something goes wrong

- **"Unable to retrieve project metadata"** from a `dotnet ef` command: the project has not been restored yet. Run `dotnet build`, then the command again.
- **"Set ConnectionStrings:DefaultConnection."**: the app did not start in the Development environment, so `appsettings.Development.json` was not read. Use `dotnet run` (which applies `Properties/launchSettings.json`) from the `Retail` folder.
- **A connection error naming the Local Database Runtime**: LocalDB is not installed or not started. Check `SqlLocalDB info`.
- **Warnings NU1902 or NU1903 about System.Security.Cryptography.Pkcs**: the SQL Server package brings in version 9.0.18, which has known vulnerabilities. Chapters 1 to 9 and the test project add version 9.0.21 to replace it. The web SDK used from Chapter 10 already includes a patched copy.

## Notes

The projects keep NuGet's normal vulnerability auditing on. When you update packages, check the whole dependency graph again.

`Experiments/ProviderComparison` runs the same kind of concurrency check against SQLite with an application-managed token, to show how a different provider behaves. `Experiments/ComplexTypes` tries EF Core 11 complex types against SQL Server (Chapter 14's activity).

The API examples bind to the loopback address and are for learning. A deployed service also needs authentication, authorization, HTTPS, protected configuration and a separate migration process (Chapter 15).
