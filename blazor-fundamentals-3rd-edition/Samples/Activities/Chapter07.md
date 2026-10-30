# Chapter 7 activity answer

The activity first watches typed and clicked navigation in your running project, then uses a copy to make a page keep its instance while its parameter changes, and adds a second route template.

## What you should see

In your running project, with the Network tab open:

- Typing http://127.0.0.1:58631/books/2 and pressing Enter adds a row of type document. Each click on C# or Blazor adds a row of type fetch. The title always matches the number in the address.

In the copy, which makes the Router interactive and loads the book in OnInitializedAsync:

- On /books/1, click Blazor. The address changes to /books/2, but the page still shows C# Field Notes and Price: $24.00.
- Press F5: Blazor Field Notes appears.
- With OnInitializedAsync changed back to OnParametersSetAsync, clicking Blazor shows Blazor Field Notes.
- With `@page "/catalog-item/{Id:int}"` added under the first @page line, /catalog-item/2 shows Blazor Field Notes, /catalog-item/999 shows No book has this identifier. and /catalog-item/abc shows Not Found.

## Files involved

- Samples/ActivityInvestigations/Chapter07: the copy's two changes and its README.
- In the copy: LibraryDesk/LibraryDesk/Components/App.razor (`<Routes @rendermode="InteractiveServer" />`) and LibraryDesk/LibraryDesk/Components/Pages/BookDetails.razor (OnInitializedAsync, then your fix and the second @page line).
- Samples/Checkpoints/Chapter07: the project the copy starts from, where BookDetails is a static page.

## Why

In your running project BookDetails is a static page, so every request, typed or clicked, creates a new instance and the book always matches the address. The copy's interactive Router handles clicks over the circuit instead. When only the Id parameter changes, Blazor keeps the instance and sets the new parameter on it. OnInitializedAsync runs once per instance, so it keeps the first book; OnParametersSetAsync runs every time parameters are set, so it loads the right one. A full load (F5) always creates a new instance, which is why the stale title disappears.

A page can have several route templates. The :int constraint checks the segment's shape: abc doesn't match any route, so the Not Found page appears, while 999 matches and BookDetails shows its own message for a missing book.
