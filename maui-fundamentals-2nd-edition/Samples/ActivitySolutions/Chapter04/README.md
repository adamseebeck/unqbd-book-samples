# Chapter 4 completed Activity solution

Open `Inventory.slnx` in Visual Studio. Set Inventory.App as the startup project, select an available target and press F5. All solution source and resources are already included; there are no patches to assemble. Its application ID differs from the checkpoint, existing launcher and your own attempt.

Open Practice > Adaptive settings.

For the Windows terminal alternative, open PowerShell in `Samples` and run:

```powershell
$env:BookTargetFrameworks = "net11.0-windows10.0.19041.0"
dotnet run --project ActivitySolutions/Chapter04/Inventory.App -f net11.0-windows10.0.19041.0
```

Set the target again in each new terminal. For another configured platform, set `BookTargetFrameworks` and `-f` to the same supported target from Chapter 2. Your Visual Studio target selection remains the primary route above.

Use the native checks below for this Activity.

## Expected checks

- In the prepared solution, Adaptive settings shows Narrow at 699 units and Wide at 700 units. Resize in both directions. Check 150% text, keyboard navigation and the safe area on the targets you can run. The comparison project Activities/Chapter04 still changes at 600 units: Narrow at 599, Wide at 600.

`Activities/Chapter04` remains the feature comparison described in the chapter. It does not run these modified source files. Try the Activity in your own copy first, then open this complete prepared answer. Native tools, hardware, permissions and configured identity require their actual target checks.
