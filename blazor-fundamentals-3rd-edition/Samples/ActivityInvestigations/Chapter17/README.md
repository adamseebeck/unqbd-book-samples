# Chapter 17 activity: a Clear catalog favorites button

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter17, the project at the end of Chapter 17, unchanged; you make the change yourself. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 17 --output C:/BookExercises/Chapter17
cd C:/BookExercises/Chapter17
```

Choose an output folder that doesn't exist yet, outside the companion code. The script copies the checkpoint, adds the Tests folder and prints the new folder. The copy also gets this README and an investigation-manifest.json file that lists any change the script made.

## What you change

The copy is your LibraryDesk project at the end of Chapter 17, unchanged, plus a Tests folder with a test for the button you add.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

Add the button and the ClearFavorites method to LibraryDesk/LibraryDesk.Client/Pages/Catalog.razor, then run the copy and open http://127.0.0.1:58631/catalog. Favorite Blazor Field Notes and C# Field Notes: the status says Favorites in this runtime: 2. Click Clear catalog favorites: Favorites in this runtime: 0. Click it again: still 0.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder:

```powershell
dotnet test Tests/Chapter17.Tests.csproj
```

Near the end, the output reads Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0, followed by the duration, and the last line starts with Build succeeded. If a test fails, the last line reads Build failed with 1 error(s) instead; that error is the failing test, not a compile error. The test finds your button by its id, clear-catalog-favorites. Before you add the button, it fails with ElementNotFoundException, and the last line says Build failed with 1 error(s): that error is the failing test, not a compile error. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter17.md explains the answer.
