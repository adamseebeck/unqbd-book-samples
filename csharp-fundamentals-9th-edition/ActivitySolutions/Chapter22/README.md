# Chapter 22 data activity

From this Chapter22 folder, run the CRUD activity:

```powershell
dotnet run --project SchoolRecords.csproj
```

The CRUD demo creates a fresh temporary SQLite database. The optional migration experiment below uses a separate database and never calls EnsureCreated. The downloadable folder nests SchoolMigrations for convenience; SchoolRecords.csproj explicitly excludes its C# files. When creating projects yourself, follow the book's sibling-directory instructions.

```powershell
cd SchoolMigrations
dotnet tool restore
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

The final command prints Students: 0. Add InitialCreate once in a fresh copy of this folder. Later runs can repeat database update and dotnet run without adding another migration with the same name. The local tool, provider and Design package all use 11.0.0. These practice commands operate on school-migrations.db in this folder.
