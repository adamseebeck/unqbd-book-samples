# Chapter 1 Activity Answer

- Open the Home component and replace the introductory paragraph with a description of your own public catalog. Keep the link as a native anchor.
- Before adding a button, write down where its eventual handler must execute. The supplied Home file is the reference solution for a static landing page; Chapter 3 supplies the interactive counter.

## Solved Source and Execution

Exact static-copy delta: `../ActivitySolutions/Chapter01/Home.razor`; its README names the target project.

## Reference Implementation

- `../global.json`
- `../LibraryDesk/LibraryDesk/Components/Pages/Home.razor`

## Expected Reasoning

A component model and an execution model answer different questions. LibraryDesk will use static pages for content and explicit interactive boundaries for behavior, while keeping private decisions on the server.

## Verification Boundary

Server interactivity requires server memory and an active network path for each connected circuit. WebAssembly moves component work to the browser but adds startup downloads and exposes its assemblies and configuration to the user. Static rendering avoids an interactive runtime for content that needs only links and form posts. None of these choices is universally faster. Measure the actual screen and deployment environment before making a performance claim.

Never put secrets in downloadable client code.

The examples target .NET 11 and use the .NET 11 SDK, version 11.0.100 or later.
