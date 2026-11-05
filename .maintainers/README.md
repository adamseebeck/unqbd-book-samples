# Maintainer checks

This folder is for the people who maintain the companion code. Readers do not need it.

## Launch recheck (November 10, 2026)

Install the released .NET 11 SDK (11.0.100). From the repository root run:

```bash
dotnet run .maintainers/verify-launch.cs
```

The script:

1. Builds every project and every solution in every book folder.
2. Runs every test project. Microsoft.Testing.Platform projects run with `dotnet run --project`; the others run with `dotnet test`.
3. Checks that each `mcr.microsoft.com/dotnet/...:11.0-resolute*` tag used by the samples, plus the four base `11.0-resolute` tags, resolves and reports a released 11.0 version rather than a preview or release candidate.
4. Prints a pass/fail table per book, an image table and a list of failures with log paths. The exit code is 0 only when everything passes.

Each project builds from its own folder, so the nearest global.json is honored, exactly as a reader would build it. MAUI platform heads are skipped unless a maui workload is installed; with one, they build for Windows on Windows (the MAUI README's command) and for Android elsewhere. The Blazor Chapter 2 activity start deliberately pins SDK 11.0.999 and is expected to fail SDK resolution. The Blazor `ActivityInvestigations/ChapterNN/Tests` projects are fragments that only build after `create_copy.py` places them beside a checkpoint, so they are skipped; the same tests run from `ActivityStarts`.

Useful options (put them after `--`):

```bash
dotnet run .maintainers/verify-launch.cs -- --books efcore,blazor
```

```bash
dotnet run .maintainers/verify-launch.cs -- --images-only
```

`--root <dir>` verifies another copy, `--parallel <n>` sets concurrency, `--skip-images` skips the image check and `--logs <dir>` sets the log folder.

Some tests need services that are not part of a plain SDK install, such as SQL Server or LocalDB on Windows. They show up as failures with their log path so they can be checked by hand.
