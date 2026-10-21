# Chapter 6 Activity Code Delta

From Samples/ActivityInvestigations run `python create_copy.py --chapter 6 --output C:/BookExercises/Chapter06` with Python 3, choosing a new working directory outside the canonical source. The runner verifies the exact baseline and applies delta.json to a complete copied target. It preserves a before/after hash manifest.

From that new directory run `dotnet run --project LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631` with the pinned SDK.

Add C# and Blazor, then remove C# through the new button. The sibling summary changes from two favorites to one and the remaining identifier is 2. A second independent scope remains empty; repeated removal leaves one.

To restore the implementation, use another fresh copy of the documented baseline. Deliberate failures belong only in the disposable copy. The expected outcome above is what to look for; running the check is what confirms it.

The disposable copy also contains the focused behavior check: `dotnet test Tests/Chapter06.Tests.csproj`. A component test does not certify a browser module download or CSS layout.
