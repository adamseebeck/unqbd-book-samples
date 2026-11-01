# Chapter 2 activity answer

The copy of the Chapter 2 project arrives with a global.json that asks for SDK 11.0.999, a version that doesn't exist. You read the error, fix the pin, change the Home page's heading and check it with a test.

## What you should see

- With "version": "11.0.999" in the copy's global.json, dotnet --version fails. Among the lines it prints are A compatible .NET SDK was not found., Requested SDK version: 11.0.999 and global.json file: followed by the full path of the copy's global.json. It also lists the installed SDKs.
- With "11.0.100" back, dotnet --version prints 11.0.100 or a later 11.0 version again.
- After you change the heading, the copy shows LibraryDesk setup verified at http://127.0.0.1:58631, in place of the template's Hello, world!
- dotnet test Tests/Chapter02.Tests.csproj ends with Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0 and a duration, then Build succeeded in and the time. Before your heading change it ends with Build failed with 1 error(s): that error is the failing test, not a compile error.

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

Every dotnet command looks for a global.json file in the current folder and its parents, and uses the SDK version it asks for. "rollForward": "latestFeature" lets a later .NET 11 SDK stand in, but no installed SDK can stand in for 11.0.999, so the command stops and tells you which file asked for it. That message is the one to look for when a project suddenly refuses to build on a new machine: compare the version in global.json with dotnet --list-sdks.

The test renders the Home page without a browser and checks its heading, so it passes only once your change is in. Chapter 18 explains component tests.
