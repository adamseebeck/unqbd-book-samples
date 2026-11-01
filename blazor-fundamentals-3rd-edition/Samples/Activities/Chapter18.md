# Chapter 18 activity answer

Use a copy of Samples/ActivityStarts/Chapter18 and open its LibraryDesk/LibraryDesk.slnx in Visual Studio. F5 starts the selected app; Shift+F5 stops that debug run. The chapter gives the task, and this file explains its answer.

The activity shows a component test catching a real bug, and you fix it: in the copy, FavoritesState stores its IDs in a List, which lets the same ID in twice.

## What you should see

Build, open Test Explorer and search for FavoritesRefreshSiblingsAndIgnoreDuplicates. Right-click that test and choose Run.

- The unsolved List lets the second Favorite C# click count twice. The test fails after WaitForAssertion times out. Its details show the assertion's Expected: Favorites: 1 and Actual: Favorites: 2. This is a test failure, not a compilation error.
- Restore private readonly HashSet<int> ids = []; and if (ids.Add(id)) Changed?.Invoke();. Save and run the selected test again: Passed.
- Clear the search and run LibraryDesk.Tests: all 18 tests pass.

## Files involved

- Samples/ActivityInvestigations/Chapter18: the copy's change and its README.
- In the copy: LibraryDesk/LibraryDesk.Client/Services/FavoritesState.cs (the bug and your fix) and LibraryDesk.Tests/ComponentTests.cs (the test that catches it).

## Why

The test clicks Favorite C# twice and expects the summary to say Favorites: 1. A HashSet ignores the second Add, and its Add returns false, so the original code raises Changed only for a real change. A List accepts the duplicate, so the count becomes 2 and the test fails with the exact text the user would see. Selecting this test in Test Explorer gives you a quick answer before you run the complete suite. The command-line alternative is dotnet test LibraryDesk.Tests --filter FavoritesRefresh.
