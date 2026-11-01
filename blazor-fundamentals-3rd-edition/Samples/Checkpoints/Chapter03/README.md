# LibraryDesk at the end of Chapter 3

This folder is the LibraryDesk project as it stands at the end of Chapter 3, laid out like your own working folder: global.json and the LibraryDesk solution folder (LibraryDesk.sln, the LibraryDesk server project and the LibraryDesk.Client project).

## Catch up from here

If you skipped Chapter 3, or your project stopped working and you can't find why, copy this whole folder to a new working folder, such as C:/BookExercises/LibraryDesk, and continue with Chapter 4 there. Work in the copy, so this folder stays as it is.

## Run it

From the working folder, run this command, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

The terminal shows Now listening on: http://127.0.0.1:58631. Open that address in your browser. Press Ctrl+C in the terminal to stop the app.

## What Chapter 3 added

Chapter 3 added the site menu, the LibraryDesk Home page, the tab icon (LibraryDesk/wwwroot/favicon.png) and the interactive counter page. Open http://127.0.0.1:58631 for the Home page and http://127.0.0.1:58631/run (Run in the menu) for the counter.
