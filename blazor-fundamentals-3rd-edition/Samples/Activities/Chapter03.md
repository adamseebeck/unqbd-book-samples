# Chapter 3 activity answer

Use a copy of Samples/ActivityStarts/Chapter03 and open its LibraryDesk/LibraryDesk.slnx in Visual Studio. F5 starts the selected app; Shift+F5 stops that debug run. The chapter gives the task, and this file explains its answer.

The activity adds a Reset button to the counter page while the copy runs under Visual Studio, in a copy of the Chapter 3 project, and compares a change Hot Reload applies with one that needs a restart.

## What you should see

- Two clicks on Add one show Current count: 2.
- Save the Reset button and method, then apply Hot Reload with Alt+F10. The button appears and the count remains 2: the current component instance was kept.
- Click Reset: Current count: 0.
- Changing the field from int to long produces Hot Reload - Changes not supported. Restart affected projects starts a new process, so the count returns to 0. Restore int when finished.
- Stop debugging, build and run Chapter03.Tests in Test Explorer. Its one test passes after Reset is implemented.

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

Hot Reload patches the running code where it can. A new button and a new method are additions it supports, so it applies them to the component that is already on the page, and the count you had stays. A field's type is part of the class's shape, which a running app can't change, so Visual Studio offers to restart the affected projects, and a restart creates a new component whose count starts at 0. When Hot Reload asks for a restart, restart.

The test renders Run.razor without a browser, clicks Add one twice and Reset once, and checks the count. It finds the Reset button by its text, so it passes only once your button is there.
