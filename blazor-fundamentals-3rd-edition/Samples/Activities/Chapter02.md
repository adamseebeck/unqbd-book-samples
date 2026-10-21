# Chapter 2 Activity Answer

- Create the clean template in a new directory. Record dotnet --version, both project paths, and the first build result.
- Change the SDK version to a deliberately unavailable value in a disposable copy, observe the resolution failure, and restore the correct value. Compare the resulting project structure with the supplied server/client sample; later service references are added in their own chapters.
- After noting the clean results, change the static Home heading to LibraryDesk setup verified and run the checkpoint. Check that the returned page shows that exact heading.

## Solved Source and Execution

The solved code change applies to a complete copy of the project; use the copy script instead of editing the original sample.

The exact code change and its copy script are in `../ActivityInvestigations/Chapter02`. Its README describes the expected result and how to start over.

## Reference Implementation

- `../Checkpoints/Chapter02/global.json`
- `../Checkpoints/Chapter02/LibraryDesk/LibraryDesk.csproj`
- `../Checkpoints/Chapter02/LibraryDesk.Client/LibraryDesk.Client.csproj`
- `../Checkpoints/Chapter02/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter02/LibraryDesk.Client/Program.cs`

## Expected Reasoning

The project baseline consists of a selected SDK, matching target frameworks, explicit package versions, and a generated server/client layout. Verify that baseline before diagnosing application behavior.

## Production Notes

The supplied sample projects include exact package versions. Restore resolves their transitive dependencies, so record the resolved package graph when you ship a reproducible build. A network or vulnerability-feed failure is not a compiler error, so read the actual message before you change code. A clean build also doesn't mean your packages are free of known vulnerabilities; check advisories separately.

Build the clean template before changing it.

Use a dedicated working copy for compilation. Generated bin and obj directories are disposable build state, while your source files and configuration are the material to preserve. The companion code ships without those outputs. The full sample contains later chapters' registrations; the initial template command intentionally starts with fewer services.
