# .NET MAUI Fundamentals, Second Edition: companion code

Companion code for *.NET MAUI Fundamentals, Second Edition* by Adam Seebeck: the Inventory application built across 16 chapters with .NET 11. The code is in the `Samples` folder; paths in the book such as `Samples/Inventory.App` refer to it. Keep the whole `Samples` folder together, because the chapter checkpoints and activity projects build the shared source in `Inventory.App` and `Inventory.Core` through relative paths.

- `Samples/Inventory.App` is the .NET MAUI reference application, `Samples/Inventory.Core` is its portable library, and `Samples/Inventory.Tests` holds the portable tests.
- `Samples/Checkpoints` has one entry project per chapter, Chapter01 through Chapter16, over that shared source. They are build configurations, not separate snapshots.
- `Samples/Activities` has one project per chapter activity. Its README maps each activity to its solution source and checks.
- `Samples/DevelopmentConfig` is Chapter 14's desktop configuration example, `Samples/Guides` holds the identity, privacy, navigation and release guides, and `Samples/Tools` has the control library and release examples.

## Requirements

Install the .NET 11 SDK, version 11.0.100 or later. `global.json` selects it and accepts later .NET 11 feature bands. From `Samples`, install the .NET MAUI workload for your target: `dotnet workload install maui-windows` on Windows, `maui-android` for Android (plus Java and the Android SDK), or `maui-ios` and `maui-maccatalyst` on a Mac with Xcode. The workload supplies the .NET MAUI Controls version, and the Microsoft.Extensions packages use 11.0.0. Chapter 2 covers each target's setup.

## Build and run

From `Samples`, build the Windows target of the reference application:

```powershell
dotnet build Inventory.App/Inventory.App.csproj -p:BookTargetFrameworks=net11.0-windows10.0.19041.0 -f net11.0-windows10.0.19041.0
```

Run it with the same target override:

```powershell
dotnet run --project Inventory.App/Inventory.App.csproj -p:BookTargetFrameworks=net11.0-windows10.0.19041.0 -f net11.0-windows10.0.19041.0
```

`BookTargetFrameworks` is a property these samples define: it selects the application's target without changing the portable library's framework. Use the same pattern for a checkpoint or activity, such as `Checkpoints/Chapter03/Chapter03.csproj`, and use the Android, iOS or Mac Catalyst framework on a host set up for it. Run the portable tests with `dotnet test Inventory.Tests/Inventory.Tests.csproj`.

Found a problem in the code? Contact us at [unqbd.com/Contact](https://www.unqbd.com/Contact).
