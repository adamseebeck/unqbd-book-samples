# Chapter 3 activity: a Reset button with Hot Reload

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter03, the project at the end of Chapter 3, unchanged; you make the change yourself. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 3 --output C:/BookExercises/Chapter03
cd C:/BookExercises/Chapter03
```

Choose an output folder that doesn't exist yet, outside the companion code. The script copies the checkpoint, adds the Tests folder and prints the new folder. The copy also gets this README and an investigation-manifest.json file that lists any change the script made.

## What you change

Nothing in the app: the copy is Samples/Checkpoints/Chapter03 with a Tests folder added. You make the change yourself while the app runs.

## Run it

Start the copy with dotnet watch, typed as one line:

```powershell
dotnet watch --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Open http://127.0.0.1:58631/run. Leave dotnet watch running while you edit LibraryDesk/LibraryDesk.Client/Pages/Run.razor: add a Reset button under Add one, with @onclick="Reset", and a Reset method in @code that sets count to 0. Press Ctrl+C in the terminal when you're done.

## What you should see

Two clicks on Add one show Current count: 2. When you save the Reset button and method, the terminal reports File updated and then C# and Razor changes applied, and the page shows the Reset button while it still says Current count: 2. Click Reset: Current count: 0. When you change private int count; to private long count; and save, the terminal reports Restart is needed to apply the changes and asks Do you want to restart your app? Press y, and the count starts again at 0.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder:

```powershell
dotnet test Tests/Chapter03.Tests.csproj
```

Near the end, the output reads Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0, followed by the duration, and the last line starts with Build succeeded. If a test fails, the last line reads Build failed with 1 error(s) instead; that error is the failing test, not a compile error. Stop dotnet watch first. The test clicks Add one twice and Reset once, so it fails until you add Reset. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter03.md explains the answer.
