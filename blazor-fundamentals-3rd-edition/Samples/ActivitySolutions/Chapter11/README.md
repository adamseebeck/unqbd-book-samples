# Chapter 11 activity: solved copy

Compare this solved example with your own activity copy. Work in a fresh copy if you want to run or edit it.

## The solved change

LibraryDesk/LibraryDesk/Program.cs has a Readers policy, built with policy.RequireAuthenticatedUser(), beside the Staff policy inside one AddAuthorization block, and a GET /api/profile endpoint, just before /api/staff/count, that returns the signed-in name and requires the Readers policy.

## Run in Visual Studio

1. Copy the complete folder to a short writable working path, keeping global.json beside the LibraryDesk solution folder.
2. In Visual Studio 2026 with .NET 11 support and the ASP.NET and web development workload, open LibraryDesk/LibraryDesk.slnx. The .sln is retained for compatibility; open only one solution file.
3. Right-click the LibraryDesk server project and choose Set as Startup Project. Select Debug, Any CPU and its http profile. Save All and build with Ctrl+Shift+B.
4. Press F5. Open http://127.0.0.1:58631. Shift+F5 stops this IDE run and may close its debugger browser. Stop the previous copy before running another on the same port.

Samples/Activities/Chapter11.md explains the behavior and reasoning.
