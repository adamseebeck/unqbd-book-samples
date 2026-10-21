# Chapter 23 Activity Answer

- Publish both supplied projects to local directories. Run the server artifact in Production and confirm the local test sign-in routes are gone.
- Serve the PWA artifact and repeat the offline reload check. The project files and commands are the answer reference.
- Keep the outputs local and note the runtime version you used; this activity doesn't deploy anything.
- Add a fixed Revision = "local-review" field to the health endpoint in a disposable finished-server copy.
- Build and run that copy in Production. Check for the marker from /healthz, a 404 for GET requests to the lab paths, and no local sign-in controls. For POSTs with a valid token, check that all three lab paths return the same status as the unrelated missing-route control (404 or 405), with no redirect or success. Get the token from /static-feedback in the same session; POSTs without a token return 400 under this .NET 11 CSRF pipeline, even for unrelated unknown routes. This copy is separate from the published packages you built above.

## Solved Source and Execution

The solved code change applies to a complete copy of the project; use the copy script instead of editing the original sample.

The exact code change and its copy script are in `../ActivityInvestigations/Chapter23`. Its README describes the expected result and how to start over.

## Reference Implementation

- `../global.json`
- `../LibraryDesk/LibraryDesk/LibraryDesk.csproj`
- `../LibraryDesk.Pwa/LibraryDesk.Pwa.csproj`
- `../LibraryDesk/LibraryDesk/Program.cs`
- `../LibraryDesk/LibraryDesk.Client/Program.cs`

## Expected Reasoning

The server and PWA publish outputs have different hosting contracts. Running them locally checks those outputs; a live release still needs its own deployment checks.

## Production Notes

Recheck behavior against the current .NET 11 servicing release and support policy before a public rollout. Do not carry an old eighteen-month STS assumption forward without checking Microsoft's current policy. Pin runtime and container tags when validating a release; a convenient latest tag can point to another build later.

Keep rollback inputs before changing a live host.

CI can reproduce builds and tests, but only when a runner actually runs the workflow. Treat a skipped check, or one that failed to start, as not run rather than passed. Keep credentials out of repository source and don't give a deployment token more access than the workflow needs. Hosted workflows, containers, real TLS and proxy behavior, and physical devices each need testing in their own environment.
