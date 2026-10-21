# Chapter 7 Activity Code Delta

From Samples/ActivityInvestigations run `python create_copy.py --chapter 7 --output C:/BookExercises/Chapter07` with Python 3, choosing a new working directory outside the canonical source. The runner verifies the exact baseline and applies delta.json to a complete copied target. It preserves a before/after hash manifest.

From that new directory run `dotnet run --project LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631` with the pinned SDK.

The new constrained alias /catalog-item/1 and /catalog-item/2 resolves the correct titles; /catalog-item/999 shows the missing-item branch. Both the original and alias routes reject a non-integer identifier through the not-found path.

To restore the implementation, use another fresh copy of the documented baseline. Deliberate failures belong only in the disposable copy. The expected outcome above is what to look for; running the check is what confirms it.
