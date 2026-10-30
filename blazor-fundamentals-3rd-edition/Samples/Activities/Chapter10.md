# Chapter 10 activity answer

The activity makes HttpCatalogQuery ask for an address that doesn't exist, in a copy, and watches the page recover.

## What you should see

Open http://127.0.0.1:58631/api-demo in the copy, open the Network tab and click Load books:

- The Network tab shows a row named not-present with status 404.
- The page shows The catalog is unavailable. Try again.
- The Load books button is active again, so you can retry.

Your own project still asks for api/books and loads three books.

## Files involved

- Samples/ActivityInvestigations/Chapter10: the copy's change and its README.
- In the copy: LibraryDesk/LibraryDesk.Client/Services/HttpCatalogQuery.cs, where the request goes to api/not-present.
- Unchanged: LibraryDesk/LibraryDesk.Client/Pages/ApiDemo.razor (the page and its catch blocks) and the GET /api/books endpoint in LibraryDesk/LibraryDesk/Program.cs.

## Why

The server has no endpoint at /api/not-present, so it answers 404. EnsureSuccessStatusCode turns any status outside 200 to 299 into an HttpRequestException. ApiDemo's first catch handles only the cancellation that happens when you leave the page, so the second catch, for catalog failures, handles the exception: it clears the list and shows the retry message. The finally block sets loading back to false, which enables the button. The query reports failures; the page decides what the visitor sees.

The copy also has a component test, Tests/Chapter10.Tests.csproj, that gives ApiDemo a stand-in HTTP handler answering 404 and checks the path, the message and the enabled button.
