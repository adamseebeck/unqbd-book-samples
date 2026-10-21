# Chapter 20 Activity Code Delta

From Samples/ActivityInvestigations run `python create_copy.py --chapter 20 --output C:/BookExercises/Chapter20` with Python 3, choosing a new working folder outside the companion code. The script checks that its starting files are unchanged, copies the complete project and applies delta.json to the copy.

From that new directory run `dotnet run --project LibraryDesk.Pwa/LibraryDesk.Pwa.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58632` with the SDK that global.json selects.

The changed PWA stores exactly three titles. A fourth save reports the three-title limit without another localStorage write, and a fresh component reloads the same three persisted titles. This component test is separate from the finished PWA's offline test with the published worker.

To start over, make another fresh copy. Make deliberate failures only in the disposable copy, and run the check to confirm the outcome above.

The disposable copy also contains a focused test: `dotnet test Tests/Chapter20.Tests.csproj`. A component test doesn't cover a browser module download or CSS layout; check those in a browser.
