# Chapter 22 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this in a copy, so your LibraryDesk project stays as it is. The task is to quiet the CatalogRead entries with configuration alone, without changing any code.

- Stop your main run with Shift+F5. Copy Samples/ActivityStarts/Chapter22 in File Explorer to a new working folder such as C:\BookExercises\Chapter22. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio. Select LibraryDesk with its http profile. Chapter22.Tests is the separate exercise test project.
- In the copy's LibraryDesk/appsettings.json, add a line to the LogLevel section that sets the category LibraryDesk.Components.CatalogReadRecorder to Warning. Add the same line to LibraryDesk/LibraryDesk/appsettings.Development.json, because a Development run reads both files.
- Run the copy with F5, open http://127.0.0.1:58631/diagnostics and click twice. The page shows Reads recorded: 2, but Debug output shows no CatalogRead entries: the category now starts at Warning, and the entries are Information.
- Stop debugging and build the copy. In Test Explorer, right-click Chapter22.Tests and choose Run. Its one test reads both appsettings files, clicks twice, and checks that the count reaches 2 without a CatalogRead entry. It should pass.
- Change "Warning" back to "Information" in both files, run the copy again and click: the CatalogRead entries return.
- Compare your change with Samples/Activities/Chapter22.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.

## Optional generator

Instead of copying the prepared start, open PowerShell in Samples/ActivityInvestigations and run:

```powershell
python create_copy.py --chapter 22 --output C:/BookExercises/Chapter22
```

Choose an output path that does not exist. The script copies the checkpoint, verifies and applies its checked activity changes, adds the exercise tests and includes them in LibraryDesk.slnx. It never edits the download. Open that generated solution and follow the same task. Python is optional for preparing exercises because ActivityStarts are already supplied.
