# Chapter 8 activity answer

The activity raises the message minimum from 5 to 10 characters in one place, the shared model, and checks both forms.

## What you should see

On /feedback, with the name Ada and the email ada@example.com:

- Message 123456789 (nine characters): Save shows The field Message must be a string with a minimum length of 10 and a maximum length of 300. and the status stays Nothing saved.
- Message 1234567890 (ten characters): the status reads Feedback accepted locally for Ada. No account was created.

On /static-feedback, with JavaScript disabled:

- Nine characters show the same validation message and No submission.
- Ten characters show Static form accepted locally.

## Files involved

- Samples/ActivityInvestigations/Chapter08: the copy's change and its README.
- In the copy: LibraryDesk/LibraryDesk.Client/Models/FeedbackInput.cs, with MinimumLength = 10 on the Message property.
- The two forms that use the model, unchanged: LibraryDesk/LibraryDesk.Client/Components/FeedbackForm.razor (interactive) and LibraryDesk/LibraryDesk/Components/Pages/StaticFeedback.razor (static).

## Why

Both forms validate the same FeedbackInput model with DataAnnotationsValidator, so the rule lives in one attribute. The interactive form checks it over the circuit before OnValidSubmit runs; the static form posts to the server, which fills the model and validates it again. The message text comes from the StringLength attribute's own format, which names both limits. One change to the shared model changed both forms.

The copy also has a component test, Tests/Chapter08.Tests.csproj, that submits nine and ten characters to FeedbackForm.
