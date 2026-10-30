# Chapter 17 activity: a Clear catalog favorites button

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter17, the project at the end of Chapter 17, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 17 --output C:/BookExercises/Chapter17
cd C:/BookExercises/Chapter17
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists each change.

## What the copy changes

LibraryDesk/LibraryDesk.Client/Pages/Catalog.razor has the finished change: a button with the id clear-catalog-favorites above the status line, and a ClearFavorites method above Add that calls Favorites.Clear() and updates the status.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

Open http://127.0.0.1:58631/catalog. Favorite Blazor Field Notes and C# Field Notes: the status says Favorites in this runtime: 2. Click Clear catalog favorites: Favorites in this runtime: 0. Click it again: still 0.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder:

```powershell
dotnet test Tests/Chapter17.Tests.csproj
```

The last line starts with Passed! and reports Passed: 1. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter17.md explains the answer.
