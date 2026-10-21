# Chapter 12 Activity Answer

- Use the supplied module and component as the answer reference. Focus the input by keyboard activation of the button, request the browser time, navigate away, and return.
- Require a fresh working instance without duplicated callbacks. In a disposable copy, break the module path and verify the visible import-failure state, then restore the correct path.

## Solved Source and Execution

The exact solved code delta below applies to a complete copied target. Follow the command rather than changing canonical source.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter12`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../Checkpoints/Chapter12/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter12/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter12/LibraryDesk.Client/wwwroot/js/catalog.js`
- `../Checkpoints/Chapter12/LibraryDesk.Client/Pages/InteropDemo.razor`

## Expected Reasoning

The module, element, callback reference, and component each have a lifetime. Placing work after render and disposing the references keeps those lifetimes aligned.

## Verification Boundary

An interop boundary serializes values and can be expensive when invoked repeatedly. Batch related work instead of calling JavaScript once per cell in a large table. Keep the JavaScript contract small and explicit. Do not pass a broad component object into JavaScript when a narrow value or method reference is enough.

Dispose every object reference you create.

DOM cleanup after a node disappears can require a JavaScript MutationObserver rather than a late server interop call. This example only owns a module and instance reference, so its disposal does not attempt to manipulate removed elements. A disconnected page cannot be used as proof that browser cleanup ran successfully; validate the behavior in the actual host being taught.
