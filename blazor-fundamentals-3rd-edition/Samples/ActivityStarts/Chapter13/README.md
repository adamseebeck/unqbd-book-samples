# Chapter 13 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this in a copy, so your LibraryDesk project stays as it is. The copy makes one book fail; you then add a book and a price column without touching BookList.

- Stop your main IDE run with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter13 to a new working folder such as C:\BookExercises\Chapter13. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio and select LibraryDesk with its http profile. In this start, SelectBook's Select method throws for book 2 before calling the callback:

```csharp
    private Task Select()
    {
        if (Id == 2) throw new InvalidOperationException();
        return Selected.InvokeAsync(Id);
    }
```

- If the debugger pauses at the activity's deliberate throw, press F5 to continue into the boundary, as in the main walkthrough.
- Open http://127.0.0.1:58631/templates. Click Select C# Field Notes: the line reads Selected identifier: 1. Click Select Blazor Field Notes: the fallback, The catalog component failed. Reload to retry., replaces the list, and the line still reads Selected identifier: 1, because the exception happened before the callback reached the page.
- Stop the copy with Shift+F5. In the copy's LibraryDesk.Client/Pages/TemplatesDemo.razor, add a third book to the books array: put a comma after the Blazor Field Notes line and add `new(3, "Database Field Notes", 26m)` below it. Run the copy again and open /templates. A Select Database Field Notes button appears below the other two; click it and the line reads Selected identifier: 3. You changed no other component.
- Stop the copy again. Change only the template so each row shows its price before the button: add `<span>@book.Price.ToString("C", CultureInfo.GetCultureInfo("en-US"))</span>` as the first line inside `<ItemTemplate Context="book">`, and add `@using System.Globalization` at the top of the page. The en-US culture prints the price in US dollars on any computer. Run the copy and open /templates: the rows read $24.00 Select C# Field Notes, $28.00 Select Blazor Field Notes and $26.00 Select Database Field Notes. BookList stays unchanged.
- Compare with Samples/Activities/Chapter13.md. The solved third book and price layout are in Samples/ActivitySolutions/Chapter13.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.
