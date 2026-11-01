# Chapter 4 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Work in a copy. Add a third note and verify that its state belongs only to that component instance.

- Stop the main app with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter04 to a new folder such as C:\BookExercises\Chapter04, then open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio.
- In LibraryDesk.Client > Pages > ComponentsDemo.razor, add a third ToggleMessage below the first two. Set Message to Database Field Notes is available. and HiddenText to Tap to read. Save and run with F5, then open /components. The first two notes start at Details hidden and the third at Tap to read.
- Click the second note twice, then the third once. The first two should be hidden again; only the third should show Database Field Notes is available. Each tag created its own state.
- In View > Output, choose Debug and find the Initialized lines: three prerendered instances and three live instances. Leave the page and find the corresponding Disposed lines. Stop debugging when finished.
- Compare with Samples/ActivitySolutions/Chapter04 and Samples/Activities/Chapter04.md. The worked solution includes a Chapter04.Tests project in its solution. Open that solution, then use Test > Test Explorer to run its one test of these three notes. Chapter 18 explains how bUnit performs those checks without a browser.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.
