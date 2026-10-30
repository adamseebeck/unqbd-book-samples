# Chapter 3 Activity Code Delta

From Samples/ActivityInvestigations run `python create_copy.py --chapter 3 --output C:/BookExercises/Chapter03` with Python 3, choosing a new working folder outside the companion code. The script checks that its starting files are unchanged, copies the complete project and applies delta.json to the copy.

From that new directory run `dotnet run --project LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631` with the SDK that global.json selects.

To try Hot Reload, run `dotnet watch --project LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631` instead. Edit the heading in LibraryDesk.Client/Pages/Run.razor and save: the terminal reports that the change was applied. Press Ctrl+R in that terminal to restart after an edit that Hot Reload can't apply.

The changed heading is visible and two Add one clicks show Current count: 2. A new full document load starts a new component at zero. Note whether your heading edit applied through Hot Reload or needed a restart; a compiled copy can't tell you.

To start over, make another fresh copy. Make deliberate failures only in the disposable copy, and run the check to confirm the outcome above.

The disposable copy also contains a focused test: `dotnet test Tests/Chapter03.Tests.csproj`. A component test doesn't cover a browser module download or CSS layout; check those in a browser.
