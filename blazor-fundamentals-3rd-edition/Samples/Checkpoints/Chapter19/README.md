# LibraryDesk at the end of Chapter 19

This is the cumulative project at the end of Chapter 19. To continue with the next chapter, work in a copy of this complete folder. To follow Chapter 19 from its beginning, use the previous checkpoint instead.

## Open, build and run in Visual Studio

1. Copy the complete folder to a short writable working path, keeping global.json beside the LibraryDesk solution folder.
2. In Visual Studio 2026 with .NET 11 support and the ASP.NET and web development workload, open LibraryDesk/LibraryDesk.slnx. The .sln is retained for compatibility; open only one solution file.
3. Right-click the LibraryDesk server project and choose Set as Startup Project. Select Debug, Any CPU and its http profile. Save All and build with Ctrl+Shift+B.
4. Press F5. Open http://127.0.0.1:58631. Shift+F5 stops this IDE run and may close its debugger browser. Stop the previous copy before running another on the same port.

## What Chapter 19 added

Chapter 19 added http://127.0.0.1:58631/live (Live in the menu): catalog notices sent through the CatalogHub SignalR hub at /hubs/catalog. LibraryDesk.Tests also holds LiveTransportTests.cs.

BookRequests.http under the server project contains the applicable HTTP checks. Start the server first, then click Send request above the request you want. Its @host defaults to the development address.

## Run the tests

Stop debugging and build. Open Test > Test Explorer, group by Project, then right-click LibraryDesk.Tests and choose Run. Expect 23 passed tests. Select a failed test to read its message and source links.

## Command-line alternative

From this outer working folder:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
dotnet test LibraryDesk.Tests
```

The run command keeps the terminal busy; Ctrl+C stops that terminal-owned process. Run tests after stopping it. F5 and Shift+F5 are the primary IDE workflow.
