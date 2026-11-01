# Chapter 17 activity answer

The task is a Clear catalog favorites button on /catalog that empties FavoritesState and updates the status. Here is one way to write it; compare it with yours.

## The change

In LibraryDesk/LibraryDesk.Client/Pages/Catalog.razor, the button sits just above the status line:

```razor
<button id="clear-catalog-favorites" @onclick="ClearFavorites">
    Clear catalog favorites
</button>
<p role="status">@status</p>
```

and the method sits above Add:

```csharp
    private void ClearFavorites()
    {
        Favorites.Clear();
        status =
            $"Favorites in this runtime: {Favorites.Ids.Count}.";
    }
```

## What you should see

- Open http://127.0.0.1:58631/catalog. Favorite Blazor Field Notes and C# Field Notes: the status says Favorites in this runtime: 2.
- Click Clear catalog favorites: Favorites in this runtime: 0.
- Click it again: still 0.
- From the copy's folder, dotnet test Tests/Chapter17.Tests.csproj favorites two books, clicks your button twice and checks the service and the status. The output reads Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0, and the last line starts with Build succeeded. If your button has a different id, the test fails with ElementNotFoundException.

## Files involved

- Samples/ActivityInvestigations/Chapter17: the README, delta.json and Tests folder that create_copy.py uses. The copy itself is Samples/Checkpoints/Chapter17, unchanged.
- In the copy: LibraryDesk/LibraryDesk.Client/Pages/Catalog.razor (your change).
- Unchanged: LibraryDesk/LibraryDesk.Client/Services/FavoritesState.cs, whose Clear method already exists.

## Why

The page changes favorites only through FavoritesState's methods, as every other page does, and then writes the status from the service's own count, so the text always matches the real state. Clear raises Changed only when there was something to clear, and the second click finds nothing, so the count stays at 0. The status says "in this runtime" because Catalog runs in Interactive Auto: in Server mode the favorites live in the circuit's FavoritesState, in WebAssembly mode in the browser tab's, and a reload or a new runtime starts again at 0.
