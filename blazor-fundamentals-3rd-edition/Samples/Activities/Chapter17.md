# Chapter 17 Activity Answer

- Use the complete Catalog page and its existing services as the answer reference. Filter for Blazor, favorite it twice, clear the filter, and favorite C#.
- Require a current-runtime count of two. Follow /books/2 and verify the same title.
- Reload and revisit while recording renderer identity and explaining any reset. No durable-save claim should appear in the UI.
- Add a Clear catalog favorites button and handler to Catalog that calls Favorites.Clear and updates the current-runtime status.
- Repeat the original two-book scenario, clear twice and require zero state and zero status without adding durable storage.

## Solved Source and Execution

The exact solved code delta below applies to a complete copied target. Follow the command rather than changing canonical source.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter17`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../Checkpoints/Chapter17/LibraryDesk.Client/Models/BookItem.cs`
- `../Checkpoints/Chapter17/LibraryDesk.Client/Services/ICatalogQuery.cs`
- `../Checkpoints/Chapter17/LibraryDesk/Services/MemoryCatalogQuery.cs`
- `../Checkpoints/Chapter17/LibraryDesk.Client/Services/FavoritesState.cs`
- `../Checkpoints/Chapter17/LibraryDesk.Client/Services/HttpCatalogQuery.cs`
- `../Checkpoints/Chapter17/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter17/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter17/LibraryDesk.Client/Pages/Catalog.razor`
- `../Checkpoints/Chapter17/LibraryDesk/Components/Pages/BookDetails.razor`

## Expected Reasoning

The capstone checks continuity: one domain model, stable routes, runtime-specific query implementations, and honest state lifetimes. Its renderer label and explicit local outcomes keep the integration testable.

## Verification Boundary

The catalog data is public, so it can safely appear in prerendered HTML and downloadable browser responses. A private catalog needs authorization at the query or endpoint boundary as well as page presentation. The capstone does not configure a client authentication-state provider, and the server security laboratory does not prove browser authentication behavior.

Never perform a write as an initialization side effect.

Catch expected transport and data-format failures without exposing internal exception text. Cancellation and missing data need distinct design choices if the source becomes a real backend. The sample retains a short public dataset and no background refresh, so it does not claim conflict resolution, offline synchronization, or persistence across machines.
