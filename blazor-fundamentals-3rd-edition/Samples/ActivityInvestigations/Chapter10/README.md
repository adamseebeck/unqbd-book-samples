# Chapter 10 activity: a missing API address

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter10, the project at the end of Chapter 10, unchanged; you make the change yourself. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 10 --output C:/BookExercises/Chapter10
cd C:/BookExercises/Chapter10
```

Choose an output folder that doesn't exist yet, outside the companion code. The script copies the checkpoint, adds the Tests folder and prints the new folder. The copy also gets this README and an investigation-manifest.json file that lists any change the script made.

## What you change

Nothing in the copy is changed for you. In the copy's LibraryDesk/LibraryDesk.Client/Services/HttpCatalogQuery.cs, change "api/books" to "api/not-present" in the GetAsync call and save the file. When you have seen the result and run the test, change it back to "api/books".

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

Open http://127.0.0.1:58631/api-demo, open the Network tab and click Load books. You see a row named not-present with status 404, the text The catalog is unavailable. Try again., and an active Load books button. EnsureSuccessStatusCode turned the 404 into an HttpRequestException, and the page's second catch handled it. After you change the address back to "api/books", run the copy again and press F5 on /api-demo, Load books shows Loaded 3 books. and the three titles.

## Check it with a test

The copy has a Tests folder with a component test for this change. Stop the app, then run this from the copy's folder while the address is still "api/not-present":

```powershell
dotnet test Tests/Chapter10.Tests.csproj
```

The output ends with Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0, duration: ... and then Build succeeded in .... Run it before your change, or after you put "api/books" back, and it fails: the output ends with Test summary: total: 1, failed: 1, succeeded: 0, skipped: 0, duration: ... and Build failed with 1 error(s) in .... That error is the failing test, not a compile error. A component test checks component logic; check the page itself in the browser.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter10.md explains the answer.
