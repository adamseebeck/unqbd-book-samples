# Chapter 7 activity: an interactive Router and a stale page

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter07, the project at the end of Chapter 7, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 7 --output C:/BookExercises/Chapter07
cd C:/BookExercises/Chapter07
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists any change the script made.

## What the copy changes

Two lines. In LibraryDesk/LibraryDesk/Components/App.razor, `<Routes @rendermode="InteractiveServer" />` makes the Router itself interactive, so clicks are handled over the circuit and a page keeps its instance when only its parameters change. In LibraryDesk/LibraryDesk/Components/Pages/BookDetails.razor, the book is loaded in OnInitializedAsync instead of OnParametersSetAsync.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

Open http://127.0.0.1:58631/books/1 and click Blazor. The address changes to /books/2, but the page still shows C# Field Notes and Price: $24.00: the instance was initialized once, with Id 1. Press F5, and Blazor Field Notes appears, because a full load creates a new instance. Stop the copy with Ctrl+C, change OnInitializedAsync back to OnParametersSetAsync and run the copy again: clicking Blazor shows Blazor Field Notes. Stop the copy again, add @page "/catalog-item/{Id:int}" under the first @page line and run it: /catalog-item/2 shows Blazor Field Notes, /catalog-item/999 shows No book has this identifier. and /catalog-item/abc shows Not Found.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter07.md explains the answer.
