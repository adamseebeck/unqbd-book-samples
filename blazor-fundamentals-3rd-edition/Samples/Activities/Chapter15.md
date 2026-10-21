# Chapter 15 Activity Answer

- Compare a disposable ordinary foreach version with the supplied VirtualDemo using the same ten thousand records. Record the rendered row counts before and after scrolling.
- Restore Virtualize and verify later identifiers appear. The supplied fixed-height implementation is the answer reference.
- Do not call the experiment a latency benchmark without controlled timing and a representative workload.

## Solved Source and Execution

The exact solved code delta below applies to a complete copied target. Follow the command rather than changing canonical source.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter15`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../Checkpoints/Chapter15/LibraryDesk.Client/Models/BookItem.cs`
- `../Checkpoints/Chapter15/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter15/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter15/LibraryDesk.Client/Pages/VirtualDemo.razor`

## Expected Reasoning

Virtualize reduces visible DOM work while the local data remains allocated. Render guards, paging, AOT, and streaming solve separate problems and require separate evidence.

## Verification Boundary

Uniform row height simplifies the virtualizer's estimates. Rich rows that expand, wrap unpredictably, or load images later need careful measurement and possibly a different layout. Stable keys preserve row identity, but they do not fix a wrong total count or a provider that returns stale pages. Cancellation matters when scrolling triggers superseded data requests.

Measure the resource you intend to reduce.

The .NET 11 Virtualize API also adds indexed positioning and scrolling support; verify those calls against the exact release if you use them. The baseline shown here uses established Items behavior to keep the core experiment narrow. Its evidence concerns functional windowing, not production latency, memory savings for the dataset, or WebAssembly AOT execution.
