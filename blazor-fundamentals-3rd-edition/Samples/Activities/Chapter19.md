# Chapter 19 Activity Answer

- Use the supplied hub and LiveCatalog as the answer reference. Connect two tabs, send a valid notice, and check that both display it.
- Send whitespace and check for a stable validation message. Navigate one tab away and return to verify a new owned connection.
- Notice that the returned tab shows no history until another connected client sends a new notice.
- Change only the client-side retained notice limit from twenty to five.
- Keep two clients connected while sending six numbered notices. Check that both lists show exactly the last five, then repeat the existing validation and connection-lifetime checks.

## Solved Source and Execution

The solved code change applies to a complete copy of the project; use the copy script instead of editing the original sample.

The exact code change and its copy script are in `../ActivityInvestigations/Chapter19`. Its README describes the expected result and how to start over.

## Reference Implementation

- `../Checkpoints/Chapter19/LibraryDesk/Hubs/CatalogHub.cs`
- `../Checkpoints/Chapter19/LibraryDesk.Client/LibraryDesk.Client.csproj`
- `../Checkpoints/Chapter19/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter19/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter19/LibraryDesk.Client/Pages/LiveCatalog.razor`

## Expected Reasoning

The hub validates and broadcasts, the browser owns its connection, and callbacks enter the renderer. Connection recovery and message persistence remain separate contracts.

## Production Notes

A public hub is unsuitable for private notifications until the server enforces access and group membership. Rate limits, payload limits, authenticated connection expiry, scale-out, and backpressure need a design tied to the deployment. This local example demonstrates broadcast behavior and validation; it isn't an internet-scale chat system.

Reconnection is not message durability.

Dispose the owned HubConnection when the component leaves the page. Avoid performing UI mutations directly on a transport callback context. Under load, bound the callback work and batch renders. A two-client local test can't tell you production throughput or latency, so load-test before you rely on it. When you test, note which transport the connection actually used.
