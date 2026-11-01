# Chapter 7 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do the first step in your running project, and the rest in a copy so your LibraryDesk project stays as it is.

- With your unchanged cumulative app running and the browser's Network tab open, type http://127.0.0.1:58631/books/2 and press Enter, then click C# and Blazor. The typed address adds a document row; each link click adds a fetch row. The title always matches the number in the address.
- Stop debugging with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter07 to a new working folder such as C:\BookExercises\Chapter07. Open its LibraryDesk/LibraryDesk.slnx in Visual Studio, select LibraryDesk with its http profile and press F5. This prepared start gives Routes an InteractiveServer render mode, so the Router keeps a page instance when only its parameters change. It also moves BookDetails's loading to OnInitializedAsync, creating the problem you investigate next.
- In the copy, open http://127.0.0.1:58631/books/1 and click Blazor. The address changes to /books/2, but the page still shows C# Field Notes and Price: $24.00: the instance was initialized once, with Id 1. Press F5, and Blazor Field Notes appears, because a full load creates a new instance.
- Stop the copy with Shift+F5, change OnInitializedAsync back to OnParametersSetAsync in the copy's BookDetails.razor, and run it again. Now clicking Blazor shows Blazor Field Notes.
- Stop the copy with Shift+F5 again. In the copy's BookDetails.razor, add a second route template under the first @page line: `@page "/catalog-item/{Id:int}"`. Run the copy again. /catalog-item/2 shows Blazor Field Notes, /catalog-item/999 shows No book has this identifier. and /catalog-item/abc shows Not Found.
- Compare with Samples/Activities/Chapter07.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.

## Optional generator

Instead of copying the prepared start, open PowerShell in Samples/ActivityInvestigations and run:

```powershell
python create_copy.py --chapter 7 --output C:/BookExercises/Chapter07
```

Choose an output path that does not exist. The script copies the checkpoint, verifies and applies its checked activity changes, adds the exercise tests and includes them in LibraryDesk.slnx. It never edits the download. Open that generated solution and follow the same task. Python is optional for preparing exercises because ActivityStarts are already supplied.
