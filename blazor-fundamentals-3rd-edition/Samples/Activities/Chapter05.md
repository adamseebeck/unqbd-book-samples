# Chapter 5 activity answer

The activity compares the default change event with @bind:event="oninput" in a copy, then adds a line that counts the matching titles.

## What you should see

- In the copy, the input is `<input id="search" @bind="search" />`, with no @bind:event. Type blazor on /binding: all three titles stay until you press Enter or click outside the box. Then the list shrinks to Blazor Field Notes.
- Stop the copy, add @bind:event="oninput" back to the input and run it again: the list changes as you type. Every change needs a restart here, because the copy runs with dotnet run.
- With `<p id="match-count">Matches: @Matches.Length</p>` under the input, the page shows Matches: 3 for an empty box, Matches: 1 for BLAZOR and Matches: 0 for xyz.

## Files involved

- Samples/ActivityInvestigations/Chapter05: the copy without @bind:event, and its README.
- In the copy: LibraryDesk/LibraryDesk.Client/Pages/BindingDemo.razor.
- Samples/ActivitySolutions/Chapter05: the solved page, with @bind:event="oninput", the Search text line and the match-count line. From that folder, dotnet test Tests/Chapter05.Tests.csproj runs the same check and passes.

## Why

@bind without @bind:event updates the field on the change event, which the browser raises when the box loses focus or you press Enter. @bind:event="oninput" updates it on every keystroke. Matches is a property computed from search, so anything that reads it, the list or the new count, shows the new result on the next render without extra code. The count needs no field of its own: one source of truth, two views of it.

The activity copy has a component test, Tests/Chapter05.Tests.csproj, that renders the page without a browser, types BLAZOR and xyz the way keystrokes do, and checks the list and your Matches line. Run it when you're done: it ends with Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0 and a duration, then Build succeeded in and the time. Run before your changes, it ends with Build failed with 1 error(s); that error is the failing test, not a compile error.
