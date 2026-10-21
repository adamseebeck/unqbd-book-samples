# Chapter 21 Activity Answer

- Use the supplied accessibility page and FeedbackForm as the answer reference. Complete empty, invalid-email, and valid-submission cases with the keyboard.
- Check label associations, focus visibility and the status message. Run an automated audit if you have one, and note whether you also tested with a screen reader or a physical device.
- Add a message help paragraph with a unique id explaining the five-to-three-hundred-character bound, and reference it from InputTextArea through aria-describedby.
- Keep the existing label and validation elements. Verify the help relationship and repeat invalid/valid keyboard form cases.

## Solved Source and Execution

The solved code change applies to a complete copy of the project; use the copy script instead of editing the original sample.

The exact code change and its copy script are in `../ActivityInvestigations/Chapter21`. Its README describes the expected result and how to start over.

## Reference Implementation

- `../Checkpoints/Chapter21/LibraryDesk.Client/Models/FeedbackInput.cs`
- `../Checkpoints/Chapter21/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter21/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter21/LibraryDesk.Client/Components/FeedbackForm.razor`
- `../Checkpoints/Chapter21/LibraryDesk.Client/Pages/Accessibility.razor`
- `../Checkpoints/Chapter21/LibraryDesk/Components/Layout/MainLayout.razor`
- `../Checkpoints/Chapter21/LibraryDesk/wwwroot/app.css`

## Expected Reasoning

Semantic controls, labels, visible focus, and understandable validation are the baseline. Testing real tasks in a browser with assistive technology shows what markup inspection alone can't.

## Production Notes

Repeated components must not duplicate document ids when they appear together. The current pages render one FeedbackForm at a time, so their fixed identifiers are unambiguous. A reusable product form rendered multiple times should generate or accept a unique id prefix and keep every label and message relationship aligned.

Test the task, not just the empty screen.

Color contrast, zoom, reduced-motion preferences, input method, error recovery, and screen-reader behavior all belong in your product's acceptance tests. A local viewport check doesn't cover every disability or device, so note which browsers and interactions you tested and which still need testing.
