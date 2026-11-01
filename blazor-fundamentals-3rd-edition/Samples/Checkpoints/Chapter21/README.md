# LibraryDesk at the end of Chapter 21

This folder is the LibraryDesk project as it stands at the end of Chapter 21, laid out like your own working folder: global.json, the LibraryDesk solution folder (LibraryDesk.sln, the LibraryDesk server project and the LibraryDesk.Client project), LibraryDesk.Tests and LibraryDesk.Pwa.

## Catch up from here

If you skipped Chapter 21, or your project stopped working and you can't find why, copy this whole folder to a new working folder, such as C:/BookExercises/LibraryDesk, and continue with Chapter 22 there. Work in the copy, so this folder stays as it is.

## Run it

From the working folder, run this command, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

The terminal shows Now listening on: http://127.0.0.1:58631. Open that address in your browser. Press Ctrl+C in the terminal to stop the app.

## What Chapter 21 added

Chapter 21 added http://127.0.0.1:58631/accessibility (Accessibility in the menu), a static page with the interactive feedback form, and the Skip to content link at the top of every page. LibraryDesk.Tests also holds AccessibilityTests.cs.

To run the offline app instead, use the command from Chapter 20 and open http://127.0.0.1:58634:

```powershell
dotnet run --project LibraryDesk.Pwa/LibraryDesk.Pwa.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58634
```

## Run the tests

Stop the app first, then run this from the working folder:

```powershell
dotnet test LibraryDesk.Tests
```

Near the end, the output reads Test summary: total: 24, failed: 0, succeeded: 24, skipped: 0, followed by the duration, and the last line starts with Build succeeded.
