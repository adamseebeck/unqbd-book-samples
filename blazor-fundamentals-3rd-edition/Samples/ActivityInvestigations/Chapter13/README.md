# Chapter 13 activity: a failing SelectBook, a third book and prices

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter13, the project at the end of Chapter 13, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 13 --output C:/BookExercises/Chapter13
cd C:/BookExercises/Chapter13
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists each change.

## What the copy changes

In LibraryDesk/LibraryDesk.Client/Components/SelectBook.razor, the Select method throws an exception for book 2 before it calls the callback.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

On http://127.0.0.1:58631/templates, Select C# Field Notes shows Selected identifier: 1. Select Blazor Field Notes replaces the list with The catalog component failed. Reload to retry., and the line still reads Selected identifier: 1, because the exception happened before the callback reached the page. After you add new(3, "Database Field Notes", 26m) to the books array in TemplatesDemo.razor and run the copy again, Select Database Field Notes shows Selected identifier: 3. With the price in the template, each row starts with its price, such as $26.00 Select Database Field Notes. The solved third book and price layout are in Samples/ActivitySolutions/Chapter13.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter13.md explains the answer.
