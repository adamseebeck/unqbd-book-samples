# Chapter 6 activity: a Remove C# button

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter06, the project at the end of Chapter 6, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 6 --output C:/BookExercises/Chapter06
cd C:/BookExercises/Chapter06
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists each change.

## What the copy changes

LibraryDesk/LibraryDesk.Client/Pages/FavoritesDemo.razor has a new Remove C# button, above FavoritesSummary, that calls Favorites.Remove(1).

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

Open http://127.0.0.1:58631/favorites and click Favorite C# and Favorite Blazor: the summary shows Favorites: 2 and Saved IDs: 1, 2. Click Remove C# twice: the summary shows Favorites: 1 and Saved IDs: 2, and the second click changes nothing. Open /favorites in a second tab: it starts at Favorites: 0.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder:

```powershell
dotnet test Tests/Chapter06.Tests.csproj
```

The last line starts with Passed! and reports Passed: 1. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter06.md explains the answer.
