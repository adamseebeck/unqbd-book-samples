# Chapter 14 activity answer

The activity gives the book card a very long title with no spaces, in a copy, and checks that the card's scoped stylesheet keeps it inside the border on a narrow screen.

## What you should see

On /style in the copy, with device emulation (Ctrl+Shift+M) at a width of 360:

- The long title wraps inside the card's border, the card grows taller, and the page doesn't scroll sideways.
- At 200 percent zoom (Ctrl+Plus) the title still wraps inside the card.

After you delete `overflow-wrap: anywhere;` from BookCard.razor.css and run the copy again:

- The title runs past the border and the page scrolls sideways. Put the line back.

Note the width and zoom you used; the result is the same at any narrow width.

## Files involved

- Samples/ActivityInvestigations/Chapter14: the copy's change and its README.
- In the copy: LibraryDesk/LibraryDesk.Client/Pages/StyleDemo.razor (the long title) and LibraryDesk/LibraryDesk.Client/Components/BookCard.razor.css (the line you delete and restore).

## Why

A browser breaks text only at spaces and hyphens unless told otherwise. A word longer than its box overflows the box and widens the page. overflow-wrap: anywhere allows a break at any point when no better break exists, so the card's height grows instead of the page's width. The rule lives in the component's scoped stylesheet, so it applies to every BookCard and to nothing else. Zoom and device emulation shrink the space in CSS pixels the same way a phone does, but they don't replace a check on a real phone.
