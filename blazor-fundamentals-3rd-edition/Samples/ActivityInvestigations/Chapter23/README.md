# Chapter 23 Activity Code Delta

From Samples/ActivityInvestigations run `python create_copy.py --chapter 23 --output C:/BookExercises/Chapter23` with Python 3, choosing a new working directory outside the canonical source. The runner verifies the exact baseline and applies delta.json to a complete copied target. It preserves a before/after hash manifest.

From that new directory run `dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Production --urls http://127.0.0.1:58631` with the pinned SDK.

The changed health endpoint returns status ok and revision local-review when this built copy runs in Production. Both local sign-in roles and sign-out remain absent: GETs are404 and controls are missing. The recorded Production Debug copy returns405 for valid-token POSTs to the unknown control and all three lab paths, while the published Release package returns404. Tokenless POSTs return400 before this routing result. The explicitly approved finished server/PWA local packages are validated separately; this marker is not a deployment or a claim about those unmodified packages.

To restore the implementation, use another fresh copy of the documented baseline. Deliberate failures belong only in the disposable copy. The expected outcome above is what to look for; running the check is what confirms it.
