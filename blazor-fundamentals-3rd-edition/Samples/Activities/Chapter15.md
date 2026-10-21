# Chapter 15 Activity Answer

- Compare a disposable ordinary foreach version with the supplied VirtualDemo using the same ten thousand records. Record the rendered row counts before and after scrolling.
- Restore Virtualize and verify later identifiers appear. The supplied fixed-height implementation is the answer reference.
- This comparison counts rows; it isn't a latency benchmark, which needs controlled timing and a representative workload.

## Solved Source and Execution

The solved code change applies to a complete copy of the project; use the copy script instead of editing the original sample.

The exact code change and its copy script are in `../ActivityInvestigations/Chapter15`. Its README describes the expected result and how to start over.

## Reference Implementation

- `../Checkpoints/Chapter15/LibraryDesk.Client/Models/BookItem.cs`
- `../Checkpoints/Chapter15/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter15/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter15/LibraryDesk.Client/Pages/VirtualDemo.razor`

## Expected Reasoning

Virtualize reduces visible DOM work while the local data remains allocated. Render guards, paging, AOT and streaming solve different problems, so measure each one on its own.

## Production Notes

Uniform row height simplifies the virtualizer's estimates. Rich rows that expand, wrap unpredictably, or load images later need careful measurement and possibly a different layout. Stable keys preserve row identity, but they do not fix a wrong total count or a provider that returns stale pages. Cancellation matters when scrolling triggers superseded data requests.

Measure the resource you intend to reduce.

The .NET 11 Virtualize API also adds indexed positioning and scrolling support; check the current API documentation if you use them. This example uses the long-standing Items behavior to stay focused on windowing. It doesn't measure production latency, memory savings or WebAssembly AOT.
