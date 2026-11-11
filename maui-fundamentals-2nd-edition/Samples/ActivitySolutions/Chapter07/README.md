# Chapter 7 completed Activity solution

Open `Inventory.slnx` in Visual Studio. Set Inventory.App as the startup project, select an available target and press F5. All solution source and resources are already included; there are no patches to assemble. Its application ID differs from the checkpoint, existing launcher and your own attempt.

Open the flyout menu > Settings; background/restore and revisit Settings.

For the Windows terminal alternative, open PowerShell in `Samples` and run:

```powershell
$env:BookTargetFrameworks = "net11.0-windows10.0.19041.0"
dotnet run --project ActivitySolutions/Chapter07/Inventory.App -f net11.0-windows10.0.19041.0
```

Set the target again in each new terminal. For another configured platform, set `BookTargetFrameworks` and `-f` to the same supported target from Chapter 2. Your Visual Studio target selection remains the primary route above.

Use the native checks below for this Activity.

## Expected checks

- In the ordinary prepared AppShell, open Settings. A fresh store says No stop recorded. Background/restore, revisit Settings and observe the saved timestamp. Revisit after another stop refreshes it. Termination need not invoke Stopped. Existing Activity07 launcher remains the Cart workflow baseline and has no Settings flyout.

`Activities/Chapter07` remains the feature comparison described in the chapter. It does not run these modified source files. Try the Activity in your own copy first, then open this complete prepared answer. Native tools, hardware, permissions and configured identity require their actual target checks.
