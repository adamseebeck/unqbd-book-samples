# Chapter 19 activity: five notices and the hub's length check

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter19, the project at the end of Chapter 19, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 19 --output C:/BookExercises/Chapter19
cd C:/BookExercises/Chapter19
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists each change.

## What the copy changes

In LibraryDesk/LibraryDesk.Client/Pages/LiveCatalog.razor, the page keeps the latest five notices: if (notices.Count == 5) instead of 20.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

Open http://127.0.0.1:58631/live in two tabs. From the first tab, broadcast Notice 1, Notice 2 and so on up to Notice 6. Both lists show Notice 2 to Notice 6; Notice 1 has been dropped. After you delete maxlength="120" from the input and run the copy again, paste a 121-character text (from copy("x".repeat(121)) in the Console) into the Notice box and click Broadcast locally: the status says Use a message from 1 to 120 characters., because the hub checks the length even when the page doesn't.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter19.md explains the answer.
