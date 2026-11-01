# LibraryDesk at the end of Chapter 20

This is the cumulative project at the end of Chapter 20. To continue with the next chapter, work in a copy of this complete folder. To follow Chapter 20 from its beginning, use the previous checkpoint instead.

## Open, build and run in Visual Studio

1. Copy the complete folder to a short writable working path, keeping global.json beside the LibraryDesk solution folder.
2. In Visual Studio 2026 with .NET 11 support and the ASP.NET and web development workload, open LibraryDesk/LibraryDesk.slnx. The .sln is retained for compatibility; open only one solution file.
3. Right-click the LibraryDesk server project and choose Set as Startup Project. Select Debug, Any CPU and its http profile. Save All and build with Ctrl+Shift+B.
4. Press F5. Open http://127.0.0.1:58631. Shift+F5 stops this IDE run and may close its debugger browser. Stop the previous copy before running another on the same port.

## What Chapter 20 added

Chapter 20 added LibraryDesk.Pwa beside the solution folder: a standalone WebAssembly progressive web app with an offline reading list. The LibraryDesk solution folder is the same as in Chapter 19.

BookRequests.http under the server project contains the applicable HTTP checks. Start the server first, then click Send request above the request you want. Its @host defaults to the development address.

## Run the tests

Stop debugging and build. Open Test > Test Explorer, group by Project, then right-click LibraryDesk.Tests and choose Run. Expect 23 passed tests. Select a failed test to read its message and source links.

## The separate PWA

Right-click LibraryDesk.Pwa and choose Set as Startup Project, select its http profile and press F5. It opens http://127.0.0.1:58634. Restore LibraryDesk as startup before returning to the server chapters.

LibraryDesk.Pwa.Tests is included in the solution and has two tests. Run that project in Test Explorer. Run All Tests also includes it, so the solution total is two higher than the server suite above. Component tests do not prove service-worker behavior.

Use the Folder Publish workflow in Chapters 20 and 23 to create artifacts/pwa. Serve artifacts/pwa/wwwroot with the loopback Python server for the actual offline checks.

## Command-line alternative

From this outer working folder:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
dotnet test LibraryDesk.Tests
dotnet test LibraryDesk.Pwa.Tests
```

The run command keeps the terminal busy; Ctrl+C stops that terminal-owned process. Run tests after stopping it. F5 and Shift+F5 are the primary IDE workflow.
