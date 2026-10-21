# Chapter 19 Activity Answer

- Use the supplied hub and LiveCatalog as the answer reference. Connect two tabs, send a valid notice, and require both to display it.
- Send whitespace and require a stable validation message. Navigate one tab away and return to verify a new owned connection.
- Record that the returned tab has no durable history unless another connected client sends a new notice.
- Change only the client-side retained notice limit from twenty to five.
- Keep two clients connected while sending six numbered notices. Require exactly the last five in both lists, then repeat the existing validation and connection-lifetime observations.

## Solved Source and Execution

The exact solved code delta below applies to a complete copied target. Follow the command rather than changing canonical source.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter19`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../Checkpoints/Chapter19/LibraryDesk/Hubs/CatalogHub.cs`
- `../Checkpoints/Chapter19/LibraryDesk.Client/LibraryDesk.Client.csproj`
- `../Checkpoints/Chapter19/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter19/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter19/LibraryDesk.Client/Pages/LiveCatalog.razor`

## Expected Reasoning

The hub validates and broadcasts, the browser owns its connection, and callbacks enter the renderer. Connection recovery and message persistence remain separate contracts.

## Verification Boundary

A public hub is unsuitable for private notifications until the server enforces access and group membership. Rate limits, payload limits, authenticated connection expiry, scale-out, and backpressure need a design tied to the deployment. This bounded loopback fixture demonstrates broadcast behavior and validation; it does not claim an internet-scale chat system.

Reconnection is not message durability.

Dispose the owned HubConnection when the component leaves the page. Avoid performing UI mutations directly on a transport callback context. Under load, bound the callback work and consider batching renders; measuring a two-client local pass cannot establish production throughput or latency. The browser transcript and hub validation results should identify the actual transport path exercised.
