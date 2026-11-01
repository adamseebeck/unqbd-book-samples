# Chapter 11 activity: solved copy

This folder is Samples/Checkpoints/Chapter11, the LibraryDesk project at the end of Chapter 11, with the activity solved. Compare it with your own copy. You can run the app from this folder; dotnet adds bin and obj folders here as it builds, which you can delete at any time.

## The solved change

LibraryDesk/LibraryDesk/Program.cs has a Readers policy, built with policy.RequireAuthenticatedUser(), beside the Staff policy inside one AddAuthorization block, and a GET /api/profile endpoint, just before /api/staff/count, that returns the signed-in name and requires the Readers policy.

## Run it

From the folder, run the command from Chapter 2, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Open http://127.0.0.1:58631/api/profile before signing in: the Network tab shows status 401. Sign in on /security as the reader and open /api/profile again: the browser shows {"name":"reader"}. As the librarian it shows {"name":"librarian"}. /api/staff/count still gives the reader 403 and the librarian 200. Press Ctrl+C in the terminal to stop the app.

Samples/Activities/Chapter11.md explains the answer.
