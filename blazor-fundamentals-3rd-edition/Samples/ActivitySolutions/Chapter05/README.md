# Chapter 5 activity: solved copy

This folder is Samples/Checkpoints/Chapter05, the LibraryDesk project at the end of Chapter 5, with the activity solved. Compare it with your own copy. You can run the app and its test from this folder; dotnet adds bin and obj folders here as it builds, which you can delete at any time.

## The solved change

LibraryDesk/LibraryDesk.Client/Pages/BindingDemo.razor keeps @bind:event="oninput" on the input and has a new line under it, `<p id="match-count">Matches: @Matches.Length</p>`, above the Search text line.

## Run it

From the folder, run the command from Chapter 2, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Open http://127.0.0.1:58631/binding. The list changes as you type, and the new line shows Matches: 3 for an empty box, Matches: 1 for BLAZOR and Matches: 0 for xyz. Press Ctrl+C in the terminal to stop the app.

## Run its test

Stop the app, then run this from the folder:

```powershell
dotnet test Tests/Chapter05.Tests.csproj
```

Near the end, the output reads Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0, followed by the duration, and the last line starts with Build succeeded.

Samples/Activities/Chapter05.md explains the answer.
