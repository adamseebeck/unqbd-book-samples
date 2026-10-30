# Chapter 23 activity answer

The activity adds a revision to /healthz in a copy of the finished app, publishes the copy, and runs its server artifact in Production.

## The change

In the copy's LibraryDesk/LibraryDesk/Program.cs, the health endpoint returns a second value:

```csharp
app.MapGet("/healthz", () =>
    Results.Ok(new { Status = "ok", Revision = "local-review" }));
```

## Commands

From the copy's folder, publish the server project and run the artifact with the commands from the chapter:

```powershell
dotnet publish LibraryDesk/LibraryDesk/LibraryDesk.csproj -c Release -o artifacts/server
cd artifacts/server
dotnet LibraryDesk.dll --contentRoot . --urls http://127.0.0.1:58633 --environment Production
```

## What you should see

- http://127.0.0.1:58633/healthz shows {"status":"ok","revision":"local-review"}.
- /lab/signin/reader shows the Not Found page, the same page as any other unknown address.
- /security has no local sign-in buttons.

Press Ctrl+C in the terminal to stop the artifact.

## Files involved

- Samples/ActivityInvestigations/Chapter23: the copy's change and its README.
- In the copy: LibraryDesk/LibraryDesk/Program.cs, and artifacts/server, the folder dotnet publish creates.
- Samples/Checkpoints/Chapter23: the finished app the copy starts from, the same files as Samples/LibraryDesk.

## Why

dotnet publish compiles the copy in Release and writes everything the server needs, including its static files, to artifacts/server. That folder runs anywhere ASP.NET Core is installed, with dotnet LibraryDesk.dll. The /lab sign-in endpoints and the sign-in buttons exist only when the environment is Development, so in Production the routes are never mapped and the address matches nothing. The environment comes from the --environment argument, not from the build. A revision value in the health reply tells you which build a server is running, without exposing anything private. A local publish is not a deployment: nothing leaves your computer.
