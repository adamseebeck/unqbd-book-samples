# Chapter 8 activity: a ten-character minimum

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter08, the project at the end of Chapter 8, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 8 --output C:/BookExercises/Chapter08
cd C:/BookExercises/Chapter08
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists each change.

## What the copy changes

In LibraryDesk/LibraryDesk.Client/Models/FeedbackInput.cs, the message minimum is MinimumLength = 10 instead of 5.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

On http://127.0.0.1:58631/feedback, with Ada, ada@example.com and the nine-character message 123456789, Save shows The field Message must be a string with a minimum length of 10 and a maximum length of 300. and Nothing saved. With 1234567890, the status reads Feedback accepted locally for Ada. No account was created. On /static-feedback with JavaScript disabled, nine characters show the same message and No submission.; ten characters show Static form accepted locally. One change to the shared model changed both forms.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder:

```powershell
dotnet test Tests/Chapter08.Tests.csproj
```

The last line starts with Passed! and reports Passed: 1. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter08.md explains the answer.
