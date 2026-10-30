# Blazor Fundamentals, Third Edition: companion code

Companion code for *Blazor Fundamentals, Third Edition* by Adam Seebeck: the LibraryDesk application built up across 23 chapters with .NET 11. The code is in the `Samples` folder; paths in the book such as `Samples/Checkpoints/Chapter03` refer to it.

- `Samples/Checkpoints` has nineteen minimal complete server/client checkpoints for Chapters 2 through 17, 19, 21 and 22. Each is the Chapter 2 project plus that chapter's files only, and each has a README with its run command; see `Samples/README.md`.
- `Samples/LibraryDesk` is the finished reference application. `Samples/LibraryDesk.Tests` holds Chapter 18's component tests, and `Samples/LibraryDesk.Pwa` is Chapter 20's progressive web app.
- `Samples/Activities` maps every chapter activity to its source and expected observations. `Samples/ActivitySolutions` has solved checkpoints for Chapters 4, 5, 9 and 13.
- `Samples/ActivityInvestigations` creates a disposable working copy for an activity: `python create_copy.py --chapter 6 --output <new folder>`.

## Requirements

Install the .NET 11 SDK, version 11.0.100 or later. Each project's `global.json` selects it and accepts later .NET 11 feature bands, and the framework packages use 11.0.0. Visual Studio 2026 or Visual Studio Code with C# Dev Kit both work; the command line is the reproducible route. Python 3 runs the activity copy script and serves the published PWA in Chapters 20 and 23.

## Run a checkpoint

Copy the checkpoint folder you want to a separate working folder, then run this from that folder:

```powershell
dotnet run --project LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Use one host at a time or choose a different port. For Chapter 18's tests, run `dotnet test LibraryDesk.Tests/LibraryDesk.Tests.csproj` from `Samples`.

Found a problem in the code? Contact us at [unqbd.com/Contact](https://www.unqbd.com/Contact).
