# Chapter 19 testing

In Visual Studio 2026 open Chapter19.slnx with File > Open > Project/Solution. Build the solution; use Test > Test Explorer for tests and Ctrl+F5 for console applications. Use F5 to attach the debugger and the chapter's breakpoint/stepping instructions. The terminal instructions below are the VS Code alternative.

Open a terminal in this Chapter19 folder, so its global.json selects Microsoft.Testing.Platform and the .NET 11 SDK.

```powershell
dotnet test --project SimpleCalculator.Tests
```

The 11 xUnit tests reference the real SimpleCalculator class library. An intentional calculator regression should fail the tests; the downloadable source is the passing version. The chapter explains the separate steps and cancellation token used by the async test.
