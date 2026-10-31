# Chapter 2 activity: a broken SDK pin and a changed heading

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter02, the project at the end of Chapter 2, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 2 --output C:/BookExercises/Chapter02
cd C:/BookExercises/Chapter02
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists any change the script made.

## What the copy changes

global.json asks for SDK 11.0.999, a version that doesn't exist, instead of 11.0.100. Nothing else changes: the Home page still says Hello, world!

## Run it

First look at the broken pin. From the copy's folder, run:

```powershell
dotnet --version
```

Then open global.json, change "11.0.999" to "11.0.100", save, and run dotnet --version again.

Next, in LibraryDesk/LibraryDesk/Components/Pages/Home.razor, change the heading Hello, world! to LibraryDesk setup verified and save. Run the copy with the command from Chapter 2, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Open http://127.0.0.1:58631, and press Ctrl+C in the terminal when you're done.

## What you should see

With 11.0.999 in global.json, dotnet --version fails. Among the lines it prints are A compatible .NET SDK was not found., Requested SDK version: 11.0.999 and global.json file: followed by the path of the copy's global.json. With 11.0.100 back, dotnet --version prints 11.0.100 or a later 11.0 version. After your heading change, the running copy shows the heading LibraryDesk setup verified at http://127.0.0.1:58631.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder:

```powershell
dotnet test Tests/Chapter02.Tests.csproj
```

Near the end, the output reads Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0, followed by the duration, and the last line starts with Build succeeded. If a test fails, the last line reads Build failed with 1 error(s) instead; that error is the failing test, not a compile error. It checks your heading change, so it fails until you make it. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter02.md explains the answer.
