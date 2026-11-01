# Chapter 3 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do the exercise in a copy so your main LibraryDesk project stays unchanged. Add a Reset button while the copy runs and observe what Hot Reload preserves.

- Stop your main run with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter03 to a new folder such as C:\BookExercises\Chapter03. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio. It includes the chapter checkpoint and its exercise Tests project.
- Start the copy's http profile with F5, open /run and click Add one twice. Expect Current count: 2.
- In LibraryDesk.Client/Pages/Run.razor, add a Reset button below Add one that calls a Reset method. Add that method inside @code and set count to 0. Save and apply Hot Reload with Alt+F10. Verify that the Reset button appears and the existing count is still 2, then click Reset and check for 0.
- Change the count field's type to long and apply again. Read the unsupported-change message and choose Restart affected projects. Check that the new process starts the count at 0. Stop, restore int and save your finished exercise.
- Open Test > Test Explorer, build and Run All Tests. Expect one passed test, checking two increments and a reset. Before you add Reset, the test should fail because it cannot find that button; read the test's failure detail rather than treating every failure as a compiler error. Chapter 18 explains bUnit component tests.
- Compare with Samples/Activities/Chapter03.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.
