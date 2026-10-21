# Chapter 5 Activity Answer

- Change the native filter to update only when the field loses focus, verify the difference, then restore oninput. Add a visible result count and ensure the count matches the list for empty, mixed-case, and absent searches.
- BindingDemo.razor is the reference for filtering and empty-state behavior; the count can derive from the same Matches value.

## Solved Source and Execution

Complete executable extension: `../ActivitySolutions/Chapter05`. Run and test commands are in that checkpoint's README.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter05`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../Checkpoints/Chapter05/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter05/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter05/LibraryDesk.Client/Pages/BindingDemo.razor`

## Expected Reasoning

Razor renders state, binding chooses how input updates it, and conditions explain empty outcomes. A local filter makes those relationships visible before form and network abstractions are added.

## Verification Boundary

The filter recomputes when rendered. That is appropriate for three titles, but repeatedly allocating and scanning a large collection can dominate rendering. Move expensive filtering into a deliberate state update or a query service when measurement shows a need. Do not hide stale results behind a ShouldRender override; Chapter 15 explains why that can create correctness defects.

Keep untrusted text encoded.

An oninput handler can fire many times per second. A later HTTP-backed filter should debounce input, cancel superseded requests, and prevent an older response from overwriting a newer search. The current example makes no remote calls and therefore has no retry or data-race policy to configure.
