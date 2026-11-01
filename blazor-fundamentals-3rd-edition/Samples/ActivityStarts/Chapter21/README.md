# Chapter 21 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this in a copy, so your LibraryDesk project stays as it is. The task is to add a help text to the feedback form's Message box and connect it to the box, so a screen reader reads it as part of the description.

- Stop your main run with Shift+F5. Copy Samples/ActivityStarts/Chapter21 in File Explorer to a new working folder, such as C:\BookExercises\Chapter21. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio. Select LibraryDesk with its http profile. The solution also includes the separate Chapter21.Tests exercise project.
- In the copy's LibraryDesk/LibraryDesk.Client/Components/FeedbackForm.razor, add a paragraph with the id feedback-message-help and the text Use 5 to 300 characters. This example saves locally. just above the Message box (the InputTextArea). Then change the box's aria-describedby so it lists both ids, the help first: feedback-message-help feedback-message-error.
- Run the copy with F5 using the http profile and open http://127.0.0.1:58631/accessibility. Inspect the Message box in the Accessibility pane: its description is the help text. Submit the form empty: the description is now the help text followed by The Message field is required.
- Repeat the empty, invalid-email and valid cases with the keyboard only, and check that the label, the focus outline and the status message still work.
- Stop debugging and build the copy. In Test Explorer, right-click Chapter21.Tests and choose Run. Its one test checks that aria-describedby lists both IDs, that the help and label remain, and that empty and valid submits still behave correctly. It should pass. The cumulative LibraryDesk.Tests and PWA tests are separate projects.
- Compare your change with Samples/Activities/Chapter21.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.
