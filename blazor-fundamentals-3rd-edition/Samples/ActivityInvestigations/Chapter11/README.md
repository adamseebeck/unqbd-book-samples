# Chapter 11 Activity Code Delta

From Samples/ActivityInvestigations run `python create_copy.py --chapter 11 --output C:/BookExercises/Chapter11` with Python 3, choosing a new working directory outside the canonical source. The runner verifies the exact baseline and applies delta.json to a complete copied target. It preserves a before/after hash manifest.

From that new directory run `dotnet run --project LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631` with the pinned SDK.

The new authenticated Readers policy returns 401 for anonymous /api/profile and the actual reader/librarian names for their signed-in requests. Reader still receives 403 for the Staff API while librarian receives 200. This tests server cookies and API authorization only.

To restore the implementation, use another fresh copy of the documented baseline. Deliberate failures belong only in the disposable copy. The expected outcome above is what to look for; running the check is what confirms it.
