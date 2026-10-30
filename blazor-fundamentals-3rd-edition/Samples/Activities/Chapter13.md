# Chapter 13 activity answer

The activity makes SelectBook fail for one book, in a copy, then adds a third book and a price to the template without changing BookList.

## What you should see

In the copy, where SelectBook's Select method throws for book 2:

- Click Select C# Field Notes: the line reads Selected identifier: 1.
- Click Select Blazor Field Notes: the fallback, The catalog component failed. Reload to retry., replaces the list, and the line still reads Selected identifier: 1.

After you add `new(3, "Database Field Notes", 26m)` to the books array in TemplatesDemo.razor (with a comma after the Blazor Field Notes line) and run the copy again:

- A Select Database Field Notes button appears; clicking it shows Selected identifier: 3.

After you change the template to show the price:

- Each row starts with its price: $24.00 Select C# Field Notes, $28.00 Select Blazor Field Notes, $26.00 Select Database Field Notes.

## The solved template

The solved copy adds `@using System.Globalization` at the top of the page and one line at the start of the ItemTemplate:

```razor
<ItemTemplate Context="book">
    <span>@book.Price.ToString("C", CultureInfo.GetCultureInfo("en-US"))</span>
    <SelectBook Id="book.Id" Title="@book.Title"
                Selected="Choose" />
</ItemTemplate>
```

## Files involved

- Samples/ActivityInvestigations/Chapter13: the copy with the failing Select method, and its README.
- In the copy: LibraryDesk/LibraryDesk.Client/Components/SelectBook.razor (the throw) and LibraryDesk/LibraryDesk.Client/Pages/TemplatesDemo.razor (your third book and price).
- Samples/ActivitySolutions/Chapter13: the solved third book and price layout, without the throw. From that folder, dotnet test Tests/Chapter13.Tests.csproj runs its test; the last line reports Passed: 1.
- Unchanged: LibraryDesk/LibraryDesk.Client/Components/BookList.razor.

## Why

The exception happens inside the ErrorBoundary, during an event in a child component, before the callback reaches TemplatesDemo. The boundary replaces its content with the fallback, and the status line, which sits outside the boundary, keeps its last value. The rest of the page and the circuit keep working.

BookList knows nothing about books: it loops over Items and renders ItemTemplate for each one. The page owns the data and the row layout, so a third book or a new column needs no change to the list component. The en-US culture makes the price print in US dollars on any machine.
