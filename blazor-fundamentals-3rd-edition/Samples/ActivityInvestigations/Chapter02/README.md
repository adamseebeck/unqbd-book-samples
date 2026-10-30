# Chapter 2 activity: the SDK pin and a changed heading

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter02, the project at the end of Chapter 2, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 2 --output C:/BookExercises/Chapter02
cd C:/BookExercises/Chapter02
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists each change.

## What the copy changes

In LibraryDesk/LibraryDesk/Components/Pages/Home.razor, the heading reads LibraryDesk setup verified instead of Hello, world!

## Run it

First try the SDK pin. In the copy's global.json, change "11.0.100" to "11.0.999", a version that doesn't exist, save, and run:

```powershell
dotnet --version
```

Put "11.0.100" back, save, and run dotnet --version again. Then run the copy with the command from Chapter 2, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Open http://127.0.0.1:58631, and press Ctrl+C in the terminal when you're done.

## What you should see

With 11.0.999 in global.json, dotnet --version fails with A compatible .NET SDK was not found. The error names the requested version, 11.0.999, and the global.json file it came from. With 11.0.100 back, dotnet --version prints 11.0.100 or a later 11.0 version again. The running copy shows the heading LibraryDesk setup verified at http://127.0.0.1:58631.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder:

```powershell
dotnet test Tests/Chapter02.Tests.csproj
```

The last line starts with Passed! and reports Passed: 1. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter02.md explains the answer.
