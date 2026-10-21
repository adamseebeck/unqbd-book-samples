# Entity Framework Core Fundamentals, Second Edition: companion code

Companion code for *Entity Framework Core Fundamentals, Second Edition* by Adam Seebeck: one retail domain built up across 16 chapters with .NET 11 and EF Core 11. Run commands from this folder so `global.json` and the local tool manifest apply.

Install the .NET 11 SDK, version `11.0.100` or later, and verify it with `dotnet --version`. `global.json` accepts later .NET 11 SDKs. Microsoft EF Core packages and the local `dotnet-ef` tool use `11.0.0`. You do not need to change your machine's default SDK.

```powershell
dotnet tool restore
dotnet ef --version
dotnet run --project Chapter01
dotnet run --project Chapter03
dotnet run --project Activities/Chapter03
```

The local `dotnet-ef` tool needs a compatible .NET runtime in addition to the SDK. If `dotnet ef --version` reports a missing runtime, install the version it names. No machine-wide runtime setting is required.

The `ChapterNN` directories are complete replacement checkpoints of the same retail domain. Their duplicate type names are intentional. Do not combine their Program.cs files in one project. Activity directories are independent solutions; an activity's extra supplier or description does not silently become a prerequisite for the next main chapter.

SQL Server Express LocalDB on Windows is the primary runtime prerequisite. Confirm the instance with `SqlLocalDB info`. The default is `(localdb)\MSSQLLocalDB`; `EFCORE_BOOK_LOCALDB` may name another already available LocalDB instance. A lab creates its own `EfCoreBook_ChNN_` database with a random suffix and removes that database on normal disposal. Do not modify its connection guard or turn it into cleanup for another database.

Chapters 1 and 2 inspect SQL or web configuration without initializing a database. Supply the chapter 2 setting to the current process, then run its host:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=(localdb)\MSSQLLocalDB;Database=EfCoreBook_Config;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
dotnet run --project Chapter02 -- --urls http://127.0.0.1:50702
```

Request `/configuration`, then stop that local process. `Activities/Chapter02` starts its own bounded loopback host and checks staging retry selection, the provider response and missing configuration. It contacts no staging service.

For the missing-setting check, remove the connection setting from every active configuration source. Removing only an environment variable still leaves appsettings.Development.json available under Development. The supplied separate-process check uses a dedicated environment with no matching settings file and removes inherited connection environment keys.

Chapters 10 and 11 accept a bounded HTTP smoke mode:

```powershell
dotnet run --project Chapter10 -- --check
dotnet run --project Chapter11 -- --check
dotnet run --project Activities/Chapter11
dotnet run --project Activities/Chapter12
```

The API activities exercise actual loopback HTTP and fresh-context persistence checks. They bind only to loopback and create disposable lab databases. They are educational hosts. A deployed service needs authentication, per-resource authorization, TLS, protected configuration, a migration process and other operational policies.

Testing/XunitSqlServer wraps the same real database model in four xUnit v2 integration tests. Each test-class instance implements IAsyncLifetime and owns a separate disposable LocalDB database. Run dotnet test Testing/XunitSqlServer for a standard test-runner report. The executable Chapter12 checkpoint has no test-runner dependency; these are complementary entry points for the same persistence rules.

## Migration checkpoints

Chapters 4 and 15, and activities 4, 7 and 15, use an operator-controlled migration database. They require an integrated-authentication LocalDB connection whose database begins `EfCoreBook_Migrations_`. Use a fresh database for each independent checkpoint. These projects never infer that a missing connection means permission to use a default shared database.

```powershell
$env:EFCORE_BOOK_MIGRATION_CONNECTION = 'Server=(localdb)\MSSQLLocalDB;Database=EfCoreBook_Migrations_Chapter04;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
dotnet ef migrations add InitialRetail --project Chapter04 --startup-project Chapter04
dotnet ef migrations script --idempotent --project Chapter04 --startup-project Chapter04 --output Chapter04/retail-idempotent.sql
dotnet run --project Chapter04
```

The generated migrations and snapshots included here are the inputs for each checkpoint. Inspect them and run the checkpoint; do not generate a second InitialRetail with the same role. Activity 4 adds Description after its initial snapshot, activity 7 adds Supplier after the relationship snapshot, and activity 15 adds ExternalReference after the final model snapshot.

For Activity15, run the expanded model first, then run `dotnet run --project Testing/PriorModelCompatibility` with that same `EFCORE_BOOK_MIGRATION_CONNECTION`. The compatibility project compiles the entire unchanged `Chapter15/Program.cs` through a source link. It reads the original product fields, rowversion, query filter, category/placement relationships, owned address, customer profile, order lines and converted TPH payment against the expanded schema. It neither recreates a reduced legacy model nor applies migrations.

Keep the migration databases until you finish the exercise, then remove only the dedicated database you created for that lab. Do not call EnsureCreated and Migrate on the same database. A Down method does not recover data dropped with a column.

## Provider experiments

`Experiments/ProviderComparison` uses the SQLite relational provider with an open in-memory SQLite connection and an application-managed Guid token. Both synchronous and asynchronous save overloads share token stamping. Its competing-writer check uses a synchronous first save and an asynchronous stale save. It is not EF's InMemory provider, and it does not establish SQL Server rowversion, SQL dialect or collation behavior.

`Experiments/ComplexTypes` checks EF Core 11 complex-member configuration against the SQL Server provider. Documentation can change between releases; the program's output shows the behavior of the packages you installed.

## Notes on packages and tests

The projects keep normal NuGet advisory auditing. A successful restore or build alone is not a vulnerability audit.

The console Design/test dependency graphs explicitly pin `System.Security.Cryptography.Pkcs` to `9.0.21`. This replaces the transitive `9.0.18` flagged by Microsoft's [October 13 high-severity advisory](https://github.com/advisories/GHSA-846f-f764-76mx) and [related moderate-severity advisory](https://github.com/advisories/GHSA-2fmv-rv2c-59x7). Recheck the complete resolved graph when you update packages; a direct security patch doesn't cover a later package set.

Every primary Full Example matches its chapter exactly, apart from line endings. Activity assertions stop the process on an incorrect result, and their PASS labels name grouped behavior checks, not a count of unique end-to-end scenarios.
