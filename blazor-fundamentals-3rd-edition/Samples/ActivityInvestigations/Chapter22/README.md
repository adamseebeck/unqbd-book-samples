# Chapter 22 activity: a Warning threshold for the diagnostics category

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter22, the project at the end of Chapter 22, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 22 --output C:/BookExercises/Chapter22
cd C:/BookExercises/Chapter22
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists each change.

## What the copy changes

The line "LibraryDesk.Components.Pages.Diagnostics": "Warning", is in the LogLevel section of both LibraryDesk/LibraryDesk/appsettings.json and LibraryDesk/LibraryDesk/appsettings.Development.json.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

Open http://127.0.0.1:58631/diagnostics and click twice. The page shows Reads recorded: 2, but the terminal shows no CatalogRead entries: the category now starts at Warning, and the entries are Information. After you change "Warning" back to "Information" in both files and run the copy again, each click writes an info: LibraryDesk.Components.Pages.Diagnostics[0] line followed by its CatalogRead line.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder:

```powershell
dotnet test Tests/Chapter22.Tests.csproj
```

The last line starts with Passed! and reports Passed: 1. It checks the copy as created: two clicks count 2, and no CatalogRead entry is written. After you change the threshold back to Information, run `$env:ACTIVITY_LOGS_SUPPRESSED = "false"` in the same PowerShell terminal and run the test again: now it expects the two entries. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter22.md explains the answer.
