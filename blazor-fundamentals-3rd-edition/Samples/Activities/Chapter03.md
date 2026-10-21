# Chapter 3 Activity Answer

- Run /run, click twice, and record the displayed value. Reload the whole document and record the new component value.
- Change the heading through Hot Reload, then make an edit that requires restarting and compare the result. The supplied Run.razor is the answer reference: the private count belongs to the component, not durable storage.
- Implement the heading My first server counter in the copied Run component. Check it with two clicks and a full reload, and record which source edit actually required a restart.

## Solved Source and Execution

The exact solved code delta below applies to a complete copied target. Follow the command rather than changing canonical source.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter03`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../Checkpoints/Chapter03/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter03/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter03/LibraryDesk.Client/Pages/Run.razor`

## Expected Reasoning

The counter turns a render check into an execution check. File responsibilities, process-aware breakpoints, and explicit startup commands make later failures easier to isolate.

## Verification Boundary

Detailed exceptions help a local developer, but they expose internals to untrusted users. Keep the development environment local and let production use its exception handler. A browser connection failure should present a recovery path rather than an unexplained frozen page. The template's reconnection UI belongs to the same server circuit behavior; it cannot preserve state after the server has discarded that circuit.

Keep one known startup command.

Record the command, selected environment, URL, and process for each test run. Those four facts prevent an old app on another port from being mistaken for the current build. A local debugging pass establishes this sample's behavior, not production availability or performance.
