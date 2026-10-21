# Chapter 18 Activity Code Delta

From Samples/ActivityInvestigations run `python create_copy.py --chapter 18 --output C:/BookExercises/Chapter18` with Python 3, choosing a new working folder outside the companion code. The script checks that its starting files are unchanged, copies the complete project and applies delta.json to the copy.

From that new directory run `dotnet test LibraryDesk.Tests/LibraryDesk.Tests.csproj --filter FullyQualifiedName~FavoritesRefreshSiblingsAndIgnoreDuplicates` with the SDK that global.json selects.

The focused FavoritesRefreshSiblingsAndIgnoreDuplicates test must fail for the List variant and pass for the original HashSet version. The defect is in storage semantics rather than the Changed notification condition.

To start over, make another fresh copy. Make deliberate failures only in the disposable copy, and run the check to confirm the outcome above.
