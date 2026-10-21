# Chapter 18 Activity Code Delta

From Samples/ActivityInvestigations run `python create_copy.py --chapter 18 --output C:/BookExercises/Chapter18` with Python 3, choosing a new working directory outside the canonical source. The runner verifies the exact baseline and applies delta.json to a complete copied target. It preserves a before/after hash manifest.

From that new directory run `dotnet test LibraryDesk.Tests/LibraryDesk.Tests.csproj --filter FullyQualifiedName~FavoritesRefreshSiblingsAndIgnoreDuplicates` with the pinned SDK.

The focused FavoritesRefreshSiblingsAndIgnoreDuplicates test must fail for the List variant and pass for the original HashSet version. The defect is in storage semantics rather than the Changed notification condition.

To restore the implementation, use another fresh copy of the documented baseline. Deliberate failures belong only in the disposable copy. The expected outcome above is what to look for; running the check is what confirms it.
