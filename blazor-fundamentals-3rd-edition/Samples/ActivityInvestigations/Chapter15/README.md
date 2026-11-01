# Chapter 15 activity: an ordinary @foreach loop instead of Virtualize

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter15, the project at the end of Chapter 15, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 15 --output C:/BookExercises/Chapter15
cd C:/BookExercises/Chapter15
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists any change the script made.

## What the copy changes

In LibraryDesk/LibraryDesk.Client/Pages/VirtualDemo.razor, the rows come from an ordinary @foreach loop instead of Virtualize.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

Open http://127.0.0.1:58631/virtual, press F12, open the Console tab and run document.getElementById("results").childElementCount. It prints 10000: every row is in the DOM, and there are no spacers. The last row is 10000: Catalog title 10000. Compare it with the 18 you counted in step 4 of Chapter 15. Then stop the copy, replace the @foreach loop in LibraryDesk/LibraryDesk.Client/Pages/VirtualDemo.razor with the Virtualize element from step 1 but with OverscanCount="10", and run the copy again: the same line prints 32 (30 rows and 2 spacers), at the top and in the middle of the list.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter15.md explains the answer.
