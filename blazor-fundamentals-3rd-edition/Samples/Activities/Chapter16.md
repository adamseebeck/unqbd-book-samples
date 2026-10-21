# Chapter 16 Activity Answer

- Use the supplied Modes page as the answer reference. Record each renderer after startup, increment each counter once, and reload.
- Then visit /auto-counter twice after allowing the client assets to download, recording its actual renderer on each visit. Explain any state reset rather than treating it as a defect in the counter.
- Add a serializable int Step parameter, defaulting to 1, to ModeCounter and increment count by Step.
- Set the Server, WebAssembly and Auto island parameters to 2, 3 and 4. Click each once, record the actual renderers and resulting counts, then explain the full-reload reset.

## Solved Source and Execution

The solved code change applies to a complete copy of the project; use the copy script instead of editing the original sample.

The exact code change and its copy script are in `../ActivityInvestigations/Chapter16`. Its README describes the expected result and how to start over.

## Reference Implementation

- `../Checkpoints/Chapter16/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter16/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter16/LibraryDesk.Client/Components/ModeCounter.razor`
- `../Checkpoints/Chapter16/LibraryDesk/Components/Pages/Modes.razor`
- `../Checkpoints/Chapter16/LibraryDesk.Client/Pages/AutoCounter.razor`

## Expected Reasoning

A static parent can host explicit interactive islands, while prerender and runtime activation have different instances. Renderer identity and actual clicks make those boundaries observable.

## Production Notes

Server-rendered HTML is generated for a request, not automatically at application build time. A Blazor Web App with Server interactivity still needs an ASP.NET Core host even when some routes are static. Copying its initial HTML to static storage does not preserve server endpoints or circuits. Standalone WebAssembly has a different deployment artifact, discussed in Chapter 23.

Test each runtime you support.

The .NET 11 template uses BasePath in the document head and includes a reconnection component for Server circuits. Having them doesn't mean subdirectory hosting or reconnection after circuit expiry works. Check asset URLs, direct routes, transport recovery and fresh state in your real deployment. Shared components that compile still need a browser test to show that WebAssembly starts and the page is interactive.
