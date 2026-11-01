# Chapter 6 activity answer

Use a copy of Samples/ActivityStarts/Chapter06 and open its LibraryDesk/LibraryDesk.slnx in Visual Studio. F5 starts the selected app; Shift+F5 stops that debug run. The chapter gives the task, and this file explains its answer.

The activity has you add a Remove C# button to the favorites page, in a copy, and check how the shared FavoritesState service responds.

## The change

In the copy's LibraryDesk/LibraryDesk.Client/Pages/FavoritesDemo.razor, the new button goes above FavoritesSummary:

```razor
<button @onclick="() => Favorites.Remove(1)">Remove C#</button>
<FavoritesSummary />
```

It is written the same way as the Favorite buttons, with Remove in place of Add. No other file changes.

## What you should see

- On /favorites, the page shows Favorite C#, Favorite Blazor and Remove C# above the summary.
- Click Favorite C# and Favorite Blazor: the summary shows Favorites: 2 and Saved IDs: 1, 2.
- Click Remove C#: Favorites: 1 and Saved IDs: 2. Click it again: nothing changes.
- Open /favorites in a second tab: it starts at Favorites: 0.
- Stop debugging, build and run Chapter06.Tests in Test Explorer. Its one test passes after Remove C# is implemented; before that it fails because the button is missing. A test failure is different from a compiler error.

## Files involved

- Samples/ActivityInvestigations/Chapter06: the copy's README and its test.
- In the copy: LibraryDesk/LibraryDesk.Client/Pages/FavoritesDemo.razor, the page you edit.
- LibraryDesk/LibraryDesk.Client/Services/FavoritesState.cs and LibraryDesk/LibraryDesk.Client/Components/FavoritesSummary.razor, unchanged.

## Why

The button changes favorites only through FavoritesState's Remove method; it never touches the set directly. Remove raises Changed only when an ID was really removed, so the second click raises nothing and the summary stays as it is. FavoritesSummary listens for Changed and renders again, which is how a sibling component sees the change. The second tab starts at 0 because FavoritesState is registered as scoped: each circuit, and so each tab, gets its own instance.

The test renders the page twice, in two separate service scopes, clicks your button in one of them, and checks that only that summary changed.
