# Chapter 8 completed Activity solution

Open `Inventory.slnx` in Visual Studio. Set Inventory.App as the startup project, select an available target and press F5. All solution source and resources are already included; there are no patches to assemble. Its application ID differs from the checkpoint, existing launcher and your own attempt.

Open Practice > Categories; add/export and inspect the CSV file.

For the portable check, open Test > Test Explorer and run `Activity08CsvTests.NameLength_column_retains_exact_CSV_and_escaping`. Chapter 10 teaches that workflow.

For the Windows terminal alternative, open PowerShell in `Samples` and run:

```powershell
$env:BookTargetFrameworks = "net11.0-windows10.0.19041.0"
dotnet run --project ActivitySolutions/Chapter08/Inventory.App -f net11.0-windows10.0.19041.0
```

Set the target again in each new terminal. For another configured platform, set `BookTargetFrameworks` and `-f` to the same supported target from Chapter 2. Your Visual Studio target selection remains the primary route above.

To run this solution's portable checks from `Samples`:

```powershell
dotnet test ActivitySolutions/Chapter08/Inventory.Tests
```

## Expected checks

- The prepared Categories export has four columns. For row 1 with name A and description D, expect `1,"A","D",1` followed by CRLF. Keep the existing quote, formula and multiline protections. Run `NameLength_column_retains_exact_CSV_and_escaping` in Test Explorer; it should pass. The complete test project is already included.

`Activities/Chapter08` remains the feature comparison described in the chapter. It does not run these modified source files. Try the Activity in your own copy first, then open this complete prepared answer. Native tools, hardware, permissions and configured identity require their actual target checks.
