# Chapter 14 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this in a copy, so your LibraryDesk project stays as it is. The copy gives the card a title too long to fit and takes away the rule that handles it; you put the rule back yourself.

- Stop your main IDE run with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter14 to a new working folder such as C:\BookExercises\Chapter14. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio and select LibraryDesk with its http profile. The copy gives StyleDemo a long title with no spaces and removes overflow-wrap from BookCard.razor.css. Run it with F5.
- Open http://127.0.0.1:58631/style, press F12, click in the developer tools and press Ctrl+Shift+M to turn on device emulation, and set the width to 360. The long title runs past the card's right border, and the page scrolls sideways.
- Stop the copy with Shift+F5. In the copy's LibraryDesk.Client/Components/BookCard.razor.css, add `overflow-wrap: anywhere;` as the last line inside the `.book-card { }` rule. Run the copy again and reload the page with emulation still at 360. Now the title wraps inside the border, the card grows taller and the page doesn't scroll sideways.
- Click an empty part of the emulated page, so the page has the keyboard focus, then zoom to 200 percent with Ctrl+Plus. The title still wraps inside the card. Press Ctrl+0 to reset the zoom, and note the width and zoom you used.
- Compare with Samples/Activities/Chapter14.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.

## Optional generator

Instead of copying the prepared start, open PowerShell in Samples/ActivityInvestigations and run:

```powershell
python create_copy.py --chapter 14 --output C:/BookExercises/Chapter14
```

Choose an output path that does not exist. The script copies the checkpoint, verifies and applies its checked activity changes, adds the exercise tests and includes them in LibraryDesk.slnx. It never edits the download. Open that generated solution and follow the same task. Python is optional for preparing exercises because ActivityStarts are already supplied.
