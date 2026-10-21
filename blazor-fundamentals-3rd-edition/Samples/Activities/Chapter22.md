# Chapter 22 Activity Answer

- Run /diagnostics, click twice, and find the matching server events with BookCount=3 and sequential ReadNumber values. Change the category threshold in a disposable copy so Information is suppressed, then restore it.
- The supplied Diagnostics and appsettings files are the answer reference. Do not create a remote telemetry resource or send real user data.

## Solved Source and Execution

The exact solved code delta below applies to a complete copied target. Follow the command rather than changing canonical source.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter22`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../Checkpoints/Chapter22/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter22/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter22/LibraryDesk/Components/Pages/Diagnostics.razor`
- `../Checkpoints/Chapter22/LibraryDesk/appsettings.json`

## Expected Reasoning

The logger category, message template, execution host, and provider determine what a log means. The local event demonstrates that path without implying an unconfigured monitoring service exists.

## Verification Boundary

Structured logs should use bounded property names and values. A raw URL, arbitrary search string, or user identifier can create high cardinality, sensitive content, and high storage cost. Correlation identifiers are useful when their lifecycle and exposure are controlled. Never log authentication cookies, bearer tokens, passwords, or an entire configuration object.

Verify the receiver before claiming observability.

Metrics and traces complement logs but do not replace error handling. A production collector needs delivery, sampling, retention, and access decisions. The local example verifies a structured server event only. It does not measure a production error rate or promise durable telemetry after the process exits.
