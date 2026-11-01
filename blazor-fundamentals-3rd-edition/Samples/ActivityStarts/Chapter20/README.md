# Chapter 20 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this in a copy, so your projects stay as they are. The task is to limit the reading list to three titles instead of fifty.

- Stop your main run with Shift+F5. Copy Samples/ActivityStarts/Chapter20 in File Explorer to a new working folder such as C:\BookExercises\Chapter20. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio. Select LibraryDesk.Pwa as the startup project with its http profile. The solution includes the separate Chapter20.Tests exercise project.
- In the copy's LibraryDesk.Pwa/Pages/Home.razor, change Save so the list allows at most 3 titles, and change the message to This local list is limited to 3 titles. Change both places, the check and the message.
- Run the copy with F5 and open http://127.0.0.1:58634. Save titles until the list has three, then try a fourth: the status reads This local list is limited to 3 titles. and the list still has three. Reload: the same three titles load.
- Stop debugging and build the solution. In Test Explorer, right-click Chapter20.Tests and choose Run. Its one test saves three titles, tries a fourth, and checks that only three writes reached localStorage and that a new page loads the same three. It should pass. The two LibraryDesk.Pwa.Tests tests are a separate suite.
- Compare your change with Samples/Activities/Chapter20.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.
