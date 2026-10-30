# Chapter 20 Activity Answer

- Use the supplied PWA Home and generated worker files as the answer reference. Publish locally, save one title, wait for worker control, stop the local server, and reload the same URL.
- Check that the title is still visible. Restore the server afterward.
- Installation and physical-device testing are separate checks; this activity doesn't deploy anything.
- Change the local reading-list limit and its message from fifty to three titles in a disposable PWA copy.
- Save three titles, try a fourth, and check that the stored list still has three after reloading. This changed copy is separate from the published offline check above.

## Commands

In a working copy of Samples, publish and serve the PWA from the folder that holds LibraryDesk.Pwa:

```powershell
dotnet publish LibraryDesk.Pwa/LibraryDesk.Pwa.csproj -c Release -o artifacts/pwa
python -m http.server 58632 --bind 127.0.0.1 --directory artifacts/pwa/wwwroot
```

Open http://127.0.0.1:58632/ and wait a few seconds for the service worker to install. Add a title and reload, so the worker controls the page. Stop the server with Ctrl+C in its terminal and reload the same address: the shell and the saved title are still there.

## Solved Source and Execution

The solved code change applies to a complete copy of the project; use the copy script instead of editing the original sample.

The exact code change and its copy script are in `../ActivityInvestigations/Chapter20`. Its README describes the expected result and how to start over.

## Reference Implementation

- `../LibraryDesk.Pwa/Pages/Home.razor`
- `../LibraryDesk.Pwa/wwwroot/manifest.webmanifest`
- `../LibraryDesk.Pwa/wwwroot/service-worker.published.js`

## Expected Reasoning

The PWA shell and device-local list use different browser stores. Published-worker activation, same-origin reloads and storage failure handling make up the offline behavior this chapter teaches.

## Production Notes

Service workers have an update lifecycle, and an old worker can continue serving an old shell while pages remain open. Test upgrade behavior with two distinct published versions before relying on a rollout. Cache storage can fail or be evicted. Present recovery and export options for valuable offline work rather than treating browser storage as a permanent backup.

Keep offline data ownership explicit.

The sample has no offline mutation queue and no conflict resolution, so a saved title doesn't mean it can sync. An online synchronization feature would require authenticated ownership, idempotent writes, version handling, and a visible recovery path. A local offline reload of the published app is also different from installing it on a physical device or running a hosted deployment, so test each one you need.
