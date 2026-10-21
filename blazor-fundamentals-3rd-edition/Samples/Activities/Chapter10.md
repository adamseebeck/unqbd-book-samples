# Chapter 10 Activity Answer

- Load /api-demo in the browser, click Load books, and require the three sample titles. In a disposable copy, change the query URL to a nonexistent endpoint, rebuild, and require the retry message with an enabled button.
- Restore api/books. The supplied HttpCatalogQuery and ApiDemo are the answer reference; no request should reach a public test API.

## Solved Source and Execution

The exact solved code delta below applies to a complete copied target. Follow the command rather than changing canonical source.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter10`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../Checkpoints/Chapter10/LibraryDesk.Client/Models/BookItem.cs`
- `../Checkpoints/Chapter10/LibraryDesk.Client/Services/ICatalogQuery.cs`
- `../Checkpoints/Chapter10/LibraryDesk/Services/MemoryCatalogQuery.cs`
- `../Checkpoints/Chapter10/LibraryDesk.Client/Services/HttpCatalogQuery.cs`
- `../Checkpoints/Chapter10/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter10/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter10/LibraryDesk.Client/Pages/ApiDemo.razor`

## Expected Reasoning

The query owns response validation, the component owns visible request state and cancellation, and the client startup owns browser transport configuration. Each layer has a distinct test boundary.

## Verification Boundary

A browser HTTP pass requires actual browser execution, including startup assets, origin rules, and request handling. A server HTTP probe or a bUnit test does not prove those paths. Test a successful load in the browser and deliberately make the endpoint unavailable to verify recovery. The component unit test supplies a failed query to verify the UI transition independently of the transport.

Do not retry writes without an idempotency design.

Server applications commonly use IHttpClientFactory for handler lifetime and configuration. Browser HttpClient has a different transport environment. Resilience packages also need exact dependencies and a bounded policy. This small same-origin read uses a timeout and manual retry instead of teaching an unpinned Polly extension as if it were built in. Public responses remain minimal DTOs with no server secrets.
