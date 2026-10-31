# Chapter 18 activity: a List that lets duplicates in

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter18, the project at the end of Chapter 18, with the change below.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 18 --output C:/BookExercises/Chapter18
cd C:/BookExercises/Chapter18
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists any change the script made.

## What the copy changes

In LibraryDesk/LibraryDesk.Client/Services/FavoritesState.cs, ids is a List<int>, and Add appends and raises Changed every time.

## Run it

Run the one test that checks favorites, from the copy's folder:

```powershell
dotnet test LibraryDesk.Tests --filter FavoritesRefresh
```

Then put the original lines back in FavoritesState.cs, `private readonly HashSet<int> ids = [];` and `if (ids.Add(id)) Changed?.Invoke();`, and run the same command again.

## What you should see

The filter runs only FavoritesRefreshSiblingsAndIgnoreDuplicates, and it fails after a few seconds. Its error message names WaitForFailedException, because WaitForAssertion kept retrying until its time ran out; ignore its advice about slower hardware. The real cause is the line under it that starts with ----: Assert.Equal() Failure: Strings differ, with Expected: "Favorites: 1" and Actual: "Favorites: 2". The second click on Favorite C# added the same ID again. The output then reads Test summary: total: 1, failed: 1, succeeded: 0, skipped: 0, and the last line says Build failed with 1 error(s). That error is the failing test, not a compile error. With the HashSet lines back, the output reads Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0, and the last line starts with Build succeeded.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter18.md explains the answer.
