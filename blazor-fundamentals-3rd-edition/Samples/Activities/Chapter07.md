# Chapter 7 Activity Answer

- Open /books/1, /books/2, and /books/999 directly and compare the results. Follow the two internal links and check that the title matches the URL.
- Try /books/not-an-integer and check that you get the not-found page. BookDetails.razor is the answer reference for the existing-item and missing-item branches.
- Add a second @page directive for /catalog-item/{Id:int} to BookDetails. Keep OnParametersSetAsync so either URL supplies the current identifier.
- Exercise identifiers 1, 2, 999 and a non-integer through the alias as well as the original route.

## Solved Source and Execution

The solved code change applies to a complete copy of the project; use the copy script instead of editing the original sample.

The exact code change and its copy script are in `../ActivityInvestigations/Chapter07`. Its README describes the expected result and how to start over.

## Reference Implementation

- `../Checkpoints/Chapter07/LibraryDesk.Client/Models/BookItem.cs`
- `../Checkpoints/Chapter07/LibraryDesk.Client/Services/ICatalogQuery.cs`
- `../Checkpoints/Chapter07/LibraryDesk/Services/MemoryCatalogQuery.cs`
- `../Checkpoints/Chapter07/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter07/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter07/LibraryDesk/Components/Pages/BookDetails.razor`

## Expected Reasoning

Routing converts URLs into component inputs, while layout and routing files control where the result appears. Parameter lifecycle handling and missing-item states keep the visible page consistent with its URL.

## Production Notes

The complete app registers the server catalog query in Program.cs and scans the client assembly for client routes. Removing AddAdditionalAssemblies or the Router's AdditionalAssemblies can make a route work in one entry path and fail in another. Verify direct navigation as well as clicks inside the application. The server needs to recognize browser-owned routes during initial requests.

Test direct URLs as well as navigation clicks.

Do not accept a return URL and redirect to it without checking that it is local. The local sign-in example uses LocalRedirect with a fixed path. A missing-item screen should avoid leaking private existence information in an authenticated application; the current catalog is public, so its explicit absence message is appropriate.
