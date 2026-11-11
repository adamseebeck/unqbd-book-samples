# Chapter activity solutions

Keep the complete Samples tree. Each ActivityNN.csproj builds the finished app's shared source and its BookActivity metadata opens the named feature. Each chapter below identifies the completed Activity solution. Where the Activity modifies that feature, the complete prepared project is under ActivitySolutions; open its Inventory.slnx directly. The existing native entry remains a feature comparison, so it does not silently acquire the prepared solution's changed files. Make your own attempt in a separate project copy first, so MyInventory stays the same as the checkpoints.

Open a terminal in Samples and set the target once for that terminal, then run an activity. This runs Chapter 3's activity on Windows; change the chapter number for another activity:

```powershell
$env:BookTargetFrameworks = "net11.0-windows10.0.19041.0"
dotnet run --project Activities/Chapter03
```

If you prefer one line, `-p:BookTargetFrameworks=<tfm> -f <tfm>` still works, for example `dotnet run --project Activities/Chapter03 -p:BookTargetFrameworks=net11.0-windows10.0.19041.0 -f net11.0-windows10.0.19041.0`.

Each activity project installs as its own app, "Inventory activity N", with its own saved data, so relaunch the same project when an activity asks you to restart. In the finished Inventory.App, Practice > Open chapter activities lists the same exercises. From the chapter that adds a page, the Practice tab of each checkpoint and of the finished app also has a button that opens it. Each chapter below names both buttons. Portable tests use `dotnet test Inventory.Tests`; to run one test, add `--filter` and its name, for example `--filter Invalid_route_id_is_rejected`.

Physical sensors, camera, permissions, assistive tools, Apple scene behavior, signing and packaging need a real target. If you can't run a target, treat its checks as untested, not passed.

## Chapter 1

Gradient handler and native lifecycle

Completed Activity solution: [Chapter 1 prepared project](../ActivitySolutions/Chapter01/README.md). Open [Inventory.slnx](../ActivitySolutions/Chapter01/Inventory.slnx) directly.

Feature comparison [Inventory.App/Controls.cs](../Inventory.App/Controls.cs). Native entry: [Activity01.csproj](Chapter01/Activity01.csproj).

Baseline run: Controls and platform; Android/Apple gradient and reconnect checks. Button: Controls and platform, in Open chapter activities.

Check: no portable test covers the gradient handler; check the gradient button on each target you run.

Completed-solution checks: Practice has two named gradient buttons. Android/Apple both use the changed colors, retain readable pressed text and correct resizing/cleanup. Windows retains ordinary buttons as documented. No portable assertion claims native rendering.

## Chapter 2

Platform/environment verification

Completed Activity solution: [Chapter 2 prepared project](../ActivitySolutions/Chapter02/README.md). Open [Inventory.slnx](../ActivitySolutions/Chapter02/Inventory.slnx) directly.

Feature comparison [Inventory.App/ActivityPages.cs](../Inventory.App/ActivityPages.cs). Native entry: [Activity02.csproj](Chapter02/Activity02.csproj).

Baseline run: Controls and platform; background, platform/runtime labels and explicit target builds. Button: Controls and platform, in Open chapter activities. The background is purple on Android and blue on Apple and Windows.

Check: the SDK, workload and native build output for each target.

Completed-solution checks: Open prepared Inventory.slnx, choose Inventory.App/target and F5. Practice heading is Inventory device check; the platform/runtime line is still present. Repeat only on available targets and record unknowns.

## Chapter 3

Profile navigation and theming

Solution: [Inventory.App/ActivityPages.cs](../Inventory.App/ActivityPages.cs). Native entry: [Activity03.csproj](Chapter03/Activity03.csproj).

Run: Profile and theming; route, injected theme service, platform label, return visit. Button: Profile and theming, in Open chapter activities.

Check: InventoryTests.Invalid_route_id_is_rejected covers the invalid IDs the page can't send, and Route_round_trip covers a valid one; both test [Inventory.Core/ProductRoute.cs](../Inventory.Core/ProductRoute.cs). Check the navigation itself on a native target.

## Chapter 4

Adaptive settings

Completed Activity solution: [Chapter 4 prepared project](../ActivitySolutions/Chapter04/README.md). Open [Inventory.slnx](../ActivitySolutions/Chapter04/Inventory.slnx) directly.

Feature comparison [Inventory.App/AdaptiveSettingsPage.xaml.cs](../Inventory.App/AdaptiveSettingsPage.xaml.cs), with its layout in [AdaptiveSettingsPage.xaml](../Inventory.App/AdaptiveSettingsPage.xaml). Native entry: [Activity04.csproj](Chapter04/Activity04.csproj).

Baseline run: Adaptive settings; widths 599/600, safe area, keyboard and large text. Button: Adaptive settings, in Practice and in Open chapter activities.

Check: width and assistive behavior on a native target; there's no portable visual test.

Completed-solution checks: In the prepared solution, Adaptive settings shows Narrow at 699 units and Wide at 700 units. Resize in both directions. Check 150% text, keyboard navigation and the safe area on the targets you can run. The comparison project Activities/Chapter04 still changes at 600 units: Narrow at 599, Wide at 600.

## Chapter 5

Stock dashboard and paging

Completed Activity solution: [Chapter 5 prepared project](../ActivitySolutions/Chapter05/README.md). Open [Inventory.slnx](../ActivitySolutions/Chapter05/Inventory.slnx) directly.

Feature comparison [Inventory.App/InventoryDashboardPage.xaml](../Inventory.App/InventoryDashboardPage.xaml), with its paging view model in [Inventory.Core/PagedProductsViewModel.cs](../Inventory.Core/PagedProductsViewModel.cs). Native entry: [Activity05.csproj](Chapter05/Activity05.csproj).

Baseline run: Stock dashboard; 9/10/50/51 stock boundaries, threshold 8 and failed-page retry. Button: Stock dashboard, in Practice and in Open chapter activities.

Check: Failed_page_retry_does_not_skip_the_page_and_final_page_stops_requests.

Completed-solution checks: On Stock dashboard, product 1 has quantity 10 and shows Low with the orange border. Product 2 has quantity 50 and remains Moderate. Check the 19/20 boundary, failed-page retry, cancellation and spoken text on the targets you can run.

## Chapter 6

MVVM detail and DI

Solution: [Inventory.Core/ActivityDetails.cs](../Inventory.Core/ActivityDetails.cs). Native entry: [Activity06.csproj](Chapter06/Activity06.csproj).

Run: MVVM detail; positive ID, missing/absent product, refresh and departure. Button: MVVM detail, in Open chapter activities.

Check: ActivityTests.Detail_rejects_invalid_and_reports_missing_without_a_raw_error covers the invalid and missing IDs.

## Chapter 7

Guarded navigation and safe restart

Completed Activity solution: [Chapter 7 prepared project](../ActivitySolutions/Chapter07/README.md). Open [Inventory.slnx](../ActivitySolutions/Chapter07/Inventory.slnx) directly.

Feature comparison [Inventory.App/WorkflowPages.cs](../Inventory.App/WorkflowPages.cs). Native entry: [Activity07.csproj](Chapter07/Activity07.csproj).

Buttons: Checkout workflow in Practice, and Navigation state in Open chapter activities. Run: Real Shell Cart/Checkout/Confirmation; invalid totals, own confirmation/back guard, explicit reset and safe Cart restart from a non-sensitive hint.

Check: portable parameter and dialog tests; check Shell and lifecycle behavior on a native target.

Completed-solution checks: In the ordinary prepared AppShell, open Settings. A fresh store says No stop recorded. Background/restore, revisit Settings and observe the saved timestamp. Revisit after another stop refreshes it. Termination need not invoke Stopped. Existing Activity07 launcher remains the Cart workflow baseline and has no Settings flyout.

## Chapter 8

Categories and CSV export

Completed Activity solution: [Chapter 8 prepared project](../ActivitySolutions/Chapter08/README.md). Open [Inventory.slnx](../ActivitySolutions/Chapter08/Inventory.slnx) directly.

Feature comparison [Inventory.Core/CategoryRepository.cs](../Inventory.Core/CategoryRepository.cs), with the CSV export in [Inventory.Core/Category.cs](../Inventory.Core/Category.cs) and the page in [Inventory.App/CategoriesPage.cs](../Inventory.App/CategoriesPage.cs). Native entry: [Activity08.csproj](Chapter08/Activity08.csproj).

Baseline run: Categories; add/reload/reopen, escaped CSV, safe timestamp after successful export. The page's Export file: line shows the path of categories.csv, so you can open the exported file. Buttons: Categories in Practice, and Category persistence and CSV in Open chapter activities.

Check: InventoryTests.Csv_handles_quotes_commas_newlines_and_formula_like_names and Failed_export_has_no_success_timestamp cover the CSV cases. For the reopen, add a category, close the app, run `dotnet run --project Activities/Chapter08` again and find the category still listed.

Completed-solution checks: The prepared Categories export has four columns. For row 1 with name A and description D, expect `1,"A","D",1` followed by CRLF. Keep the existing quote, formula and multiline protections. Run `NameLength_column_retains_exact_CSV_and_escaping` in Test Explorer; it should pass. The complete test project is already included.

## Chapter 9

Durable synchronization queue

Solution: [Inventory.Core/SyncQueue.cs](../Inventory.Core/SyncQueue.cs). Native entry: [Activity09.csproj](Chapter09/Activity09.csproj).

Run: Offline queue; controlled failure, due time and backoff, pending count, newer-revision acknowledgment, and a second Enqueue button for Product 2 (the Activity's code task). Button: Offline queue, in Practice and in Open chapter activities.

Check: Queue real SQLite persistence/backoff/revision tests.

## Chapter 10

Order viewmodel testing

Solution: [Inventory.Tests/ActivityTests.cs](../Inventory.Tests/ActivityTests.cs), which tests the order view model in [Inventory.Core/Activities.cs](../Inventory.Core/Activities.cs). Native entry: [Activity10.csproj](Chapter10/Activity10.csproj).

Run: Portable project; fictional total, one overlapping save, returned ID, invalid/canceled/failure paths.

Check: Order activity tests. They don't cover server-side idempotency, and no real order, payment or message is involved.

## Chapter 11

Metric card and scoped native shadow

Solution: [Inventory.App/MetricCard.cs](../Inventory.App/MetricCard.cs). Native entry: [Activity11.csproj](Chapter11/Activity11.csproj).

Run: Metric cards; bindable values, independent cards, theme and current native handler types, and a fourth Reorder card with its own Change reorder count only button (the Activity's last task). Button: Metric cards, in Practice and in Open chapter activities.

Check: Native build plus native rendering/assistive checks. To see value coercion in the chapter's QuantityPicker, add `new QuantityPicker { Value = -5 }` to a page in your copy of the project; it shows 0, because its coerceValue applies `Math.Max(0, value)`.

## Chapter 12

Location/photo/shake confirmation

Completed Activity solution: [Chapter 12 prepared project](../ActivitySolutions/Chapter12/README.md). Open [Inventory.slnx](../ActivitySolutions/Chapter12/Inventory.slnx) directly.

Feature comparison [Inventory.App/InspectionPage.cs](../Inventory.App/InspectionPage.cs), with the location logic in [Inventory.Core/LocationCoordinator.cs](../Inventory.Core/LocationCoordinator.cs). Native entry: [Activity12.csproj](Chapter12/Activity12.csproj).

Baseline run: Site inspection; permission/capability, cancel, 2.5g peaks/500ms, 2s debounce, confirm-only list clear. Buttons: Site inspection in Practice, and Shake-to-clear in Open chapter activities.

Check: ShakeDetector and LocationCoordinator portable tests, plus device checks on real hardware.

Completed-solution checks: Run `Shake_requires_two_peaks_and_obeys_window_debounce_and_reset`; it should pass with the three-second debounce and the same 500 ms window for two peaks. Run the other portable tests too. On a native target, check consent and the visible button for clearing the list. The comparison project Activity12 keeps its two-second debounce.

## Chapter 13

Public-client auth and proactive renewal

Completed Activity solution: [Chapter 13 prepared project](../ActivitySolutions/Chapter13/README.md). Open [Inventory.slnx](../ActivitySolutions/Chapter13/Inventory.slnx) directly.

Feature comparison [Inventory.App/RenewalPracticePage.cs](../Inventory.App/RenewalPracticePage.cs), with the renewal logic in [Inventory.Core/AccessTokenCoordinator.cs](../Inventory.Core/AccessTokenCoordinator.cs). Native entry: [Activity13.csproj](Chapter13/Activity13.csproj).

Buttons: Token renewal in Practice, and Controlled token renewal in Open chapter activities. Run: Controlled expiry/renewal clock and failure UI plus portable session tests; real sign-in requires your own registration and native setup.

Check: token, vault, cooldown and cancellation tests and the identity guide; no real provider or account is involved.

Completed-solution checks: Run both theory rows: a 59-second advance reuses the session with 121 seconds remaining; a 60-second advance renews it at 120 seconds. Each row starts a new coordinator and exceeds the 30-second cooldown. The existing failure, expiry and cancellation tests should still pass. The prepared renewal page advances one minute and uses a two-minute window. The comparison page keeps its six-minute advance and five-minute window. No real identity provider is used.

## Chapter 14

Localized accessible settings and public config

Solution: [Inventory.App/LocalizedSettingsPage.cs](../Inventory.App/LocalizedSettingsPage.cs). Native entry: [Activity14.csproj](Chapter14/Activity14.csproj).

Run: English/Spanish settings, heading label, decorative shape, large text, keyboard, contrast, and the Activity's Footer line from both .resx files. The compact view switch uses the same `compact_view` preference as the Settings page. Buttons: Localized settings and Configuración español in Practice, and Settings English and Configuración español in Open chapter activities.

Check: RESX and configuration checks, plus assistive-technology tests on your targets.

## Chapter 15

Paging/message lifetime and measurement

Completed Activity solution: [Chapter 15 prepared project](../ActivitySolutions/Chapter15/README.md). Open [Inventory.slnx](../ActivitySolutions/Chapter15/Inventory.slnx) directly.

Feature comparison [Inventory.App/BrowsingPage.cs](../Inventory.App/BrowsingPage.cs). Native entry: [Activity15.csproj](Chapter15/Activity15.csproj).

Baseline run: Browser lifetimes; 20-row pages, departure cancellation, return visit, one active message callback. Buttons: Browsing in Practice, and Browser lifetimes in Open chapter activities.

Check: paging and cancellation tests, plus native lifetime and profiler checks.

Completed-solution checks: Run the cache test. Two 5-byte results exceed the 8-byte limit, so requesting the first key again must decode it again. Changing a returned array must not change a retained cache hit. Run the existing portable suite and the native Browser lifetime checks too.

## Chapter 16

Version/pipeline/release preparation

Solution: [Tools/update-version.ps1](../Tools/update-version.ps1). Native entry: [Activity16.csproj](Chapter16/Activity16.csproj).

Run: The update-version.ps1 commands and an ordinary Release build; signing, publishing and store steps are separate.

Check: install PowerShell 7, then from Samples copy your project with `Copy-Item MyInventory ReleaseCopy -Recurse` and run `pwsh Tools/update-version.ps1 -ProjectPath ReleaseCopy/Inventory.App/Inventory.App.csproj -DisplayVersion 1.0.1 -BuildNumber 2`. The project file then shows 1.0.1 and build 2. Run it again with `-BuildNumber 1`; it stops with "Build number must not decrease." Then check the target build output.
