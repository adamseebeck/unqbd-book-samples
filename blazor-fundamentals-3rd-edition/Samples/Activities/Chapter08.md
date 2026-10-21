# Chapter 8 Activity Answer

- Run the empty and valid feedback cases using the supplied components as the answer reference. Try an invalid email-shaped value and a message shorter than five characters; neither should report acceptance.
- Then repeat on /static-feedback with JavaScript disabled and confirm that server validation still rejects invalid data. No email should be sent and no account should appear.
- Change the shared FeedbackInput message minimum from five to ten characters in a disposable copy.
- With a valid name and email, check that a nine-character message fails and a ten-character message passes in both the interactive and static server forms. Restore the five-character minimum afterward.

## Solved Source and Execution

The solved code change applies to a complete copy of the project; use the copy script instead of editing the original sample.

The exact code change and its copy script are in `../ActivityInvestigations/Chapter08`. Its README describes the expected result and how to start over.

## Reference Implementation

- `../Checkpoints/Chapter08/LibraryDesk.Client/Models/FeedbackInput.cs`
- `../Checkpoints/Chapter08/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter08/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter08/LibraryDesk.Client/Components/FeedbackForm.razor`
- `../Checkpoints/Chapter08/LibraryDesk.Client/Pages/Feedback.razor`
- `../Checkpoints/Chapter08/LibraryDesk/Components/Pages/StaticFeedback.razor`

## Expected Reasoning

The model defines rules, the validator runs them, the form chooses the submission path, and the status reports the actual effect. Static SSR and interactive forms have different request lifecycles even when they share a model.

## Production Notes

A validation attribute describes acceptable input, not permission to perform an operation. A real feedback write still needs a server endpoint, ownership or abuse rules, bounded persistence, and a response contract. The sample makes none of those external writes. Use antiforgery protection for cookie-authenticated form operations and test missing-token requests separately from valid-token requests.

Keep validation and authorization as separate decisions.

Third-party validation libraries need an integration package that supports your version. Registering a validator service alone does not connect it to EditForm. This book uses the built-in mechanism. Async validation also needs cancellation and stale-result handling when a field changes during a remote check; this chapter's synchronous example doesn't cover that.
