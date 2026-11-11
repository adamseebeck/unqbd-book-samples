# Chapter 15 completed Activity solution

Open `Inventory.slnx` in Visual Studio. Set Inventory.App as the startup project, select an available target and press F5. All solution source and resources are already included; there are no patches to assemble. Its application ID differs from the checkpoint, existing launcher and your own attempt.

Run the cache fixture in Test Explorer, then open Practice > Browsing for the existing lifetime checks.

For the portable check, open Test > Test Explorer and run `Activity15CacheTests.Cache_evicts_and_isolates`. Chapter 10 teaches that workflow.

For the Windows terminal alternative, open PowerShell in `Samples` and run:

```powershell
$env:BookTargetFrameworks = "net11.0-windows10.0.19041.0"
dotnet run --project ActivitySolutions/Chapter15/Inventory.App -f net11.0-windows10.0.19041.0
```

Set the target again in each new terminal. For another configured platform, set `BookTargetFrameworks` and `-f` to the same supported target from Chapter 2. Your Visual Studio target selection remains the primary route above.

To run this solution's portable checks from `Samples`:

```powershell
dotnet test ActivitySolutions/Chapter15/Inventory.Tests
```

## Expected checks

- Run the cache test. Two 5-byte results exceed the 8-byte limit, so requesting the first key again must decode it again. Changing a returned array must not change a retained cache hit. Run the existing portable suite and the native Browser lifetime checks too.

`Activities/Chapter15` remains the feature comparison described in the chapter. It does not run these modified source files. Try the Activity in your own copy first, then open this complete prepared answer. Native tools, hardware, permissions and configured identity require their actual target checks.
