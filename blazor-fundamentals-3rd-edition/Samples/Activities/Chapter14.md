# Chapter 14 Activity Answer

- Replace the card title with a long unbroken identifier and verify that it wraps without horizontal page overflow. Use browser zoom and a narrow viewport, then restore the normal title.
- The supplied BookCard and stylesheet are the answer reference for local isolation, wrapping, and conditional width. Note the viewport and zoom you used; emulation isn't a physical-device test.

## Solved Source and Execution

The solved code change applies to a complete copy of the project; use the copy script instead of editing the original sample.

The exact code change and its copy script are in `../ActivityInvestigations/Chapter14`. Its README describes the expected result and how to start over.

## Reference Implementation

- `../Checkpoints/Chapter14/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter14/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter14/LibraryDesk.Client/Components/BookCard.razor`
- `../Checkpoints/Chapter14/LibraryDesk.Client/Components/BookCard.razor.css`
- `../Checkpoints/Chapter14/LibraryDesk.Client/Pages/StyleDemo.razor`

## Expected Reasoning

Global host links, isolated component styles, and content-aware reflow work together. A static card gives a focused way to verify the asset pipeline without adding an interactive runtime.

## Production Notes

Local assets reduce third-party dependencies and make offline and privacy behavior easier to reason about. If a team adopts a CSS framework, pin its version and compile production assets through a repeatable toolchain. A playground CDN script is not the production Tailwind workflow. Do not fetch an unpinned latest stylesheet during every page load and call the result reproducible.

Verify the generated CSS in the served page.

Responsive checks need real content. Long titles, translated text, zoom, and validation messages often reveal problems that a short placeholder misses. CSS isolation prevents some selector collisions, but it doesn't give you contrast, keyboard usability or semantic structure. Chapter 21 applies those additional checks to a form.
