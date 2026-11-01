# Chapter 13 activity: solved copy

Compare this solved example with your own activity copy. Work in a fresh copy if you want to run or edit it.

## The solved change

LibraryDesk/LibraryDesk.Client/Pages/TemplatesDemo.razor has a third book, new(3, "Database Field Notes", 26m), in its books array, and the ItemTemplate shows each price before the button with @book.Price.ToString("C", CultureInfo.GetCultureInfo("en-US")), with @using System.Globalization at the top of the page. BookList and SelectBook are unchanged, so Select Blazor Field Notes works here: the exception for book 2 belongs to the activity copy, not to this solution.

## Run in Visual Studio

1. Copy the complete folder to a short writable working path, keeping global.json beside the LibraryDesk solution folder.
2. In Visual Studio 2026 with .NET 11 support and the ASP.NET and web development workload, open LibraryDesk/LibraryDesk.slnx. The .sln is retained for compatibility; open only one solution file.
3. Right-click the LibraryDesk server project and choose Set as Startup Project. Select Debug, Any CPU and its http profile. Save All and build with Ctrl+Shift+B.
4. Press F5. Open http://127.0.0.1:58631. Shift+F5 stops this IDE run and may close its debugger browser. Stop the previous copy before running another on the same port.

The solution includes Chapter13.Tests. Stop debugging, build, then run that project in Test Explorer. Expect one passed test.

Samples/Activities/Chapter13.md explains the behavior and reasoning.
