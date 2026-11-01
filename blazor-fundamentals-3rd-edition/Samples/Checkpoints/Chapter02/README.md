# LibraryDesk at the end of Chapter 2

This folder is the LibraryDesk project as it stands at the end of Chapter 2, laid out like your own working folder: global.json and the LibraryDesk solution folder (LibraryDesk.sln, the LibraryDesk server project and the LibraryDesk.Client project).

## Catch up from here

If you skipped Chapter 2, or your project stopped working and you can't find why, copy this whole folder to a new working folder, such as C:/BookExercises/LibraryDesk, and continue with Chapter 3 there. Work in the copy, so this folder stays as it is.

## Run it

From the working folder, run this command, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

The terminal shows Now listening on: http://127.0.0.1:58631. Open that address in your browser. Press Ctrl+C in the terminal to stop the app.

## What Chapter 2 added

Chapter 2 created the project from the Blazor Web App template, pinned the SDK in global.json and kept the Data Protection keys in memory with two lines in LibraryDesk/Program.cs. Open http://127.0.0.1:58631: the template's Home page shows Hello, world!
