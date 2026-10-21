# Chapter 8 Activity Answer

- Run the empty and valid feedback cases using the supplied components as the answer reference. Try an invalid email-shaped value and a message shorter than five characters; neither should report acceptance.
- Then repeat on /static-feedback with JavaScript disabled and confirm that server validation still rejects invalid data. No email should be sent and no account should appear.
- Change the shared FeedbackInput message minimum from five to ten characters in a disposable copy.
- With a valid name and email, require a nine-character message to fail and a ten-character message to pass in interactive and static server forms. Restore the five-character baseline afterward.

## Solved Source and Execution

The exact solved code delta below applies to a complete copied target. Follow the command rather than changing canonical source.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter08`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../Checkpoints/Chapter08/LibraryDesk.Client/Models/FeedbackInput.cs`
- `../Checkpoints/Chapter08/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter08/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter08/LibraryDesk.Client/Components/FeedbackForm.razor`
- `../Checkpoints/Chapter08/LibraryDesk.Client/Pages/Feedback.razor`
- `../Checkpoints/Chapter08/LibraryDesk/Components/Pages/StaticFeedback.razor`

## Expected Reasoning

The model defines rules, the validator runs them, the form chooses the submission path, and the status reports the actual effect. Static SSR and interactive forms have different request lifecycles even when they share a model.

## Verification Boundary

A validation attribute describes acceptable input, not permission to perform an operation. A real feedback write still needs a server endpoint, ownership or abuse rules, bounded persistence, and a response contract. The sample makes none of those external writes. Use antiforgery protection for cookie-authenticated form operations and test missing-token requests separately from valid-token requests.

Keep validation and authorization as separate decisions.

Third-party validation libraries require an explicitly supported integration and package version. Registering a validator service alone does not connect it to EditForm. This edition uses the built-in mechanism rather than naming an unverified adapter package. Async validation also needs cancellation and stale-result handling when a field changes during a remote check; the synchronous fixture does not claim to exercise that separate path.
