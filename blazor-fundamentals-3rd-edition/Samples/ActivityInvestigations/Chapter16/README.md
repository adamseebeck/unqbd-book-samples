# Chapter 16 activity: a Step parameter on ModeCounter

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter16, the project at the end of Chapter 16, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 16 --output C:/BookExercises/Chapter16
cd C:/BookExercises/Chapter16
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists any change the script made.

## What the copy changes

In LibraryDesk/LibraryDesk/Components/Pages/Modes.razor, the three islands pass Step="2" (Server), Step="3" (WebAssembly) and Step="4" (Auto) to ModeCounter. ModeCounter doesn't have a Step parameter yet; you add it.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

Open http://127.0.0.1:58631/modes. The page shows An unhandled exception occurred while processing the request. and InvalidOperationException: Object of type 'LibraryDesk.Client.Components.ModeCounter' does not have a property matching the name 'Step'. Stop the copy, add [Parameter] public int Step { get; set; } = 1; to the @code block of LibraryDesk/LibraryDesk.Client/Components/ModeCounter.razor and change count++ to count += Step, then run the copy again. Wait until no counter reads Static and click each button once: the lines read Renderer: Server. Count: 2, Renderer: WebAssembly. Count: 3 and Renderer: WebAssembly. Count: 4. After F5, all three counts return to 0, because the reload created new instances.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder:

```powershell
dotnet test Tests/Chapter16.Tests.csproj
```

Run it after you have added the Step parameter; until then the test project doesn't compile, because it sets ModeCounter's Step. Near the end, the output reads Test summary: total: 3, failed: 0, succeeded: 3, skipped: 0, followed by the duration, and the last line starts with Build succeeded. If a test fails, the last line reads Build failed with 1 error(s) instead; that error is the failing test, not a compile error. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter16.md explains the answer.
