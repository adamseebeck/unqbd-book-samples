# Chapter 23 Activity Code Delta

From Samples/ActivityInvestigations run `python create_copy.py --chapter 23 --output C:/BookExercises/Chapter23` with Python 3, choosing a new working folder outside the companion code. The script checks that its starting files are unchanged, copies the complete project and applies delta.json to the copy.

From that new directory run `dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Production --urls http://127.0.0.1:58631` with the SDK that global.json selects.

The changed health endpoint returns status ok and revision local-review when this built copy runs in Production. Both local sign-in roles and sign-out stay absent: GET requests return 404 and the controls are missing. This Production Debug copy returns 405 for valid-token POSTs to the unknown-route control and all three lab paths, while the published Release package returns 404. POSTs without a token return 400 before routing. This marker change is a local exercise; it doesn't deploy anything or change the finished server and PWA packages.

To start over, make another fresh copy. Make deliberate failures only in the disposable copy, and run the check to confirm the outcome above.
