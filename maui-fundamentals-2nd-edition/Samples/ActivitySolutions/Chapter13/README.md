# Chapter 13 completed Activity solution

Open `Inventory.slnx` in Visual Studio. Set Inventory.App as the startup project, select an available target and press F5. All solution source and resources are already included; there are no patches to assemble. Its application ID differs from the checkpoint, existing launcher and your own attempt.

Open Practice > Token renewal; run the two-minute boundary fixture first.

For the portable check, open Test > Test Explorer and run `Activity13RenewalWindowTests.Renewal_respects_two_minutes`. Chapter 10 teaches that workflow.

For the Windows terminal alternative, open PowerShell in `Samples` and run:

```powershell
$env:BookTargetFrameworks = "net11.0-windows10.0.19041.0"
dotnet run --project ActivitySolutions/Chapter13/Inventory.App -f net11.0-windows10.0.19041.0
```

Set the target again in each new terminal. For another configured platform, set `BookTargetFrameworks` and `-f` to the same supported target from Chapter 2. Your Visual Studio target selection remains the primary route above.

To run this solution's portable checks from `Samples`:

```powershell
dotnet test ActivitySolutions/Chapter13/Inventory.Tests
```

## Expected checks

- Run both theory rows: a 59-second advance reuses the session with 121 seconds remaining; a 60-second advance renews it at 120 seconds. Each row starts a new coordinator and exceeds the 30-second cooldown. The existing failure, expiry and cancellation tests should still pass. The prepared renewal page advances one minute and uses a two-minute window. The comparison page keeps its six-minute advance and five-minute window. No real identity provider is used.

`Activities/Chapter13` remains the feature comparison described in the chapter. It does not run these modified source files. Try the Activity in your own copy first, then open this complete prepared answer. Native tools, hardware, permissions and configured identity require their actual target checks.
