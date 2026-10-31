# Chapter 21 activity answer

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
- From the copy's folder, dotnet test Tests/Chapter21.Tests.csproj runs the copy's test. The output reads Test summary: total: 1, failed: 0, succeeded: 1, skipped: 0, and the last line starts with Build succeeded.

## Files involved

- Samples/ActivityInvestigations/Chapter21: the copy (your Chapter 21 project, unchanged), its README and its Tests folder.
- In the copy: LibraryDesk/LibraryDesk.Client/Components/FeedbackForm.razor.
- Unchanged: LibraryDesk.Tests/AccessibilityTests.cs, the suite's own check of the form's labels and descriptions.

## Why

aria-describedby takes a list of ids. A screen reader reads the label as the box's name, then the text of each listed element, in order, as its description. The error element is empty until validation fails, so before a submit the description is only the help text, and after a failed submit the error follows it. The label still names the box through for and id, so adding a description changes nothing about how the box is named or reached.
