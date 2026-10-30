# Chapter 19 testing

Open a terminal in this Chapter19 folder, so its global.json selects Microsoft.Testing.Platform and the .NET 11 SDK.

```powershell
dotnet test --project SimpleCalculator.Tests
```

The 11 xUnit tests reference the real SimpleCalculator class library. An intentional calculator regression should fail the tests; the downloadable source is the passing version. The chapter explains the separate steps and cancellation token used by the async test.
