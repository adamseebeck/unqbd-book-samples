# Chapter 5 activity: the change event and a match count

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter05, the project at the end of Chapter 5, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 5 --output C:/BookExercises/Chapter05
cd C:/BookExercises/Chapter05
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists any change the script made.

## What the copy changes

In LibraryDesk/LibraryDesk.Client/Pages/BindingDemo.razor, the input has no @bind:event, so it uses the default change event: `<input id="search" @bind="search" />`.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

Open http://127.0.0.1:58631/binding and type blazor: all three titles stay until you press Enter or click outside the box. Stop the copy, add @bind:event="oninput" back to the input and run the copy again: the list changes as you type. Stop it again, add a paragraph with the id match-count under the input that shows Matches: and the number of matching titles, and run it again: the page shows Matches: 3 for an empty box, Matches: 1 for BLAZOR and Matches: 0 for xyz.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder:

```powershell
dotnet test Tests/Chapter05.Tests.csproj
```

Near the end, the output reads Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0, followed by the duration, and the last line starts with Build succeeded. If a test fails, the last line reads Build failed with 1 error(s) instead; that error is the failing test, not a compile error. It checks the finished activity, the input event and your match-count line, so it fails until both changes are in. The solved copy in Samples/ActivitySolutions/Chapter05 has a test with the same name that passes too. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter05.md explains the answer.
