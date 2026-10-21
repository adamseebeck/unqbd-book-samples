# Chapter 10 Activity Answer

- Load /api-demo in the browser, click Load books, and check for the three sample titles. In a disposable copy, change the query URL to a nonexistent endpoint, rebuild, and check for the retry message with an enabled button.
- Restore api/books. The supplied HttpCatalogQuery and ApiDemo are the answer reference; no request should reach a public test API.

## Solved Source and Execution

The solved code change applies to a complete copy of the project; use the copy script instead of editing the original sample.

The exact code change and its copy script are in `../ActivityInvestigations/Chapter10`. Its README describes the expected result and how to start over.

## Reference Implementation

- `../Checkpoints/Chapter10/LibraryDesk.Client/Models/BookItem.cs`
- `../Checkpoints/Chapter10/LibraryDesk.Client/Services/ICatalogQuery.cs`
- `../Checkpoints/Chapter10/LibraryDesk/Services/MemoryCatalogQuery.cs`
- `../Checkpoints/Chapter10/LibraryDesk.Client/Services/HttpCatalogQuery.cs`
- `../Checkpoints/Chapter10/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter10/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter10/LibraryDesk.Client/Pages/ApiDemo.razor`

## Expected Reasoning

The query owns response validation, the component owns visible request state and cancellation, and the client startup owns browser transport configuration. Each layer can be tested on its own.

## Production Notes

Test browser HTTP in a real browser: startup assets, origin rules and request handling only run there, so a server-side request or a bUnit test can't cover them. Load the data successfully in the browser, then make the endpoint unavailable to check recovery. The component unit test supplies a failed query to check the UI transition without any transport.

Do not retry writes without an idempotency design.

Server applications commonly use IHttpClientFactory for handler lifetime and configuration. Browser HttpClient has a different transport environment. Resilience packages also need exact dependencies and a bounded policy. This small same-origin read uses a timeout and a manual retry; add a resilience library such as Polly only as an explicit, versioned dependency. Public responses remain minimal DTOs with no server secrets.
