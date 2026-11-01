# Chapter 23 activity: a revision on /healthz

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter23, the project at the end of Chapter 23, unchanged; you make the change yourself. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 23 --output C:/BookExercises/Chapter23
cd C:/BookExercises/Chapter23
```

Choose an output folder that doesn't exist yet, outside the companion code. The script copies the checkpoint and prints the new folder. The copy also gets this README and an investigation-manifest.json file that lists any change the script made.

## What you change

The copy is the finished app at the end of Chapter 23, unchanged. You make the change yourself: in LibraryDesk/LibraryDesk/Program.cs, give the /healthz reply a second property, Revision = "local-review".

## Run it

Publish the copy and run its server artifact in Production with the commands from Chapter 23, from the copy's folder:

```powershell
dotnet publish LibraryDesk/LibraryDesk/LibraryDesk.csproj -c Release -o artifacts/server
cd artifacts/server
dotnet LibraryDesk.dll --contentRoot . --urls http://127.0.0.1:58633 --environment Production
```

Run the artifact rather than dotnet run: outside Development, dotnet run doesn't serve the Blazor script and the scoped styles, so the pages would lose their interactivity and part of their styling. Press Ctrl+C in the terminal when you're done.

## What you should see

Open http://127.0.0.1:58633/healthz: the page shows {"status":"ok","revision":"local-review"}. /lab/signin/reader shows the Not Found page, and /security has no local sign-in buttons.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter23.md explains the answer.
