# Chapter 17 Activity Code Delta

From Samples/ActivityInvestigations run `python create_copy.py --chapter 17 --output C:/BookExercises/Chapter17` with Python 3, choosing a new working folder outside the companion code. The script checks that its starting files are unchanged, copies the complete project and applies delta.json to the copy.

From that new directory run `dotnet run --project LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631` with the SDK that global.json selects.

After favoriting two distinct books, the new clear button empties the injected scoped FavoritesState and updates the page status to zero. Repeating clear stays at zero. It does not save anything across runtimes or reloads.

To start over, make another fresh copy. Make deliberate failures only in the disposable copy, and run the check to confirm the outcome above.

The disposable copy also contains a focused test: `dotnet test Tests/Chapter17.Tests.csproj`. A component test doesn't cover a browser module download or CSS layout; check those in a browser.
