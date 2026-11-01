# Chapter 22 activity answer

The activity raises the log threshold for the CatalogReadRecorder category to Warning, in a copy, and sees the entries disappear from the terminal while the page keeps counting. Here is the change; compare it with yours.

## The change

Add this line to the LogLevel section of both appsettings.json and appsettings.Development.json, after the Default line:

```json
      "LibraryDesk.Components.CatalogReadRecorder": "Warning",
```

## What you should see

- Open /diagnostics and click twice. The page shows Reads recorded: 2, but the terminal shows no CatalogRead entries.
- From the copy's folder, dotnet test Tests/Chapter22.Tests.csproj reads both appsettings files, clicks twice and checks that the count reaches 2 and no entry is written. The output reads Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0, and the last line starts with Build succeeded.
- Change "Warning" back to "Information" in both files and run the copy again. Each click writes the entry again, as in the chapter:

```text
info: LibraryDesk.Components.CatalogReadRecorder[0]
      CatalogRead 3 1
```

## Files involved

- Samples/ActivityInvestigations/Chapter22: the README, delta.json and Tests folder that create_copy.py uses. The copy itself is Samples/Checkpoints/Chapter22, unchanged.
- In the copy: LibraryDesk/LibraryDesk/appsettings.json and LibraryDesk/LibraryDesk/appsettings.Development.json.
- Unchanged: LibraryDesk/LibraryDesk/Components/CatalogReadRecorder.razor, which still calls LogInformation on each click.

## Why

The category of an ILogger<CatalogReadRecorder> is the class's full name, LibraryDesk.Components.CatalogReadRecorder. The most specific LogLevel entry that matches a category wins over Default. With Warning, the logger drops Information entries before any provider writes them, so the terminal stays quiet, while the component's own count, which doesn't depend on logging, still goes up. The app runs in Development, which reads appsettings.Development.json on top of appsettings.json, so the line must be in both files.
