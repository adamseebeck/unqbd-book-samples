# Chapter 16 Activity Code Delta

From Samples/ActivityInvestigations run `python create_copy.py --chapter 16 --output C:/BookExercises/Chapter16` with Python 3, choosing a new working folder outside the companion code. The script checks that its starting files are unchanged, copies the complete project and applies delta.json to the copy.

From that new directory run `dotnet run --project LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631` with the SDK that global.json selects.

The new scalar Step parameter changes one-click counts to 2 for Server, 3 for WebAssembly and 4 for Auto. Component tests check parameter behavior; run the page in a browser to see each island's renderer and count. Full reload recreates their state.

To start over, make another fresh copy. Make deliberate failures only in the disposable copy, and run the check to confirm the outcome above.

The disposable copy also contains a focused test: `dotnet test Tests/Chapter16.Tests.csproj`. A component test doesn't cover a browser module download or CSS layout; check those in a browser.
