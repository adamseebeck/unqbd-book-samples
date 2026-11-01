# Chapter 12 activity: a missing JavaScript module

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter12, the project at the end of Chapter 12, unchanged; you make the change yourself. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 12 --output C:/BookExercises/Chapter12
cd C:/BookExercises/Chapter12
```

Choose an output folder that doesn't exist yet, outside the companion code. The script copies the checkpoint, adds the Tests folder and prints the new folder. The copy also gets this README and an investigation-manifest.json file that lists any change the script made.

## What you change

Nothing in the copy is changed for you. First run the copy and use /interop from the keyboard. Then stop it, and in the copy's LibraryDesk/LibraryDesk.Client/Pages/InteropDemo.razor change "./js/catalog.js" to "./js/not-present.js" in the import call and save the file.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

Before your change, open http://127.0.0.1:58631/interop, press Tab until Focus input is highlighted and press Enter: the cursor moves into the text box and the status reads Input focused. Click Read browser time, click another menu link and come back with Interop: both buttons become active again after a moment, and one click on Read browser time updates the time once.

After your change, open http://127.0.0.1:58631/interop with the Network tab open. The status settles on Browser module unavailable. Reload to retry., both buttons stay disabled, and the Network tab shows not-present.js with status 404.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder:

```powershell
dotnet test Tests/Chapter12.Tests.csproj
```

After your change the output ends with Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0, duration: ... and then Build succeeded in .... Before your change the test fails: the output ends with Test summary: total: 1, failed: 1, succeeded: 0, skipped: 0, duration: ... and Build failed with 1 error(s) in .... That error is the failing test, not a compile error. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter12.md explains the answer.
