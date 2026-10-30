# LibraryDesk at the end of Chapter 5

This folder is the LibraryDesk project as it stands at the end of Chapter 5, laid out like your own working folder: global.json and the LibraryDesk solution folder (LibraryDesk.sln, the LibraryDesk server project and the LibraryDesk.Client project).

## Catch up from here

If you skipped Chapter 5, or your project stopped working and you can't find why, copy this whole folder to a new working folder, such as C:/BookExercises/LibraryDesk, and continue with Chapter 6 there. Work in the copy, so this folder stays as it is.

## Run it

From the working folder, run this command, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

The terminal shows Now listening on: http://127.0.0.1:58631. Open that address in your browser. Press Ctrl+C in the terminal to stop the app.

## What Chapter 5 added

Chapter 5 added http://127.0.0.1:58631/binding (Binding in the menu): a text box that filters three book titles as you type.
