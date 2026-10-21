# Chapter 11 Activity Code Delta

From Samples/ActivityInvestigations run `python create_copy.py --chapter 11 --output C:/BookExercises/Chapter11` with Python 3, choosing a new working folder outside the companion code. The script checks that its starting files are unchanged, copies the complete project and applies delta.json to the copy.

From that new directory run `dotnet run --project LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631` with the SDK that global.json selects.

The new authenticated Readers policy returns 401 for anonymous /api/profile and the actual reader/librarian names for their signed-in requests. Reader still receives 403 for the Staff API while librarian receives 200. This tests server cookies and API authorization only.

To start over, make another fresh copy. Make deliberate failures only in the disposable copy, and run the check to confirm the outcome above.
