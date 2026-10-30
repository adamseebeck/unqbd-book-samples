# Chapter 22 activity answer

The activity raises the log threshold for the Diagnostics category to Warning, in a copy, and sees the entries disappear from the terminal while the page keeps counting.

## The change

The copy adds this line to the LogLevel section of both appsettings.json and appsettings.Development.json:

```json
      "LibraryDesk.Components.Pages.Diagnostics": "Warning",
```

## What you should see

- Open /diagnostics and click twice. The page shows Reads recorded: 2, but the terminal shows no CatalogRead entries.
- Change "Warning" back to "Information" in both files and run the copy again. Each click writes the entry again, as in the chapter:

```text
info: LibraryDesk.Components.Pages.Diagnostics[0]
      CatalogRead 3 1
```

- From the copy's folder, dotnet test Tests/Chapter22.Tests.csproj checks the copy as created: two clicks count 2, and no entry is written. Its last line reports Passed: 1.

## Files involved

- Samples/ActivityInvestigations/Chapter22: the copy's change, its README and its Tests folder.
- In the copy: LibraryDesk/LibraryDesk/appsettings.json and LibraryDesk/LibraryDesk/appsettings.Development.json.
- Unchanged: LibraryDesk/LibraryDesk/Components/Pages/Diagnostics.razor, which still calls LogInformation on each click.

## Why

The category of an ILogger<Diagnostics> is the class's full name, LibraryDesk.Components.Pages.Diagnostics. The most specific LogLevel entry that matches a category wins over Default. With Warning, the logger drops Information entries before any provider writes them, so the terminal stays quiet, while the page's own count, which doesn't depend on logging, still goes up. The app runs in Development, which reads appsettings.Development.json on top of appsettings.json, so the line must change in both files.
