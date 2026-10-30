# Chapter 10 activity: a missing API address

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter10, the project at the end of Chapter 10, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 10 --output C:/BookExercises/Chapter10
cd C:/BookExercises/Chapter10
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists each change.

## What the copy changes

In LibraryDesk/LibraryDesk.Client/Services/HttpCatalogQuery.cs, the request goes to api/not-present instead of api/books.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

Open http://127.0.0.1:58631/api-demo, open the Network tab and click Load books. You see a row named not-present with status 404, the text The catalog is unavailable. Try again., and an active Load books button. EnsureSuccessStatusCode turned the 404 into an HttpRequestException, and the page's second catch handled it.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder:

```powershell
dotnet test Tests/Chapter10.Tests.csproj
```

The last line starts with Passed! and reports Passed: 1. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter10.md explains the answer.
