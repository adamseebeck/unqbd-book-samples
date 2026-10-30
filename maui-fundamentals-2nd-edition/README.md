# .NET MAUI Fundamentals, Second Edition: companion code

Companion code for *.NET MAUI Fundamentals, Second Edition* by Adam Seebeck: the Inventory application built across 16 chapters with .NET 11. The code is in the `Samples` folder; paths in the book such as `Samples/Inventory.App` refer to it. Keep the whole `Samples` folder together, because the chapter checkpoints and activity projects build the shared source in `Inventory.App` and `Inventory.Core` through relative paths.

- `Samples/Inventory.App` is the .NET MAUI reference application, `Samples/Inventory.Core` is its portable library, and `Samples/Inventory.Tests` holds the portable tests.
- `Samples/Checkpoints` has one entry project per chapter, Chapter01 through Chapter16, over that shared source.
- `Samples/Activities` has one project per chapter activity. Its README maps each activity to its solution source and checks.
- `Samples/DevelopmentConfig` is Chapter 14's desktop configuration example, `Samples/Guides` holds the identity, privacy, navigation and release guides, and `Samples/Tools` has the control library and release examples.

## Which project do I run?

- `Inventory.App` is the finished app. Start here.
- `Checkpoints/ChapterNN/ChapterNN.csproj` is the same finished app under the name "Inventory chapter N". It is not a snapshot of that chapter.
- `Activities/ChapterNN/ActivityNN.csproj` opens chapter N's exercise directly. In the finished app, Practice > Open chapter activities opens the same pages.

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

A window opens on the Products page with three products: Laptop (Stock 15), Mouse (Stock 150) and Keyboard (Stock 75).

`BookTargetFrameworks` is a property these samples define: it selects the application's target without changing the portable library's framework. Use the same pattern for a checkpoint or activity, and use the Android, iOS or Mac Catalyst framework on a host set up for it. Run the portable tests with `dotnet test Inventory.Tests/Inventory.Tests.csproj`.

## Run an activity

From `Samples`, this runs Chapter 3's activity on Windows. Change the chapter number for another activity:

```powershell
dotnet run --project Activities/Chapter03/Activity03.csproj -p:BookTargetFrameworks=net11.0-windows10.0.19041.0 -f net11.0-windows10.0.19041.0
```

## Before you change code

Copy the whole `Samples` folder and make your changes in the copy. All 33 entry projects (the app, the 16 checkpoints and the 16 activities) compile the same `Inventory.App` files, so one edit changes every one of them. To get back to the original, delete the copy.

## Separate apps

Each entry project installs as its own app with its own name and its own saved data. When a chapter asks you to restart the app and check saved data, relaunch the same project you started with.

## Android setup

On a Windows PC with an Intel or AMD processor:

1. Install the tools. In the Visual Studio Installer, select the .NET Multi-platform App UI development workload; it installs the Android SDK, a JDK and the Android emulator. Without Visual Studio, run `dotnet workload install maui-android` in `Samples`, then follow the Android steps on the Visual Studio Code tab of [learn.microsoft.com/dotnet/maui/get-started/installation](https://learn.microsoft.com/dotnet/maui/get-started/installation).
2. Turn on acceleration. Search Windows for windows features, open Turn Windows features on or off, select Hyper-V and Windows Hypervisor Platform (Windows Home lists only the second), and restart.
3. Create an emulator. In Visual Studio, open the Android Device Manager, select New, keep a Pixel base device, choose the x86_64 processor and a recent Android version, select Create, then Start. [learn.microsoft.com/dotnet/maui/android/emulator/device-manager](https://learn.microsoft.com/dotnet/maui/android/emulator/device-manager) shows each screen.
4. With the emulator running, choose it in Visual Studio's run list and press F5, or run this command from `Samples`. The first deployment takes several minutes.

   ```powershell
   dotnet run --project Inventory.App/Inventory.App.csproj -p:BookTargetFrameworks=net11.0-android -f net11.0-android
   ```

5. Inventory opens on the emulator with the same three products as on Windows. If the emulator doesn't start, see [learn.microsoft.com/dotnet/maui/android/emulator/hardware-acceleration](https://learn.microsoft.com/dotnet/maui/android/emulator/hardware-acceleration).

Found a problem in the code? Contact us at [unqbd.com/Contact](https://www.unqbd.com/Contact).
