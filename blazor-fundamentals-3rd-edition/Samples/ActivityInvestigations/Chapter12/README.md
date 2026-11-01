# Chapter 12 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this in a copy, so your LibraryDesk project stays as it is. You use the page from the keyboard, then break the module path and watch the page handle it.

- Stop your main IDE run with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter12 to a new working folder such as C:\BookExercises\Chapter12. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio and select LibraryDesk with its http profile. It contains the unchanged checkpoint and an exercise Tests project. Run it with F5.
- Open http://127.0.0.1:58631/interop and use the keyboard: press Tab until Focus input is highlighted and press Enter. The cursor moves into the text box and the status reads Input focused. Click Read browser time, then click another menu link and come back with Interop. Both buttons become active again after a moment, and one click on Read browser time updates the time once. Coming back created a new InteropDemo with its own module and reference; the old one was disposed when you left.
- Stop the copy with Shift+F5. In the copy's LibraryDesk.Client/Pages/InteropDemo.razor, change "./js/catalog.js" to "./js/not-present.js" in the import call and save the file. Run the copy again with F5 using the http profile.
- Open http://127.0.0.1:58631/interop with the Network tab open. The status settles on Browser module unavailable. Reload to retry., both buttons stay disabled, and the Network tab shows not-present.js with status 404.
- Compare with Samples/Activities/Chapter12.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.

## Optional generator

Instead of copying the prepared start, open PowerShell in Samples/ActivityInvestigations and run:

```powershell
python create_copy.py --chapter 12 --output C:/BookExercises/Chapter12
```

Choose an output path that does not exist. The script copies the checkpoint, verifies and applies its checked activity changes, adds the exercise tests and includes them in LibraryDesk.slnx. It never edits the download. Open that generated solution and follow the same task. Python is optional for preparing exercises because ActivityStarts are already supplied.
