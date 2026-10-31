# LibraryDesk at the end of Chapter 19

This folder is the LibraryDesk project as it stands at the end of Chapter 19, laid out like your own working folder: global.json, the LibraryDesk solution folder (LibraryDesk.sln, the LibraryDesk server project and the LibraryDesk.Client project) and LibraryDesk.Tests.

## Catch up from here

If you skipped Chapter 19, or your project stopped working and you can't find why, copy this whole folder to a new working folder, such as C:/BookExercises/LibraryDesk, and continue with Chapter 20 there. Work in the copy, so this folder stays as it is.

## Run it

From the working folder, run this command, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

The terminal shows Now listening on: http://127.0.0.1:58631. Open that address in your browser. Press Ctrl+C in the terminal to stop the app.

## What Chapter 19 added

Chapter 19 added http://127.0.0.1:58631/live (Live in the menu): catalog notices sent through the CatalogHub SignalR hub at /hubs/catalog. LibraryDesk.Tests also holds LiveTransportTests.cs.

## Run the tests

Stop the app first, then run this from the working folder:

```powershell
dotnet test LibraryDesk.Tests
```

Near the end, the output reads Test summary: total: 23, failed: 0, succeeded: 23, skipped: 0, followed by the duration, and the last line starts with Build succeeded.
