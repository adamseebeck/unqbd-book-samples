# Chapter 20 activity: a three-title reading list

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter20, the project at the end of Chapter 20, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 20 --output C:/BookExercises/Chapter20
cd C:/BookExercises/Chapter20
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists each change.

## What the copy changes

In LibraryDesk.Pwa/Pages/Home.razor, the reading list allows 3 titles instead of 50.

## Run it

Run the offline app with the dotnet run command from Chapter 20, typed as one line:

```powershell
dotnet run --project LibraryDesk.Pwa/LibraryDesk.Pwa.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58634
```

Open http://127.0.0.1:58634 and give the app a few seconds to download. Press Ctrl+C in the terminal when you're done.

## What you should see

Save titles until the list has three, then try a fourth: the status reads This local list is limited to 3 titles. and the list still has three. Reload: the same three titles load.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder:

```powershell
dotnet test Tests/Chapter20.Tests.csproj
```

The last line starts with Passed! and reports Passed: 1. It saves three titles, tries a fourth, and checks that only three writes reached localStorage. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter20.md explains the answer.
