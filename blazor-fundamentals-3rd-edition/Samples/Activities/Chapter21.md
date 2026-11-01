# Chapter 21 activity answer

Use a copy of Samples/ActivityStarts/Chapter21 and open its LibraryDesk/LibraryDesk.slnx in Visual Studio. F5 starts the selected app; Shift+F5 stops that debug run. The chapter gives the task, and this file explains its answer.

The activity adds a help text to the feedback form's Message box, in a copy, and connects it with aria-describedby. Here is the change; compare it with yours.

## The change

In the copy's LibraryDesk/LibraryDesk.Client/Components/FeedbackForm.razor, the help paragraph sits above the Message box, and the box's aria-describedby lists both ids:

```razor
    <p id="feedback-message-help">Use 5 to 300 characters. This example saves locally.</p>
    <InputTextArea id="feedback-message"
                   aria-required="true"
                   aria-describedby="feedback-message-help feedback-message-error"
                   @bind-Value="model.Message" />
```

## What you should see

- Open /accessibility and inspect the Message box in the Accessibility pane: its description is the help text, Use 5 to 300 characters. This example saves locally.
- Submit the form empty: the description is now the help text followed by The Message field is required.
- With the keyboard only, the empty, invalid-email and valid cases behave as before: Tab reaches each box after its label, the focus outline is visible, and the status message reports the result.
- Stop debugging, build and run Chapter21.Tests in Test Explorer. Its one exercise test passes after the description IDs and help text are corrected.

## Files involved

- Samples/ActivityInvestigations/Chapter21: the README, delta.json and Tests folder that create_copy.py uses. The copy itself is Samples/Checkpoints/Chapter21, unchanged.
- In the copy: LibraryDesk/LibraryDesk.Client/Components/FeedbackForm.razor.
- Unchanged: LibraryDesk.Tests/AccessibilityTests.cs, the suite's own check of the form's labels and descriptions.

## Why

aria-describedby takes a list of ids. A screen reader reads the label as the box's name, then the text of each listed element, in order, as its description. The error element is empty until validation fails, so before a submit the description is only the help text, and after a failed submit the error follows it. The label still names the box through for and id, so adding a description changes nothing about how the box is named or reached.
