# Chapter 6 activity answer

The activity adds a Remove C# button to the favorites page, in a copy, and checks how the shared FavoritesState service responds.

## What you should see

- On /favorites, click Favorite C# and Favorite Blazor: the summary shows Favorites: 2 and Saved IDs: 1, 2.
- Click Remove C#: Favorites: 1 and Saved IDs: 2. Click it again: nothing changes.
- Open /favorites in a second tab: it starts at Favorites: 0.

## Files involved

- Samples/ActivityInvestigations/Chapter06: the copy's change and its README.
- In the copy: LibraryDesk/LibraryDesk.Client/Pages/FavoritesDemo.razor, with the new button above FavoritesSummary:

```razor
<button id="remove-csharp" @onclick="() => Favorites.Remove(1)">
    Remove C#
</button>
```

- LibraryDesk/LibraryDesk.Client/Services/FavoritesState.cs and LibraryDesk/LibraryDesk.Client/Components/FavoritesSummary.razor, unchanged.

## Why

The button changes favorites only through FavoritesState's Remove method; it never touches the set directly. Remove raises Changed only when an ID was really removed, so the second click raises nothing and the summary stays as it is. FavoritesSummary listens for Changed and renders again, which is how a sibling component sees the change. The second tab starts at 0 because FavoritesState is registered as scoped: each circuit, and so each tab, gets its own instance.

The copy also has a component test, Tests/Chapter06.Tests.csproj, that checks the same counts and a second, separate scope.
