# Chapter 18 Activity Answer

- Run the supplied suite and inspect each test name and outcome. In a disposable copy, replace the FavoritesState HashSet with a List and change Add to append and notify unconditionally.
- This deliberately permits duplicates: require the favorites behavior test to fail, then restore the HashSet implementation and rerun. Removing only the conditional notification would not defeat HashSet deduplication.
- The exact reversible delta and runner are in Samples/ActivityInvestigations/Chapter18. Do not add a test that merely compares service source text.

## Solved Source and Execution

The exact solved code delta below applies to a complete copied target. Follow the command rather than changing canonical source.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter18`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../LibraryDesk.Tests/LibraryDesk.Tests.csproj`
- `../LibraryDesk.Tests/ComponentTests.cs`

## Expected Reasoning

Component tests exercise rendering contracts quickly, while the runtime and browser checks establish different facts. Pinned APIs and real assertions make the suite reproducible and useful.

## Verification Boundary

Keep failure cases deliberate and bounded. Tests should not call public demonstration APIs, production user stores, mail senders, or payment endpoints. The query replacements make the required data and failure explicit. A test suite that depends on an unrelated external service can report network availability as an application regression.

Use browser tests for browser behavior.

The suite's assertions are intentionally focused. It does not mirror every implementation line or promise complete accessibility from a markup check. Extend it when a reproduced defect reveals a missing contract, such as an older HTTP response replacing a newer filter result. Preserve failed results when they explain the correction and record the exact source hashes tested.
