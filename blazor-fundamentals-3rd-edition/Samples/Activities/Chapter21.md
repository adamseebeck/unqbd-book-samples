# Chapter 21 Activity Answer

- Use the supplied accessibility page and FeedbackForm as the answer reference. Complete empty, invalid-email, and valid-submission cases with the keyboard.
- Record label associations, focus visibility, and the observed status. Run an available automated audit, then separately record whether a screen reader and physical device were actually used.
- Add a message help paragraph with a unique id explaining the five-to-three-hundred-character bound, and reference it from InputTextArea through aria-describedby.
- Keep the existing label and validation elements. Verify the help relationship and repeat invalid/valid keyboard form cases.

## Solved Source and Execution

The exact solved code delta below applies to a complete copied target. Follow the command rather than changing canonical source.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter21`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../Checkpoints/Chapter21/LibraryDesk.Client/Models/FeedbackInput.cs`
- `../Checkpoints/Chapter21/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter21/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter21/LibraryDesk.Client/Components/FeedbackForm.razor`
- `../Checkpoints/Chapter21/LibraryDesk.Client/Pages/Accessibility.razor`
- `../Checkpoints/Chapter21/LibraryDesk/Components/Layout/MainLayout.razor`
- `../Checkpoints/Chapter21/LibraryDesk/wwwroot/app.css`

## Expected Reasoning

Semantic controls, labels, visible focus, and understandable validation are the baseline. Browser and assistive-technology task checks supply evidence that markup inspection alone cannot provide.

## Verification Boundary

Repeated components must not duplicate document ids when they appear together. The current pages render one FeedbackForm at a time, so their fixed identifiers are unambiguous. A reusable product form rendered multiple times should generate or accept a unique id prefix and keep every label and message relationship aligned.

Test the task, not just the empty screen.

Color contrast, zoom, reduced-motion preferences, input method, error recovery, and screen-reader behavior belong to the product's acceptance matrix. The source examples teach those responsibilities without claiming that a local viewport pass covers every disability or device. Document the browser and interaction tested, and retain specific unresolved environments for later verification.
