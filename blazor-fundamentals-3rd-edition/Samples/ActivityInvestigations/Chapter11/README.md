# Chapter 11 activity: a Readers policy and GET /api/profile

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter11, the project at the end of Chapter 11, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 11 --output C:/BookExercises/Chapter11
cd C:/BookExercises/Chapter11
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists each change.

## What the copy changes

LibraryDesk/LibraryDesk/Program.cs has the solved change: a Readers policy built with policy.RequireAuthenticatedUser() beside the Staff policy, and a GET /api/profile endpoint, just before /api/staff/count, that returns the signed-in name and requires the Readers policy. Compare it with your own attempt.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

Open http://127.0.0.1:58631/api/profile before signing in: the Network tab shows status 401. Sign in on /security as the reader and open /api/profile again: the browser shows {"name":"reader"}. As the librarian it shows {"name":"librarian"}. /api/staff/count still gives the reader 403 and the librarian 200.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter11.md explains the answer.
