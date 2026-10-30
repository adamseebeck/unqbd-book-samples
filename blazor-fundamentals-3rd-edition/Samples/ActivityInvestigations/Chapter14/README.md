# Chapter 14 activity: a very long title

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter14, the project at the end of Chapter 14, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 14 --output C:/BookExercises/Chapter14
cd C:/BookExercises/Chapter14
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists each change.

## What the copy changes

In LibraryDesk/LibraryDesk.Client/Pages/StyleDemo.razor, the card has a very long title with no spaces.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

Open http://127.0.0.1:58631/style, turn on device emulation (Ctrl+Shift+M) and set the width to 360. The long title wraps inside the border, the card grows taller and the page doesn't scroll sideways. At 200 percent zoom the title still wraps inside the card. After you delete overflow-wrap: anywhere; from LibraryDesk/LibraryDesk.Client/Components/BookCard.razor.css and run the copy again, the title runs past the border and the page scrolls sideways.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter14.md explains the answer.
