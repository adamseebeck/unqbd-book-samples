# Chapter 3 activity answer

The activity adds a Reset button to the counter page while dotnet watch runs, in a copy of the Chapter 3 project, and compares a change Hot Reload applies with one that needs a restart.

## What you should see

- Two clicks on Add one show Current count: 2.
- When you save the Reset button and method, the terminal reports File updated and then C# and Razor changes applied. The page shows the Reset button and still says Current count: 2: Hot Reload changed the running component without creating a new one.
- Click Reset: Current count: 0.
- When you change private int count; to private long count; and save, the terminal reports Restart is needed to apply the changes, gives the reason (error ENC0009) and asks Do you want to restart your app? After you press y, the app restarts and the count starts again at 0.
- After you stop dotnet watch, dotnet test Tests/Chapter03.Tests.csproj ends with Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0 and a duration, then Build succeeded in and the time.

## The change

LibraryDesk/LibraryDesk.Client/Pages/Run.razor in the copy:

```razor
@page "/run"
@rendermode InteractiveServer
<h1>First run</h1>
<p>Renderer: @RendererInfo.Name</p>
<p role="status" id="count">Current count: @count</p>
<button @onclick="Increment">Add one</button>
<button @onclick="Reset">Reset</button>

@code {
    private int count;
    private void Increment() => count++;
    private void Reset() => count = 0;
}
```

## Files involved

- Samples/ActivityInvestigations/Chapter03: the copy's README and its test.
- In the copy: LibraryDesk/LibraryDesk.Client/Pages/Run.razor, the page you edit.
- Samples/Checkpoints/Chapter03: the project the copy starts from.

## Why

Hot Reload patches the running code where it can. A new button and a new method are additions it supports, so it applies them to the component that is already on the page, and the count you had stays. A field's type is part of the class's shape, which a running app can't change, so dotnet watch asks for a restart, and a restart creates a new component whose count starts at 0. When Hot Reload asks for a restart, restart.

The test renders Run.razor without a browser, clicks Add one twice and Reset once, and checks the count. It finds the Reset button by its text, so it passes only once your button is there.
