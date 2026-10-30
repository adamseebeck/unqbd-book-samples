# Chapter 3 activity: Hot Reload on the counter page

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter03, the project at the end of Chapter 3, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 3 --output C:/BookExercises/Chapter03
cd C:/BookExercises/Chapter03
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists each change.

## What the copy changes

In LibraryDesk/LibraryDesk.Client/Pages/Run.razor, the heading reads My first server counter instead of First run.

## Run it

Start the copy with dotnet watch, typed as one line:

```powershell
dotnet watch --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Open http://127.0.0.1:58631/run. Press Ctrl+C in the terminal when you're done.

## What you should see

The /run page shows the heading My first server counter. Two clicks on Add one show Current count: 2. After F5 the page shows Current count: 0, because the reload starts a new circuit with a new component. When you change the heading text and save, the terminal reports File updated and then C# and Razor changes applied, and the page shows the new heading without a restart. When you change private int count; to private long count; and save, the terminal reports Restart is needed to apply the changes and asks Do you want to restart your app? Press y, and the count starts again at 0.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder:

```powershell
dotnet test Tests/Chapter03.Tests.csproj
```

The last line starts with Passed! and reports Passed: 1. Run it before you edit Run.razor: it checks the copy as created. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter03.md explains the answer.
