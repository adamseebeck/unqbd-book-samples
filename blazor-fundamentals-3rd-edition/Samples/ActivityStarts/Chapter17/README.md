# Chapter 17 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this activity in a copy, so your LibraryDesk project stays as it is. The task is to add a Clear catalog favorites button to Catalog that empties FavoritesState and updates the status.

- Stop your main IDE run with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter17 to a new working folder such as C:\BookExercises\Chapter17. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio and select LibraryDesk with its http profile. It contains the chapter checkpoint and an exercise Tests project.
- In the copy's LibraryDesk.Client/Pages/Catalog.razor, add a button just above the status line, with the id clear-catalog-favorites and the text Clear catalog favorites, that calls a new ClearFavorites method. Write ClearFavorites above Add: it calls Favorites.Clear() and then sets status the same way Add does. FavoritesState has had its Clear method since Chapter 6.
- Run the copy with F5 using the http profile and open http://127.0.0.1:58631/catalog. Favorite Blazor Field Notes and C# Field Notes: the status says Favorites in this runtime: 2. Click Clear catalog favorites: Favorites in this runtime: 0. Click it again: still 0. Press Shift+F5 when you're done.
- Check your work with the copy's test. In Test Explorer, right-click Chapter17.Tests and choose Run. The test finds your button by its id, favorites two books, clicks Clear twice and checks the status. Expect one passed exercise test. If the id is different, the test fails with ElementNotFoundException.
- Compare your code with Samples/Activities/Chapter17.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.
