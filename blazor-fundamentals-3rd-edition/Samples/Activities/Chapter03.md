# Chapter 3 activity answer

The activity runs a copy of the Chapter 3 project with dotnet watch and compares two kinds of change: one Hot Reload applies while the app runs, and one that needs a restart.

## What you should see

- The copy's /run page shows the heading My first server counter.
- Two clicks on Add one show Current count: 2.
- After F5 the page shows Current count: 0. The reload ended the old circuit; the new page load created a new Run component, and its count field starts at 0.
- When you change the heading's text and save, the terminal reports File updated and then C# and Razor changes applied, and the page shows the new heading. The app didn't restart.
- When you change private int count; to private long count; and save, the terminal reports Restart is needed to apply the changes, gives the reason (error ENC0009) and asks Do you want to restart your app? After you press y, the app restarts and the count starts again at 0.

## Files involved

- Samples/ActivityInvestigations/Chapter03: the copy's change and its README with the dotnet watch command.
- In the copy: LibraryDesk/LibraryDesk.Client/Pages/Run.razor, the page you edit.
- Samples/Checkpoints/Chapter03: the project the copy starts from.

## Why

The count lives in one component instance on the server. A reload doesn't keep that instance; it creates a new one, so the count resets. Hot Reload patches the running code where it can, which covers markup and most method bodies, so a heading change appears at once. A field's type is part of the class's shape, which a running app can't change, so dotnet watch asks for a restart. When Hot Reload asks for a restart, restart.

The copy also has a component test, Tests/Chapter03.Tests.csproj, that checks the heading and the counter. Run it before you edit Run.razor.
