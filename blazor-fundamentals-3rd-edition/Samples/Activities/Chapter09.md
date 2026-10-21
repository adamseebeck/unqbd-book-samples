# Chapter 9 Activity Answer

- Replace the server query in a disposable copy with a deterministic implementation returning two different book records. Keep the interface and component unchanged, build, and require the displayed count and titles to change.
- The supplied MemoryCatalogQuery is the reference structure. Explain why making FavoritesState a singleton would be an unrelated and unsafe change.

## Solved Source and Execution

Complete executable extension: `../ActivitySolutions/Chapter09`. Run and test commands are in that checkpoint's README.

## Reference Implementation

- `../Checkpoints/Chapter09/LibraryDesk.Client/Models/BookItem.cs`
- `../Checkpoints/Chapter09/LibraryDesk.Client/Services/ICatalogQuery.cs`
- `../Checkpoints/Chapter09/LibraryDesk/Services/MemoryCatalogQuery.cs`
- `../Checkpoints/Chapter09/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter09/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter09/LibraryDesk.Client/Pages/ServicesDemo.razor`

## Expected Reasoning

The component depends on a query contract, while each runtime supplies an implementation with a suitable lifetime. The public stateless query and scoped favorites service illustrate different ownership requirements.

## Verification Boundary

A service boundary is useful when it hides a real responsibility. Avoid adding interfaces for every arithmetic helper merely to imitate architecture. The catalog contract earns its place because server memory and browser HTTP are different implementations of the same reader-facing operation. Tests can fail that operation without hosting a remote website.

Keep browser configuration public by design.

An injected service can outlive the component using it. Dispose resources at their actual owning boundary and avoid making a service own an element reference or a renderer callback indefinitely. For database-backed components, short-lived per-operation contexts often fit better than a circuit-long context. This book's in-memory query is not evidence that a database provider's concurrency behavior was tested.
