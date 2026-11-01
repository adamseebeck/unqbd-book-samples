# Chapter 2 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do the exercise in a copy. The prepared activity starts with an SDK-selection problem that you must repair.

- Stop your own app with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter02 to C:\BookExercises\Chapter02, or another new folder. Work in that copy, and open its LibraryDesk/LibraryDesk.slnx in Visual Studio.
- The copy's outer global.json requests SDK 11.0.999, which is not installed. Read the SDK error. Open that global.json in Visual Studio, replace 11.0.999 with 11.0.100, and save. Reload or reopen the solution so the projects can load.
- In the server project's Components/Pages/Home.razor, change Hello, world! to LibraryDesk setup verified. Save and press F5. Check the heading at http://127.0.0.1:58631, then stop debugging.
- The copy includes a Tests project that checks Home's heading. It is already included in the activity solution. Open Test > Test Explorer, build the solution, then Run All Tests. Expect one passed test. Chapter 18 explains how these component tests work.
- Compare with Samples/Activities/Chapter02.md. To make another fresh copy from a terminal, the optional Samples/ActivityInvestigations/create_copy.py tool remains available; its README explains the Python requirement.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.
