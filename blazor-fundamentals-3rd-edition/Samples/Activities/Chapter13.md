# Chapter 13 Activity Answer

- Add a third BookItem and select it through the existing SelectBook component. Check that the parent status shows its numeric identifier.
- Replace only the row fragment with a title and price layout while leaving BookList unchanged. The supplied files are the answer reference for typed composition and parent-owned selection.

## Solved Source and Execution

Complete executable extension: `../ActivitySolutions/Chapter13`. Run and test commands are in that checkpoint's README.

## Reference Implementation

- `../Checkpoints/Chapter13/LibraryDesk.Client/Models/BookItem.cs`
- `../Checkpoints/Chapter13/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter13/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter13/LibraryDesk.Client/Components/BookList.razor`
- `../Checkpoints/Chapter13/LibraryDesk.Client/Components/SelectBook.razor`
- `../Checkpoints/Chapter13/LibraryDesk.Client/Pages/TemplatesDemo.razor`

## Expected Reasoning

A typed fragment controls row markup, a typed callback reports the selection, and a local error boundary contains unexpected failures. Clear ownership removes the need for most imperative references.

## Production Notes

Templates execute rendering logic supplied by the parent, so keep the contract narrow and avoid passing arbitrary executable fragments across a render-mode boundary. Delegate-like render content cannot be serialized as an ordinary parameter into a different runtime. Compose the BookList and SelectBook inside one interactive island.

Keep template composition inside one runtime boundary.

The example deliberately avoids a custom modal. A usable modal requires focus movement, focus containment, escape behavior, and a return-focus strategy; a visual overlay with a Close button is incomplete. Build and test those behaviors if the product requires a dialog, rather than advertising a decorative overlay as an accessible modal system.
