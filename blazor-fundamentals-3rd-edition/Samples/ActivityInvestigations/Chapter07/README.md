# Chapter 7 Activity Code Delta

From Samples/ActivityInvestigations run `python create_copy.py --chapter 7 --output C:/BookExercises/Chapter07` with Python 3, choosing a new working folder outside the companion code. The script checks that its starting files are unchanged, copies the complete project and applies delta.json to the copy.

From that new directory run `dotnet run --project LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631` with the SDK that global.json selects.

The new constrained alias /catalog-item/1 and /catalog-item/2 resolves the correct titles; /catalog-item/999 shows the missing-item branch. Both the original and alias routes reject a non-integer identifier through the not-found path.

To start over, make another fresh copy. Make deliberate failures only in the disposable copy, and run the check to confirm the outcome above.
