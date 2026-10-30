# Chapter 2 activity answer

The activity checks two things in a copy of the Chapter 2 project: what the SDK pin in global.json does when it asks for a version you don't have, and that the copy runs with the command from Chapter 2.

## What you should see

- With "version": "11.0.999" in the copy's global.json, dotnet --version fails. The error says A compatible .NET SDK was not found., then Requested SDK version: 11.0.999 and global.json file: followed by the full path of the copy's global.json. It also lists the installed SDKs.
- With "11.0.100" back, dotnet --version prints 11.0.100 or a later 11.0 version again.
- Run with the command from Chapter 2, the copy shows the heading LibraryDesk setup verified at http://127.0.0.1:58631. The copy's Home page has that heading in place of the template's Hello, world!

## Files involved

- Samples/ActivityInvestigations/Chapter02: the copy's change and its README.
- In the copy: global.json (you edit it) and LibraryDesk/LibraryDesk/Components/Pages/Home.razor (the changed heading).
- Samples/Checkpoints/Chapter02: the project the copy starts from.

## Why

Every dotnet command looks for a global.json file in the current folder and its parents, and uses the SDK version it asks for. "rollForward": "latestFeature" lets a newer feature band of .NET 11 stand in, but no installed SDK can stand in for 11.0.999, so the command stops and tells you which file asked for it. That message is the one to look for when a project suddenly refuses to build on a new machine: check the version in global.json against dotnet --list-sdks.

The copy also has a component test, Tests/Chapter02.Tests.csproj, that checks the changed heading. Run it from the copy's folder with dotnet test Tests/Chapter02.Tests.csproj.
