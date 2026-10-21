# Chapter 3 Activity Answer

- Run /run, click twice, and record the displayed value. Reload the whole document and record the new component value.
- Change the heading through Hot Reload, then make an edit that requires restarting and compare the result. The supplied Run.razor is the answer reference: the private count belongs to the component, not durable storage.
- Implement the heading My first server counter in the copied Run component. Check it with two clicks and a full reload, and record which source edit actually required a restart.

## Solved Source and Execution

The solved code change applies to a complete copy of the project; use the copy script instead of editing the original sample.

The exact code change and its copy script are in `../ActivityInvestigations/Chapter03`. Its README describes the expected result and how to start over.

## Reference Implementation

- `../Checkpoints/Chapter03/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter03/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter03/LibraryDesk.Client/Pages/Run.razor`

## Expected Reasoning

The counter turns a render check into an execution check. File responsibilities, process-aware breakpoints, and explicit startup commands make later failures easier to isolate.

## Production Notes

Detailed exceptions help a local developer, but they expose internals to untrusted users. Keep the development environment local and let production use its exception handler. A browser connection failure should present a recovery path rather than an unexplained frozen page. The template's reconnection UI belongs to the same server circuit behavior; it cannot preserve state after the server has discarded that circuit.

Keep one known startup command.

Note the command, environment, URL and process for each run. Those four facts keep you from mistaking an old app on another port for the current build. Local debugging shows how this sample behaves, not how it performs or stays available in production.
