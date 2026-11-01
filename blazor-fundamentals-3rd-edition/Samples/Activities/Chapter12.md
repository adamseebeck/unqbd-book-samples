# Chapter 12 activity answer

Use a copy of Samples/ActivityStarts/Chapter12 and open its LibraryDesk/LibraryDesk.slnx in Visual Studio. F5 starts the selected app; Shift+F5 stops that debug run. The chapter gives the task, and this file explains its answer.

The activity runs in a copy of the Chapter 12 checkpoint: first the keyboard path on /interop, then a module path that you break yourself.

## The change

In the copy's LibraryDesk/LibraryDesk.Client/Pages/InteropDemo.razor, the import call becomes:

```csharp
                await JS.InvokeAsync<IJSObjectReference>(
                    "import", "./js/not-present.js");
```

## What you should see

Before your change:

- Press Tab until Focus input is highlighted and press Enter: the cursor moves into the text box and the status reads Input focused.
- Click Read browser time, click another menu link and come back with Interop. Both buttons become active again after a moment, and one click on Read browser time updates the time once.

After your change, where InteropDemo imports ./js/not-present.js:

- The status settles on Browser module unavailable. Reload to retry.
- Both buttons stay disabled.
- The Network tab shows not-present.js with status 404.

## Files involved

- Samples/ActivityInvestigations/Chapter12: the copy's README and its test. The copy is Samples/Checkpoints/Chapter12 unchanged.
- In the copy: LibraryDesk/LibraryDesk.Client/Pages/InteropDemo.razor, the one file you change.
- Unchanged: LibraryDesk/LibraryDesk.Client/wwwroot/js/catalog.js, the module the page loads before your change.

## Why

Coming back to /interop creates a new InteropDemo with its own module and .NET object reference; the old instance released both when you left, so the time updates once, not twice. After your change the browser can't load the module, so the import call fails with a JSException. The page catches it, shows the message, and leaves module null. Both buttons are disabled while module is null, so the visitor can't call into a module that never loaded.

The copy also has a component test, Tests/Chapter12.Tests.csproj, that gives InteropDemo a stand-in JavaScript runtime whose import fails and checks the path, the message and the disabled buttons. It passes after your change.
