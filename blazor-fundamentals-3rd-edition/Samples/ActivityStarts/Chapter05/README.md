# Chapter 5 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this in a copy, so your LibraryDesk project stays as it is. In the copy, the text box has lost its @bind:event, and you put the page right.

- Stop your main IDE run with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter05 to a new working folder such as C:\BookExercises\Chapter05. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio and select LibraryDesk with its http profile. The prepared copy has oninput removed from the binding example. Run it with F5.
- In the copy, the input is `<input id="search" @bind="search" />`, so it uses the default change event. Open http://127.0.0.1:58631/binding and type blazor: all three titles stay until you press Enter or click outside the box, and then only Blazor Field Notes remains.
- Stop the copy with Shift+F5, add @bind:event="oninput" back to the input in LibraryDesk/LibraryDesk.Client/Pages/BindingDemo.razor, and run the copy again. The list now changes as you type.
- Stop the copy again. Under the input, add a paragraph with the id match-count that shows Matches: and the number of matching titles; Matches.Length gives you the number. Run the copy again and check it: an empty box shows Matches: 3, BLAZOR shows Matches: 1, and xyz shows Matches: 0.
- Stop the copy. Its Tests folder has a component test: a small program that renders the page without a browser, types BLAZOR and xyz the way keystrokes do, and checks the list and your Matches line (Chapter 18 explains component tests). In Test Explorer, right-click Chapter05.Tests and choose Run. Expect one passed exercise test. If you run it before your changes, the exercise test fails. Select the failed test to read its message and stack trace; a compiler error appears in Error List instead.
- Compare with Samples/ActivitySolutions/Chapter05, a solved copy, and read Samples/Activities/Chapter05.md for the answer.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.
