# Chapter 19 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this activity in a copy, so your LibraryDesk project stays as it is. The task is to make the page keep only the latest five notices instead of twenty, and to see the hub reject a notice that is too long.

- Stop your main run with Shift+F5. Copy Samples/ActivityStarts/Chapter19 in File Explorer to a new working folder, such as C:\BookExercises\Chapter19. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio. Select LibraryDesk with its http profile. The prepared start is the unchanged chapter checkpoint.
- In the copy's LibraryDesk/LibraryDesk.Client/Pages/LiveCatalog.razor, change the CatalogNotice callback so the list never holds more than five notices.
- Run the copy with F5 using the http profile and open http://127.0.0.1:58631/live in two tabs. From the first tab, broadcast Notice 1, Notice 2 and so on up to Notice 6. Both lists show Notice 2 to Notice 6; Notice 1 has been dropped.
- Stop the copy with Shift+F5. In LiveCatalog.razor, delete maxlength="120" from the input and run the copy again. Reload the /live tab, or open http://127.0.0.1:58631/live again, so the page has the new input. Press F12, open the Console tab, type copy("x".repeat(121)) and press Enter: the Console's copy function puts a 121-character text on the clipboard. Paste it into the Notice box with Ctrl+V and click Broadcast locally. The status says Use a message from 1 to 120 characters.: the hub checks the length even when the page doesn't.
- Compare your change with Samples/Activities/Chapter19.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.

## Optional generator

Instead of copying the prepared start, open PowerShell in Samples/ActivityInvestigations and run:

```powershell
python create_copy.py --chapter 19 --output C:/BookExercises/Chapter19
```

Choose an output path that does not exist. The script copies the checkpoint, verifies and applies its checked activity changes, adds the exercise tests and includes them in LibraryDesk.slnx. It never edits the download. Open that generated solution and follow the same task. Python is optional for preparing exercises because ActivityStarts are already supplied.
