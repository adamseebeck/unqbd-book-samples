# Chapter 18 activity answer

The activity shows a component test catching a real bug: in the copy, FavoritesState stores its IDs in a List, which lets the same ID in twice.

## What you should see

From the copy's folder:

```powershell
dotnet test LibraryDesk.Tests --filter FavoritesRefresh
```

- The filter runs only FavoritesRefreshSiblingsAndIgnoreDuplicates, and it fails after about five seconds. The error starts with WaitForFailedException, because WaitForAssertion kept retrying until its time ran out; ignore its advice about slow machines. The real cause is the line under it that starts with ----: Assert.Equal() Failure: Strings differ, with Expected: "Favorites: 1" and Actual: "Favorites: 2". The last line starts with Failed! and reports Failed: 1, Passed: 0, Skipped: 0, Total: 1.
- After you put the original lines back, `private readonly HashSet<int> ids = [];` and `if (ids.Add(id)) Changed?.Invoke();`, the same command's last line starts with Passed! and reports Passed: 1.

## Files involved

- Samples/ActivityInvestigations/Chapter18: the copy's change and its README.
- In the copy: LibraryDesk/LibraryDesk.Client/Services/FavoritesState.cs (the bug) and LibraryDesk.Tests/ComponentTests.cs (the test that catches it).

## Why

The test clicks Favorite C# twice and expects the summary to say Favorites: 1. A HashSet ignores the second Add, and its Add returns false, so the original code raises Changed only for a real change. A List accepts the duplicate, so the count becomes 2 and the test fails with the exact text the user would see. --filter matches test names that contain FavoritesRefresh, so you get a quick answer from one test before you run the whole suite.
