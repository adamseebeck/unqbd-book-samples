# Chapter 15 activity answer

The activity replaces Virtualize with an ordinary @foreach loop, in a copy, and counts the rows in the DOM.

## What you should see

On /virtual in the copy, press F12, open the Console tab and run the line from step 4:

```javascript
document.getElementById("results").childElementCount
```

- It prints 10000: every row is in the DOM, and there are no spacers.
- Scroll to the end: the last row is 10000: Catalog title 10000.
- Your running project, with Virtualize, prints 18: 16 rows plus the two spacers.

Write down both numbers. This comparison counts elements; it isn't a timing benchmark.

## Files involved

- Samples/ActivityInvestigations/Chapter15: the copy's change and its README.
- In the copy: LibraryDesk/LibraryDesk.Client/Pages/VirtualDemo.razor, where the rows come from:

```razor
    @foreach (var book in books)
    {
        <div @key="book.Id" style="height: 40px">
            @book.Id: @book.Title
        </div>
    }
```

## Why

@foreach renders an element for every item, so the browser builds and lays out 10,000 rows even though the 300-pixel box shows about 8. Virtualize renders only the rows in view plus OverscanCount rows on each side, and two spacer elements stand in for the rest, so the count stays at 18 wherever you scroll. Fewer elements means less memory and less work for each render. Measure the resource you want to reduce, here the element count, before you claim a speed gain.
