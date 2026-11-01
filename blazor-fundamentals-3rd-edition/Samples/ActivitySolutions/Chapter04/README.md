# Chapter 4 activity: solved copy

This folder is Samples/Checkpoints/Chapter04, the LibraryDesk project at the end of Chapter 4, with the activity solved. Compare it with your own copy. You can run the app and its test from this folder; dotnet adds bin and obj folders here as it builds, which you can delete at any time.

## The solved change

LibraryDesk/LibraryDesk.Client/Pages/ComponentsDemo.razor has a third ToggleMessage with the Message Database Field Notes is available. and a HiddenText of Tap to read. Your own message can be any text.

## Run it

From the folder, run the command from Chapter 2, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Open http://127.0.0.1:58631/components. The third note starts with Tap to read. Click the middle note's button twice and the third note's button once: the first note shows Details hidden, the middle one Details hidden, and the third Database Field Notes is available. The terminal shows three Initialized lines from prerendering and three from the live page. Press Ctrl+C in the terminal to stop the app.

## Run its test

Stop the app, then run this from the folder:

```powershell
dotnet test Tests/Chapter04.Tests.csproj
```

Near the end, the output reads Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0, followed by the duration, and the last line starts with Build succeeded.

Samples/Activities/Chapter04.md explains the answer.
