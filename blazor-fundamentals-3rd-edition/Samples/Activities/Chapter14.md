# Chapter 14 activity answer

Use a copy of Samples/ActivityStarts/Chapter14 and open its LibraryDesk/LibraryDesk.slnx in Visual Studio. F5 starts the selected app; Shift+F5 stops that debug run. The chapter gives the task, and this file explains its answer.

The activity gives the book card a very long title with no spaces and takes away the rule that handles it, in a copy. You write the rule back into the card's scoped stylesheet.

## What you should see

On /style in the copy, with device emulation (Ctrl+Shift+M) at a width of 360:

- As the copy is created, the long title runs past the card's right border and the page scrolls sideways.

After you add `overflow-wrap: anywhere;` to the `.book-card` rule in BookCard.razor.css and run the copy again:

- The title wraps inside the card's border, the card grows taller, and the page doesn't scroll sideways.
- At 200 percent zoom (Ctrl+Plus, with the page focused) the title still wraps inside the card.

Note the width and zoom you used; the result is the same at any narrow width.

## The solved rule

```css
.book-card {
    padding: 1rem;
    border: 1px solid #526474;
    border-radius: 0.4rem;
    overflow-wrap: anywhere;
}
```

## Files involved

- Samples/ActivityInvestigations/Chapter14: the copy's changes and its README.
- In the copy: LibraryDesk/LibraryDesk.Client/Pages/StyleDemo.razor (the long title) and LibraryDesk/LibraryDesk.Client/Components/BookCard.razor.css (the line you write).

## Why

A browser breaks text only at spaces and hyphens unless told otherwise. A word longer than its box overflows the box and widens the page. overflow-wrap: anywhere allows a break at any point when no better break exists, so the card's height grows instead of the page's width. The rule lives in the component's scoped stylesheet, so it applies to every BookCard and to nothing else. Zoom and device emulation shrink the space in CSS pixels the same way a phone does, but they don't replace a check on a real phone.
