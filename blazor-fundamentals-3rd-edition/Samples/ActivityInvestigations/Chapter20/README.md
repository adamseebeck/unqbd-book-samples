# Chapter 20 Activity Code Delta

From Samples/ActivityInvestigations run `python create_copy.py --chapter 20 --output C:/BookExercises/Chapter20` with Python 3, choosing a new working directory outside the canonical source. The runner verifies the exact baseline and applies delta.json to a complete copied target. It preserves a before/after hash manifest.

From that new directory run `dotnet run --project LibraryDesk.Pwa/LibraryDesk.Pwa.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58632` with the pinned SDK.

The changed PWA stores exactly three titles. A fourth save reports the three-title limit without another localStorage write, and a fresh component reloads the same three persisted titles. This focused component proof is separate from the finished PWA published-worker offline test.

To restore the implementation, use another fresh copy of the documented baseline. Deliberate failures belong only in the disposable copy. The expected outcome above is what to look for; running the check is what confirms it.

The disposable copy also contains the focused behavior check: `dotnet test Tests/Chapter20.Tests.csproj`. A component test does not certify a browser module download or CSS layout.
