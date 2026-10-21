# Chapter 17 Activity Answer

- Use the complete Catalog page and its existing services as the answer reference. Filter for Blazor, favorite it twice, clear the filter, and favorite C#.
- Check that the current-runtime count is two. Follow /books/2 and verify the same title.
- Reload and revisit, noting which renderer runs and explaining any reset. The UI should never say the favorites are saved.
- Add a Clear catalog favorites button and handler to Catalog that calls Favorites.Clear and updates the current-runtime status.
- Repeat the original two-book scenario, clear twice and check for zero state and a zero status, without adding durable storage.

## Solved Source and Execution

The solved code change applies to a complete copy of the project; use the copy script instead of editing the original sample.

The exact code change and its copy script are in `../ActivityInvestigations/Chapter17`. Its README describes the expected result and how to start over.

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

## Production Notes

The catalog data is public, so it can safely appear in prerendered HTML and downloadable browser responses. A private catalog needs authorization at the query or endpoint boundary as well as page presentation. The capstone doesn't configure a client authentication-state provider, and Chapter 11's server security examples don't cover browser authentication.

Never perform a write as an initialization side effect.

Catch expected transport and data-format failures without exposing internal exception text. Cancellation and missing data need distinct design choices if the source becomes a real backend. The sample keeps a short public dataset and no background refresh, so it has no conflict resolution, offline synchronization or persistence across machines.
