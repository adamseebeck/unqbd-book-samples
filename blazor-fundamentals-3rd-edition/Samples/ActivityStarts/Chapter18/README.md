# Chapter 18 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this activity in a copy, so your LibraryDesk project stays as it is. The task is to see a test catch a real bug, then fix it: FavoritesState stores its IDs in a HashSet, which ignores duplicates, and in the copy a List lets the same ID in twice.

- Stop your main run with Shift+F5. Copy Samples/ActivityStarts/Chapter18 in File Explorer to a new working folder, such as C:\BookExercises\Chapter18. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio. The prepared start replaces FavoritesState's HashSet with a List. This exercise runs tests, so no startup browser is needed.
- In the copy, LibraryDesk.Client/Services/FavoritesState.cs has the bug: ids is a List<int>, and Add appends and raises Changed every time.
- Build the solution, then open Test Explorer and search for FavoritesRefreshSiblingsAndIgnoreDuplicates. Right-click that test and choose Run. It fails after WaitForAssertion times out. Select it and read the details: the assertion expected Favorites: 1 but received Favorites: 2. The second click added the same ID again. A failed test is different from a compilation error.
- Fix the bug: restore private readonly HashSet<int> ids = []; and change Add to if (ids.Add(id)) Changed?.Invoke();. Save, then run the same selected test again. Its result changes to Passed. Clear the search and run the project's complete suite: all 18 tests should pass.
- Compare your results with Samples/Activities/Chapter18.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.
