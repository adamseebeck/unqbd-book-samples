# Chapter 13 activity: solved copy

This folder is Samples/Checkpoints/Chapter13, the LibraryDesk project at the end of Chapter 13, with the activity solved. Compare it with your own copy. You can run the app and its test from this folder; dotnet adds bin and obj folders here as it builds, which you can delete at any time.

## The solved change

LibraryDesk/LibraryDesk.Client/Pages/TemplatesDemo.razor has a third book, new(3, "Database Field Notes", 26m), in its books array, and the ItemTemplate shows each price before the button with @book.Price.ToString("C", CultureInfo.GetCultureInfo("en-US")), with @using System.Globalization at the top of the page. BookList and SelectBook are unchanged, so Select Blazor Field Notes works here: the exception for book 2 belongs to the activity copy, not to this solution.

## Run it

From the folder, run the command from Chapter 2, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Open http://127.0.0.1:58631/templates. The rows read $24.00 Select C# Field Notes, $28.00 Select Blazor Field Notes and $26.00 Select Database Field Notes. Click Select Database Field Notes: the line reads Selected identifier: 3. Press Ctrl+C in the terminal to stop the app.

## Run its test

Stop the app, then run this from the folder:

```powershell
dotnet test Tests/Chapter13.Tests.csproj
```

Near the end, the output reads Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0, followed by the duration, and the last line starts with Build succeeded.

Samples/Activities/Chapter13.md explains the answer.
