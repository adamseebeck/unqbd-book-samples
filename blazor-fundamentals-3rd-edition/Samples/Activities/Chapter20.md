# Chapter 20 Activity Answer

- Use the supplied PWA Home and generated worker files as the answer reference. Publish locally, save one title, wait for worker control, stop the local server, and reload the same URL.
- Require the title to remain visible. Restore the server afterward.
- Record installability or physical-device checks separately and make no hosted deployment or account change.
- Change the local reading-list limit and its message from fifty to three titles in a disposable PWA copy.
- Save three titles, attempt a fourth, and require the stored list to remain three after reloading. Keep this modified component check separate from the original published-shell offline acceptance.

## Solved Source and Execution

The exact solved code delta below applies to a complete copied target. Follow the command rather than changing canonical source.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter20`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../LibraryDesk.Pwa/Pages/Home.razor`
- `../LibraryDesk.Pwa/wwwroot/manifest.webmanifest`
- `../LibraryDesk.Pwa/wwwroot/service-worker.published.js`

## Expected Reasoning

The PWA shell and device-local list use different browser stores. Published-worker activation, same-origin reloads, and storage failure handling establish the narrow offline behavior taught here.

## Verification Boundary

Service workers have an update lifecycle, and an old worker can continue serving an old shell while pages remain open. Test upgrade behavior with two distinct published versions before relying on a rollout. Cache storage can fail or be evicted. Present recovery and export options for valuable offline work rather than treating browser storage as a permanent backup.

Keep offline data ownership explicit.

The current sample has no offline mutation queue and no conflict resolution. An online synchronization feature would require authenticated ownership, idempotent writes, version handling, and a visible recovery path. Do not infer those capabilities from a saved title. The author validation record must distinguish a local published offline reload from installation on a physical device or hosted deployment.
