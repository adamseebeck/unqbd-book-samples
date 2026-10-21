# Chapter 23 Activity Answer

- Publish both supplied projects to local directories. Run the server artifact in Production and prove the local auth fixture routes are absent.
- Serve the PWA artifact and repeat the offline reload check. The project files and commands are the answer reference.
- Keep the outputs local, record exact runtime and hashes, and perform no deployment, purchase, or account change.
- Add a fixed Revision = "local-review" field to the health endpoint in a disposable finished-server copy.
- Build and run that copy in Production; require the marker from /healthz, missing GETs and no local sign-in controls. For valid-token POSTs, require all three lab paths to match the unrelated missing-route control (404 or 405), with no redirect or success. Obtain the token from /static-feedback in the same session; tokenless POSTs return 400 under this .NET 11 CSRF pipeline, including unrelated unknown routes. Keep the local package validation separate from this built-copy behavior proof.

## Solved Source and Execution

The exact solved code delta below applies to a complete copied target. Follow the command rather than changing canonical source.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter23`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../global.json`
- `../LibraryDesk/LibraryDesk/LibraryDesk.csproj`
- `../LibraryDesk.Pwa/LibraryDesk.Pwa.csproj`
- `../LibraryDesk/LibraryDesk/Program.cs`
- `../LibraryDesk/LibraryDesk.Client/Program.cs`

## Expected Reasoning

The server and PWA publish outputs have different hosting contracts. Running local artifacts verifies those outputs while preserving the separate decisions and evidence required for a live release.

## Verification Boundary

Recheck behavior against the current .NET 11 servicing release and support policy before a public rollout. Do not carry an old eighteen-month STS assumption forward without checking Microsoft's current policy. Pin runtime and container tags when validating a release; a convenient latest tag can point to another build later.

Keep rollback inputs before changing a live host.

CI can reproduce builds and tests, but a workflow file is not evidence that a runner executed. A skipped, billing-blocked, or infrastructure-failed check is unavailable validation. Keep credentials out of repository source and avoid giving a deployment token broader access than the workflow needs. This chapter leaves hosted workflows, containers, real TLS/proxy behavior, and physical-device verification as explicit environment checks.
