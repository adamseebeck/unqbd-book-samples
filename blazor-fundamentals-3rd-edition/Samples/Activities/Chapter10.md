# Chapter 10 activity answer

The activity makes HttpCatalogQuery ask for an address that doesn't exist, in a copy, watches the page recover, and then repairs the address.

## The change

In the copy's LibraryDesk/LibraryDesk.Client/Services/HttpCatalogQuery.cs, the request line becomes:

```csharp
            await http.GetAsync("api/not-present", cancellationToken);
```

At the end you change "api/not-present" back to "api/books".

## What you should see

Open http://127.0.0.1:58631/api-demo in the copy, open the Network tab and click Load books:

- The Network tab shows a row named not-present with status 404.
- The page shows The catalog is unavailable. Try again.
- The Load books button is active again, so you can retry.

After you put "api/books" back, run the copy again and press F5, Load books shows Loaded 3 books. and the three titles. Without F5 the page keeps the old client code and asks for api/not-present again.

## Files involved

- Samples/ActivityInvestigations/Chapter10: the copy's README and its test. The copy is Samples/Checkpoints/Chapter10 unchanged.
- In the copy: LibraryDesk/LibraryDesk.Client/Services/HttpCatalogQuery.cs, the one file you change.
- Unchanged: LibraryDesk/LibraryDesk.Client/Pages/ApiDemo.razor (the page and its catch blocks) and the GET /api/books endpoint in LibraryDesk/LibraryDesk/Program.cs.

## Why

The server has no endpoint at /api/not-present, so it answers 404. EnsureSuccessStatusCode turns any status outside 200 to 299 into an HttpRequestException. ApiDemo's first catch handles only the cancellation that happens when you leave the page, so the second catch, for catalog failures, handles the exception: it clears the list and shows the retry message. The finally block sets loading back to false, which enables the button. The query reports failures; the page decides what the visitor sees.

The copy also has a component test, Tests/Chapter10.Tests.csproj, that gives ApiDemo a stand-in HTTP handler answering 404 and checks the path, the message and the enabled button. It passes while the address is api/not-present.
