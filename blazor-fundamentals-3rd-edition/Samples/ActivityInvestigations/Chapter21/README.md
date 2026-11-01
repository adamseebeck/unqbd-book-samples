# Chapter 21 activity: a help text for the Message box

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter21, the project at the end of Chapter 21, unchanged; you make the change yourself. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 21 --output C:/BookExercises/Chapter21
cd C:/BookExercises/Chapter21
```

Choose an output folder that doesn't exist yet, outside the companion code. The script copies the checkpoint, adds the Tests folder and prints the new folder. The copy also gets this README and an investigation-manifest.json file that lists any change the script made.

## What you change

The copy is your LibraryDesk project at the end of Chapter 21, unchanged, plus a Tests folder. You make the change yourself: in LibraryDesk/LibraryDesk.Client/Components/FeedbackForm.razor, add a paragraph with the id feedback-message-help (Use 5 to 300 characters. This example saves locally.) above the Message box, and make the box's aria-describedby list both ids: feedback-message-help feedback-message-error.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

Open http://127.0.0.1:58631/accessibility and inspect the Message box in the Accessibility pane: its description is the help text, Use 5 to 300 characters. This example saves locally. Submit the form empty: the description is now the help text followed by The Message field is required. The label, the focus outline and the status message work the same with the keyboard alone.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder:

```powershell
dotnet test Tests/Chapter21.Tests.csproj
```

Near the end, the output reads Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0, followed by the duration, and the last line starts with Build succeeded. If a test fails, the last line reads Build failed with 1 error(s) instead; that error is the failing test, not a compile error. It checks that the Message box's aria-describedby lists both ids, that the help text and the label are in place, and that the empty and valid submits behave as before. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter21.md explains the answer.
