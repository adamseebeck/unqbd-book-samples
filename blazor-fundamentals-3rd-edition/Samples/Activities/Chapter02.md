# Chapter 2 Activity Answer

- Create the clean template in a new directory. Record dotnet --version, both project paths, and the first build result.
- Change the SDK version to a deliberately unavailable value in a disposable copy, observe the resolution failure, and restore the correct value. Compare the resulting project structure with the supplied server/client sample; later service references are added in their own chapters.
- After recording the clean baseline, change the static Home heading to LibraryDesk setup verified and run the checkpoint. Require that exact heading in the returned page.

## Solved Source and Execution

The exact solved code delta below applies to a complete copied target. Follow the command rather than changing canonical source.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter02`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../Checkpoints/Chapter02/global.json`
- `../Checkpoints/Chapter02/LibraryDesk/LibraryDesk.csproj`
- `../Checkpoints/Chapter02/LibraryDesk.Client/LibraryDesk.Client.csproj`
- `../Checkpoints/Chapter02/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter02/LibraryDesk.Client/Program.cs`

## Expected Reasoning

The project baseline consists of a selected SDK, matching target frameworks, explicit package versions, and a generated server/client layout. Verify that baseline before diagnosing application behavior.

## Verification Boundary

The supplied sample projects include exact package versions. Restore resolves their transitive dependencies, so record the resolved package graph when you ship a reproducible build. Network failures and vulnerability-feed failures have different meanings from compiler failures. Report the actual failure and keep a local build pass separate from package-advisory coverage.

Build the clean template before changing it.

Use a dedicated working copy for compilation. Generated bin and obj directories are disposable build state, while your source files and configuration are the material to preserve. The companion code ships without those outputs. The full sample contains later chapters' registrations; the initial template command intentionally starts with fewer services.
