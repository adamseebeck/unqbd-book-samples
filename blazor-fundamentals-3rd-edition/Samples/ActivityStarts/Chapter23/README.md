# Chapter 23 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this in a copy, so your LibraryDesk project stays as it is. The task is to make /healthz report which build is running, then publish the copy and check the change in Production.

- Stop your main run with Shift+F5. Copy Samples/ActivityStarts/Chapter23 in File Explorer to a new working folder such as C:\BookExercises\Chapter23. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio. The prepared start is the finished checkpoint. Select LibraryDesk for its server publish profile.
- In the copy's LibraryDesk/Program.cs, change the /healthz line so the object it returns has a second property, Revision = "local-review", beside Status = "ok".
- Publish the copy's server through its Visual Studio Folder profile to the copy's artifacts/server. Open PowerShell in that folder and start it with this chapter's dotnet LibraryDesk.dll command. Run the published artifact in Production so the framework scripts and scoped styles come from the files intended for hosting.
- Open http://127.0.0.1:58633/healthz: the page shows {"status":"ok","revision":"local-review"}. Open /lab/signin/reader: the Not Found page. Open /security: no local sign-in buttons. Stop the copy with Ctrl+C.
- Compare your change with Samples/Activities/Chapter23.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.
