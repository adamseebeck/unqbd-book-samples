# LibraryDesk at the end of Chapter 23

This folder is the LibraryDesk project as it stands at the end of Chapter 23, laid out like your own working folder: global.json, the LibraryDesk solution folder (LibraryDesk.sln, the LibraryDesk server project and the LibraryDesk.Client project), LibraryDesk.Tests and LibraryDesk.Pwa.

## Catch up from here

If you skipped Chapter 23, or your project stopped working and you can't find why, copy this whole folder to a new working folder, such as C:/BookExercises/LibraryDesk, and continue there. Work in the copy, so this folder stays as it is.

## Run it

From the working folder, run this command, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

The terminal shows Now listening on: http://127.0.0.1:58631. Open that address in your browser. Press Ctrl+C in the terminal to stop the app.

## What Chapter 23 added

Chapter 23 published this project. Its one code change is in LibraryDesk.Pwa/Pages/Home.razor: the heading reads Offline reading list (version 2), so the republished offline app installs a new service worker. This folder is the finished app, the same files as Samples/LibraryDesk, Samples/LibraryDesk.Tests and Samples/LibraryDesk.Pwa.

To run the offline app instead, use the command from Chapter 20 and open http://127.0.0.1:58634:

```powershell
dotnet run --project LibraryDesk.Pwa/LibraryDesk.Pwa.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58634
```

To publish both apps and run them the way Chapter 23 does, run these from the working folder, in order, and the python command in a second terminal opened in the working folder:

```powershell
dotnet publish LibraryDesk/LibraryDesk/LibraryDesk.csproj -c Release -o artifacts/server
dotnet publish LibraryDesk.Pwa/LibraryDesk.Pwa.csproj -c Release -o artifacts/pwa
cd artifacts/server
dotnet LibraryDesk.dll --contentRoot . --urls http://127.0.0.1:58633 --environment Production
python -m http.server 58632 --bind 127.0.0.1 --directory artifacts/pwa/wwwroot
```

## Run the tests

Stop the app first, then run this from the working folder:

```powershell
dotnet test LibraryDesk.Tests
```

The last line starts with Passed! and reports Failed: 0, Passed: 24, Skipped: 0, Total: 24.
