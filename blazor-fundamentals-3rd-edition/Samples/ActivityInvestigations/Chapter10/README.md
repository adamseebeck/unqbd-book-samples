# Chapter 10 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this in a copy, so your LibraryDesk project stays as it is. You point HttpCatalogQuery at an address that doesn't exist, watch the page recover, and then repair it.

- Stop your main IDE run with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter10 to a new working folder such as C:\BookExercises\Chapter10. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio and select LibraryDesk with its http profile. It contains the unchanged checkpoint and an exercise Tests project.
- In the copy's LibraryDesk.Client/Services/HttpCatalogQuery.cs, change "api/books" to "api/not-present" in the GetAsync call and save the file. Then save and press F5.
- Open http://127.0.0.1:58631/api-demo, open the Network tab and click Load books. You should see a row named not-present with status 404, the text The catalog is unavailable. Try again., and an active Load books button. EnsureSuccessStatusCode turned the 404 into an HttpRequestException, and the page's second catch handled it.
- Stop the copy with Shift+F5, change "api/not-present" back to "api/books", and run the copy again. The open page still runs the client code it downloaded before, so press F5 to load the new code. Then click Load books: the text reads Loaded 3 books. and the three titles appear.
- Compare with Samples/Activities/Chapter10.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.

## Optional generator

Instead of copying the prepared start, open PowerShell in Samples/ActivityInvestigations and run:

```powershell
python create_copy.py --chapter 10 --output C:/BookExercises/Chapter10
```

Choose an output path that does not exist. The script copies the checkpoint, verifies and applies its checked activity changes, adds the exercise tests and includes them in LibraryDesk.slnx. It never edits the download. Open that generated solution and follow the same task. Python is optional for preparing exercises because ActivityStarts are already supplied.
