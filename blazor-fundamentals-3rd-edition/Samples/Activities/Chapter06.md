# Chapter 6 Activity Answer

- Use the supplied favorites page as the answer reference. Add two books, repeat one Add, then use Clear.
- Verify counts of one, two, two, and zero. Navigate with a full reload and explain why the in-memory count can reset.
- Extend the service only through its methods, and confirm a second browser circuit begins with its own favorites set.
- Add a Remove C# button to FavoritesDemo using Favorites.Remove(1), without exposing its collection for mutation.
- Add both books, remove C# twice, and require one remaining favorite with identifier 2. Verify another independent circuit or scope remains empty.

## Solved Source and Execution

The exact solved code delta below applies to a complete copied target. Follow the command rather than changing canonical source.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter06`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../Checkpoints/Chapter06/LibraryDesk.Client/Services/FavoritesState.cs`
- `../Checkpoints/Chapter06/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter06/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter06/LibraryDesk.Client/Components/FavoritesSummary.razor`
- `../Checkpoints/Chapter06/LibraryDesk.Client/Components/ThemeLabel.razor`
- `../Checkpoints/Chapter06/LibraryDesk.Client/Pages/FavoritesDemo.razor`

## Expected Reasoning

The favorites example separates component, service, and cascading state. Its scope, notification, and disposal contracts explain both live updates and the limits of in-memory persistence.

## Verification Boundary

Memory grows with the number of favorites and the number of active server circuits. This exercise accepts any positive identifier to focus on state; a real write boundary must also check that the item exists and that the current user can store it. Persist authenticated favorites behind a server API with ownership checks rather than trusting a browser identifier.

Persist only the state the product needs.

Reconnection can restore an existing retained circuit, but server restart, expiry, and a newly selected Auto runtime require a recovery design. .NET 11 offers additional circuit persistence and opt-in pause features; they have serialization and hosting constraints and are not enabled by the scoped registration shown here. The source checks for this example concern the live service instance, not a distributed persistence claim.
