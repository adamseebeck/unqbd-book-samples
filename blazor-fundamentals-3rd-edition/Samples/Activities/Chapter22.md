# Chapter 22 activity answer

Use a copy of Samples/ActivityStarts/Chapter22 and open its LibraryDesk/LibraryDesk.slnx in Visual Studio. F5 starts the selected app; Shift+F5 stops that debug run. The chapter gives the task, and this file explains its answer.

The activity raises the log threshold for the CatalogReadRecorder category to Warning, in a copy, and sees the entries disappear from Visual Studio's Debug output while the page keeps counting. Here is the change; compare it with yours.

## The change

Add this line to the LogLevel section of both appsettings.json and appsettings.Development.json, after the Default line:

```json
      "LibraryDesk.Components.CatalogReadRecorder": "Warning",
```

## What you should see

- Open /diagnostics and click twice. The page shows Reads recorded: 2, but Visual Studio's Debug output shows no CatalogRead entries.
- Stop debugging, build and run Chapter22.Tests in Test Explorer. Its one test reads both appsettings files, clicks twice and checks that the count reaches 2 without an Information entry.
- Change "Warning" back to "Information" in both files and run the copy again. Each click writes the entry again, as in the chapter:

```text
LibraryDesk.Components.CatalogReadRecorder:
    Information: CatalogRead 3 1
```

## Files involved

- Samples/ActivityInvestigations/Chapter22: the README, delta.json and Tests folder that create_copy.py uses. The copy itself is Samples/Checkpoints/Chapter22, unchanged.
- In the copy: LibraryDesk/LibraryDesk/appsettings.json and LibraryDesk/LibraryDesk/appsettings.Development.json.
- Unchanged: LibraryDesk/LibraryDesk/Components/CatalogReadRecorder.razor, which still calls LogInformation on each click.

## Why

The category of an ILogger<CatalogReadRecorder> is the class's full name, LibraryDesk.Components.CatalogReadRecorder. The most specific LogLevel entry that matches a category wins over Default. With Warning, the logger drops Information entries before any provider writes them, so Visual Studio's Debug output stays quiet, while the component's own count, which doesn't depend on logging, still goes up. The app runs in Development, which reads appsettings.Development.json on top of appsettings.json, so the line must be in both files.
