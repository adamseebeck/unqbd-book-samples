# Blazor Fundamentals, Third Edition: companion code

The code is in Samples. Paths printed in the book start there.

This download follows the book's Visual Studio workflow. Use Windows, Visual Studio 2026 with .NET 11 support and the ASP.NET and web development workload, the released .NET 11 SDK 11.0.100 or later, and Edge or Chrome. Framework packages are pinned to 11.0.0; SDK and package versions use different numbering. Python 3 is needed only for the local static host in Chapters 20 and 23 and the optional exercise generator.

## Find the right folder

- Checkpoints/Chapter02 to Chapter23: the complete cumulative project at the end of each chapter. Start a chapter from the previous checkpoint if you need to catch up.
- LibraryDesk: the finished server/client solution. LibraryDesk.Tests, LibraryDesk.Pwa and LibraryDesk.Pwa.Tests sit beside it and are included in LibraryDesk/LibraryDesk.slnx.
- ActivityStarts/Chapter02 to Chapter23: prepared unsolved exercise copies, with any supplied Tests project included in the solution.
- Activities: every activity's answer and explanation.
- ActivitySolutions: solved project copies for Chapters 4, 5, 11 and 13.
- ActivityInvestigations: optional Python generator and the checked exercise changes it applies.

## Open, build, run and debug

For a checkpoint or activity, copy the complete ChapterNN folder to a short working path. For the finished app, copy global.json and all four sibling folders, LibraryDesk, LibraryDesk.Tests, LibraryDesk.Pwa and LibraryDesk.Pwa.Tests, into one new working folder. Preserve their relative locations so the solution can load all five projects.

Keep global.json beside the LibraryDesk solution folder. Open LibraryDesk/LibraryDesk.slnx in Visual Studio. The older .sln is retained for compatibility; open only one solution at a time. Right-click the LibraryDesk server project and choose Set as Startup Project, then select Debug, Any CPU and http. Save All, build with Ctrl+Shift+B and press F5. The server uses http://127.0.0.1:58631. Shift+F5 stops this debug run. Stop the prior copy before starting another on the same port.

Chapter 3 teaches breakpoints, Locals, stepping and Hot Reload. Chapter 10 extends debugging to client C# in WebAssembly. Use browser DevTools for requests, HTML, accessibility, storage and workers. View > Output with Debug selected shows server debugger output and log entries; server logs do not appear in the browser's Console.

## Tests

After building, open Test > Test Explorer. Run a project or selected test from its context menu. The cumulative LibraryDesk.Tests suite has 18 tests at Chapter 18, 23 at Chapters 19 and 20, and 24 from Chapter 21 onward. LibraryDesk.Pwa.Tests has 2 tests and is included from Chapter 20. The finished solution therefore has 26 tests. Exercise Tests projects are separate; follow the chapter to select the right project or test. Passing component tests does not establish browser interactivity or offline behavior.

## PWA and publishing

Set LibraryDesk.Pwa as startup with its http profile to debug the separate offline reading-list app at http://127.0.0.1:58634. Restore LibraryDesk as startup for the server chapters. Right-click each app project and use Publish > Folder to build the local Release artifacts described in Chapters 11, 20 and 23. A local publish creates files; deploying to a live host is a separate action.

Chapters 20 and 23 use Python's loopback static server to check the PWA's published service worker. Check Python in a new PowerShell window with python --version. Install Python 3 from python.org if needed; if your installation uses py, substitute py for python. Development F5 uses a worker that does not cache the app, so it cannot establish offline startup.

## Activities

Copy ActivityStarts/ChapterNN to a new working folder, open its solution and follow its README. These starts intentionally contain the investigation described in the chapter, not the finished answer. Chapter 2's SDK pin is deliberately broken (11.0.999); repair it as the task directs. Keep the cumulative project separate. To start again, use another clean copy.

## Command-line alternatives

Run from the outer working folder, the one holding global.json:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
dotnet test LibraryDesk.Tests
dotnet test LibraryDesk.Pwa.Tests
```

The run command keeps its terminal busy; Ctrl+C stops that terminal-owned process. Run tests after stopping it. F5 and Shift+F5 are the primary IDE workflow. See each chapter's alternative section for the exact create and publish commands.
