# Chapter 2 activity answer

Use a copy of Samples/ActivityStarts/Chapter02 and open its LibraryDesk/LibraryDesk.slnx in Visual Studio. F5 starts the selected app; Shift+F5 stops that debug run. The chapter gives the task, and this file explains its answer.

The copy of the Chapter 2 project arrives with a global.json that asks for SDK 11.0.999, a version that is not installed. You read the error, fix the pin, change the Home page's heading and check it with a test.

## What you should see

- The unsolved global.json requests SDK 11.0.999. Visual Studio cannot load or build the projects with that unavailable SDK; its error identifies the SDK selection problem.
- Restore 11.0.100 in the outer global.json, save, then reload or reopen the solution so its projects load.
- Change Home.razor's heading and press F5: LibraryDesk setup verified appears at http://127.0.0.1:58631.
- Stop debugging, build and run Chapter02.Tests in Test Explorer. Its one test passes after the heading change and fails before it. A failed assertion is different from a compiler error.

## The change

In the copy's global.json, the version goes back to 11.0.100:

```json
{
    "sdk": {
        "version": "11.0.100",
        "rollForward": "latestFeature"
    }
}
```

In LibraryDesk/LibraryDesk/Components/Pages/Home.razor, the heading line becomes:

```razor
<h1>LibraryDesk setup verified</h1>
```

## Files involved

- Samples/ActivityInvestigations/Chapter02: the copy's change (global.json), its README and the test.
- In the copy: global.json and LibraryDesk/LibraryDesk/Components/Pages/Home.razor, the two files you edit.
- Samples/Checkpoints/Chapter02: the project the copy starts from.

## Why

Every dotnet command looks for a global.json file in the current folder and its parents, and uses the SDK version it asks for. "rollForward": "latestFeature" lets a later .NET 11 SDK stand in, but no installed SDK can stand in for 11.0.999, so the command stops and tells you which file asked for it. Read the SDK-selection error before changing the app code. The command-line alternative is dotnet --list-sdks to inspect installations and dotnet --version to see which SDK the folder selects.

The test renders the Home page without a browser and checks its heading, so it passes only once your change is in. Chapter 18 explains component tests.
