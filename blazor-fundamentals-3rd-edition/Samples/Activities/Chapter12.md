# Chapter 12 activity answer

The activity has two parts: the keyboard path on /interop in your running project, and a broken module path in a copy.

## What you should see

In your running project:

- Press Tab until Focus input is highlighted and press Enter: the cursor moves into the text box and the status reads Input focused.
- Click Read browser time, click another menu link and come back with Interop. Both buttons become active again after a moment, and one click on Read browser time updates the time once.

In the copy, where InteropDemo imports ./js/not-present.js:

- The status settles on Browser module unavailable. Reload to retry.
- Both buttons stay disabled.
- The Network tab shows not-present.js with status 404.

## Files involved

- Samples/ActivityInvestigations/Chapter12: the copy's change and its README.
- In the copy: LibraryDesk/LibraryDesk.Client/Pages/InteropDemo.razor, with the module path ./js/not-present.js.
- Unchanged: LibraryDesk/LibraryDesk.Client/wwwroot/js/catalog.js, the module your project loads.

## Why

Coming back to /interop creates a new InteropDemo with its own module and .NET object reference; the old instance released both when you left, so the time updates once, not twice. In the copy the browser can't load the module, so the import call fails with a JSException. The page catches it, shows the message, and leaves module null. Both buttons are disabled while module is null, so the visitor can't call into a module that never loaded.

The copy also has a component test, Tests/Chapter12.Tests.csproj, that gives InteropDemo a stand-in JavaScript runtime whose import fails and checks the path, the message and the disabled buttons.
