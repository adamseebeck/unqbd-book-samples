# Chapter 22 Activity Code Delta

From Samples/ActivityInvestigations run `python create_copy.py --chapter 22 --output C:/BookExercises/Chapter22` with Python 3, choosing a new working directory outside the canonical source. The runner verifies the exact baseline and applies delta.json to a complete copied target. It preserves a before/after hash manifest.

From that new directory run `dotnet run --project LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631` with the pinned SDK.

Two clicks produce sequential ReadNumber 1 and 2 and BookCount 3 in baseline server Information logs. With the category threshold Warning, UI counts still advance but those Information records are absent. Restore the original category threshold.

To restore the implementation, use another fresh copy of the documented baseline. Deliberate failures belong only in the disposable copy. The expected outcome above is what to look for; running the check is what confirms it.

The disposable copy also contains the focused behavior check: `dotnet test Tests/Chapter22.Tests.csproj`. A component test does not certify a browser module download or CSS layout.
