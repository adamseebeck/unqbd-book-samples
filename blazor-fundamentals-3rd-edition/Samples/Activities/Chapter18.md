# Chapter 18 Activity Answer

- Run the supplied suite and inspect each test name and outcome. In a disposable copy, replace the FavoritesState HashSet with a List and change Add to append and notify unconditionally.
- This deliberately permits duplicates: check that the favorites behavior test fails, then restore the HashSet implementation and rerun. Removing only the conditional notification would not defeat HashSet deduplication.
- The exact reversible change and its copy script are in Samples/ActivityInvestigations/Chapter18. Do not add a test that merely compares service source text.

## Solved Source and Execution

The solved code change applies to a complete copy of the project; use the copy script instead of editing the original sample.

The exact code change and its copy script are in `../ActivityInvestigations/Chapter18`. Its README describes the expected result and how to start over.

## Reference Implementation

- `../LibraryDesk.Tests/LibraryDesk.Tests.csproj`
- `../LibraryDesk.Tests/ComponentTests.cs`

## Expected Reasoning

Component tests check rendering contracts quickly; runtime and browser tests cover what they can't. Pinned APIs and real assertions make the suite reproducible and useful.

## Production Notes

Keep failure cases deliberate and bounded. Tests should not call public demonstration APIs, production user stores, mail senders, or payment endpoints. The query replacements make the required data and failure explicit. A test suite that depends on an unrelated external service can report network availability as an application regression.

Use browser tests for browser behavior.

The suite's assertions are intentionally focused. It does not mirror every implementation line or promise complete accessibility from a markup check. Extend it when a reproduced defect reveals a missing contract, such as an older HTTP response replacing a newer filter result.
