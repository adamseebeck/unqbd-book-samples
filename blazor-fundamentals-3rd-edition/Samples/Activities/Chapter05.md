# Chapter 5 activity answer

Use a copy of Samples/ActivityStarts/Chapter05 and open its LibraryDesk/LibraryDesk.slnx in Visual Studio. F5 starts the selected app; Shift+F5 stops that debug run. The chapter gives the task, and this file explains its answer.

The activity compares the default change event with @bind:event="oninput" in a copy, then adds a line that counts the matching titles.

## What you should see

- In the copy, the input is `<input id="search" @bind="search" />`, with no @bind:event. Type blazor on /binding: all three titles stay until you press Enter or click outside the box. Then the list shrinks to Blazor Field Notes.
- Stop the copy, add @bind:event="oninput" back to the input and run it again: the list changes as you type. Use Shift+F5 before editing and F5 afterward for this exercise; Chapter 3 covers applying supported edits with Hot Reload.
- With `<p id="match-count">Matches: @Matches.Length</p>` under the input, the page shows Matches: 3 for an empty box, Matches: 1 for BLAZOR and Matches: 0 for xyz.

## Files involved

- Samples/ActivityInvestigations/Chapter05: the copy without @bind:event, and its README.
- In the copy: LibraryDesk/LibraryDesk.Client/Pages/BindingDemo.razor.
- Samples/ActivitySolutions/Chapter05: the solved page, with @bind:event="oninput", the Search text line and the match-count line. Open its solution, build and run Chapter05.Tests in Test Explorer; its one test passes.

## Why

@bind without @bind:event updates the field on the change event, which the browser raises when the box loses focus or you press Enter. @bind:event="oninput" updates it on every keystroke. Matches is a property computed from search, so anything that reads it, the list or the new count, shows the new result on the next render without extra code. The count needs no field of its own: one source of truth, two views of it.

The activity copy has a component test, Tests/Chapter05.Tests.csproj, that renders the page without a browser, types BLAZOR and xyz the way keystrokes do, and checks the list and your Matches line. Build and run Chapter05.Tests in Test Explorer when finished. Its one test passes after the changes; before them, its failed assertion shows what is missing.
