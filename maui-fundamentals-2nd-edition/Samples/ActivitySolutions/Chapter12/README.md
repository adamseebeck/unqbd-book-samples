# Chapter 12 completed Activity solution

Open `Inventory.slnx` in Visual Studio. Set Inventory.App as the startup project, select an available target and press F5. All solution source and resources are already included; there are no patches to assemble. Its application ID differs from the checkpoint, existing launcher and your own attempt.

Open Practice > Site inspection; run the portable shake fixture first.

For the portable check, open Test > Test Explorer and run `ActivityTests.Shake_requires_two_peaks_and_obeys_window_debounce_and_reset`. Chapter 10 teaches that workflow.

For the Windows terminal alternative, open PowerShell in `Samples` and run:

```powershell
$env:BookTargetFrameworks = "net11.0-windows10.0.19041.0"
dotnet run --project ActivitySolutions/Chapter12/Inventory.App -f net11.0-windows10.0.19041.0
```

Set the target again in each new terminal. For another configured platform, set `BookTargetFrameworks` and `-f` to the same supported target from Chapter 2. Your Visual Studio target selection remains the primary route above.

To run this solution's portable checks from `Samples`:

```powershell
dotnet test ActivitySolutions/Chapter12/Inventory.Tests
```

## Expected checks

- Run `Shake_requires_two_peaks_and_obeys_window_debounce_and_reset`; it should pass with the three-second debounce and the same 500 ms window for two peaks. Run the other portable tests too. On a native target, check consent and the visible button for clearing the list. The comparison project Activity12 keeps its two-second debounce.

`Activities/Chapter12` remains the feature comparison described in the chapter. It does not run these modified source files. Try the Activity in your own copy first, then open this complete prepared answer. Native tools, hardware, permissions and configured identity require their actual target checks.
