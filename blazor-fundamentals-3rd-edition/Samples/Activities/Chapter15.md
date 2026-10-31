# Chapter 15 activity answer

The activity starts from a copy where VirtualDemo renders every row with an ordinary @foreach loop. You count its elements, then bring Virtualize back with more overscan and count again.

## What you should see

On /virtual in the copy, press F12, open the Console tab and run the line from step 4:

```javascript
document.getElementById("results").childElementCount
```

- As the copy is created, it prints 10000: every row is in the DOM, and there are no spacers. The last row is 10000: Catalog title 10000.
- Your own project printed 18 in step 4: 16 rows plus the two spacers.

After you put Virtualize back with OverscanCount="10" and run the copy again:

- The line prints 32 at the top: 30 rows and the two spacers.
- Drag the scroll bar to the middle and run it again: still 32.

Write down the numbers. This comparison counts elements; it isn't a timing benchmark.

## The solved loop

```razor
<Virtualize Items="books" Context="book" ItemSize="40"
            OverscanCount="10">
    <div @key="book.Id" style="height: 40px">
        @book.Id: @book.Title
    </div>
</Virtualize>
```

## Files involved

- Samples/ActivityInvestigations/Chapter15: the copy's change and its README.
- In the copy: LibraryDesk/LibraryDesk.Client/Pages/VirtualDemo.razor.

## Why

@foreach renders an element for every item, so the browser builds and lays out 10,000 rows even though the 300-pixel box shows about 8. Virtualize renders only the rows near the visible area plus OverscanCount rows on each side, and two spacer elements stand in for the rest. Raising OverscanCount from 3 to 10 adds 7 rows on each side, 14 in all, so the count goes from 18 to 32, and it stays the same wherever you scroll. More overscan means fewer blank moments during fast scrolling, at the cost of more elements. Measure the resource you want to reduce, here the element count, before you claim a speed gain.
