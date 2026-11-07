# .NET MAUI Fundamentals, Second Edition: companion code

Companion code for *.NET MAUI Fundamentals, Second Edition* by Adam Seebeck: the Inventory application built across 16 chapters with .NET 11. The code is in the `Samples` folder; paths in the book such as `Samples/Inventory.App` refer to it. Keep the whole `Samples` folder together: every project uses its `global.json`, `Directory.Build.props` and `NuGet.Config`, and the activity projects build the source in `Inventory.App` and `Inventory.Core` through relative paths.

- `Samples/Inventory.App` is the finished .NET MAUI app, `Samples/Inventory.Core` is its portable library, and `Samples/Inventory.Tests` holds the portable tests. The finished app is Chapter 16's app plus the Activity solutions: the activity gallery, the Controls and platform page, the profile pages, the MVVM detail page and the order view model.
- `Samples/Checkpoints` has the app as it stands at the end of each chapter, Chapter01 through Chapter16. `Samples/checkpoints.json` lists what each checkpoint adds.
- `Samples/Activities` has one project per chapter activity. Its README maps each activity to its solution source and checks.
- `Samples/DevelopmentConfig` is Chapter 14's desktop configuration example, `Samples/Guides` holds the identity, privacy, navigation and release guides, and `Samples/Tools` has the control library and release examples.

## Which project do I run?

- `Inventory.App` is the finished app. Start here.
- `Checkpoints/ChapterNN` is the app as it stands at the end of chapter N: the previous checkpoint plus that chapter's changes. Each checkpoint is a full copy of the app, with its own `Inventory.App`, `Inventory.Core` from Chapter 3 on and `Inventory.Tests` from Chapter 10 on. It builds and runs on its own and installs as a separate app named "Inventory chapter N", with its own saved data. Chapter 2 makes no code change, so its checkpoint is Chapter 1's app under its own name.
- `Activities/ChapterNN` opens chapter N's activity solution directly. In the finished app, Practice > Open chapter activities opens the same pages.
- `MyInventory` is your own copy of the app, which you create in Chapter 2. See [Your own copy](#your-own-copy).

## Requirements

On Windows, the book uses Visual Studio 2026 with the .NET Multi-platform App UI development workload. Without Visual Studio, install the .NET 11 SDK, version 11.0.100 or later. `global.json` selects it and accepts later .NET 11 feature bands. From `Samples`, install the .NET MAUI workload for your target: `dotnet workload install maui-windows` on Windows, `maui-android` for Android (plus Java and the Android SDK), or `maui-ios` and `maui-maccatalyst` on a Mac with Xcode. The workload supplies the .NET MAUI Controls version, and the Microsoft.Extensions packages use 11.0.0. Chapter 2 covers each target's setup.

## Build and run

In Visual Studio, open `Samples/Inventory.slnx`, choose Windows Machine in the run list and press F5. In a terminal, open it in `Samples` and choose the target once for that terminal. In PowerShell:

```powershell
$env:BookTargetFrameworks = "net11.0-windows10.0.19041.0"
```

`BookTargetFrameworks` is a property these samples define. When it is set, a project builds only the target it names, so you don't need the Android or Apple tools to build for Windows. It doesn't change the portable library's framework. The setting lasts until you close the terminal, so set it again in each new terminal. Use `net11.0-android`, `net11.0-ios` or `net11.0-maccatalyst` on a host set up for that target; in a macOS or Linux shell, the line is `export BookTargetFrameworks=net11.0-maccatalyst`.

Build and run the finished app:

```powershell
dotnet build Inventory.App
dotnet run --project Inventory.App
```

A window opens on the Products page with three products: Laptop (Stock 15), Mouse (Stock 150) and Keyboard (Stock 75).

Run the portable tests with `dotnet test Inventory.Tests`. If you prefer one line without the terminal setting, `-p:BookTargetFrameworks=<tfm> -f <tfm>` still works, for example `dotnet run --project Inventory.App -p:BookTargetFrameworks=net11.0-windows10.0.19041.0 -f net11.0-windows10.0.19041.0`.

## Run a checkpoint

Every checkpoint has an `Inventory.slnx`: open `Samples/Checkpoints/Chapter05/Inventory.slnx` in Visual Studio and press F5. From `Samples`, with `BookTargetFrameworks` set, this runs the same app, as it stands at the end of Chapter 5. Change the chapter number for another checkpoint:

```powershell
dotnet run --project Checkpoints/Chapter05/Inventory.App
```

From Chapter 10 on, Test Explorer or `dotnet test Checkpoints/Chapter10/Inventory.Tests` runs that checkpoint's tests.

The Practice tab has a button for each chapter page the checkpoint contains: Adaptive settings (Chapter 4), Stock dashboard (5), Checkout workflow (7), Categories (8), Offline queue (9), Metric cards (11), Site inspection (12), Token renewal (13), Localized settings and Configuración español (14), and Browsing (15). The finished app has the same buttons plus Open chapter activities.

## Run an activity

In Visual Studio, open `Samples/Activities/Chapter03/Activity03.csproj` and press F5. From `Samples`, with `BookTargetFrameworks` set, this runs the same activity solution. Change the chapter number for another activity:

```powershell
dotnet run --project Activities/Chapter03
```

## Your own copy

Make your changes in your own copy, not in `Inventory.App` or a checkpoint. Chapter 2 creates it from `Samples`:

```powershell
Copy-Item Checkpoints/Chapter01 MyInventory -Recurse
```

Open `MyInventory/Inventory.slnx` in Visual Studio. In `MyInventory/Inventory.App/Inventory.App.csproj`, change `ApplicationTitle` to `My Inventory` and `ApplicationId` to `com.example.myinventory`, so your copy keeps its saved data apart from the checkpoints. Then press F5, or run it with `dotnet run --project MyInventory/Inventory.App`. Keep the copy inside `Samples`, so it uses the same SDK and build settings as the checkpoints.

In each chapter, make every step's change in `MyInventory` and run it. If your version misbehaves, compare it with that chapter's checkpoint, or copy the checkpoint again and continue from there. Do each Activity in a separate copy, such as `Copy-Item MyInventory MyActivity03 -Recurse`, so `MyInventory` stays the same as the checkpoints.

Each checkpoint has its own copy of the source, so a change in one checkpoint affects no other project. The 16 activity projects compile the finished app's files in `Inventory.App` and `Inventory.Core`, so a change there changes the finished app and every activity.

## Separate apps

Each project installs as its own app with its own name and its own saved data: the finished app, each checkpoint ("Inventory chapter N"), each activity ("Inventory activity N") and your own copy. When a chapter asks you to restart the app and check saved data, relaunch the same project you started with.

## Android setup

On a Windows PC with an Intel or AMD processor:

1. Install the tools. In the Visual Studio Installer, select the .NET Multi-platform App UI development workload; it installs the Android SDK, a JDK and the Android emulator. Without Visual Studio, run `dotnet workload install maui-android` in `Samples`, then follow the Android steps on the Visual Studio Code tab of [learn.microsoft.com/dotnet/maui/get-started/installation](https://learn.microsoft.com/dotnet/maui/get-started/installation).
2. Turn on acceleration. Search Windows for windows features, open Turn Windows features on or off, select Hyper-V and Windows Hypervisor Platform (Windows Home lists only the second), and restart.
3. Create an emulator. In Visual Studio, select Tools > Android > Android Device Manager, select New, keep a Pixel base device, choose the x86_64 processor and a recent Android version, select Create, then Start. [learn.microsoft.com/dotnet/maui/android/emulator/device-manager](https://learn.microsoft.com/dotnet/maui/android/emulator/device-manager) shows each screen.
4. With the emulator running, choose it in Visual Studio's run list and press F5, or run these from `Samples`. The first deployment takes several minutes.

   ```powershell
   $env:BookTargetFrameworks = "net11.0-android"
   dotnet run --project Inventory.App
   ```

5. Inventory opens on the emulator with the same three products as on Windows. If the emulator doesn't start, see [learn.microsoft.com/dotnet/maui/android/emulator/hardware-acceleration](https://learn.microsoft.com/dotnet/maui/android/emulator/hardware-acceleration).

Found a problem in the code? Contact us at [unqbd.com/Contact](https://www.unqbd.com/Contact).
