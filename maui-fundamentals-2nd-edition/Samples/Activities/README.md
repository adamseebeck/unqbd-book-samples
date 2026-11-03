# Chapter activity solutions

Keep the complete Samples tree. Each ActivityNN.csproj builds the finished app's source in Inventory.App and Inventory.Core, and its BookActivity metadata opens the matching exercise directly. Each entry is a complete solution; run it on the targets you have. Do your own attempt in a separate copy of your project, such as `Copy-Item MyInventory MyActivity03 -Recurse` from Samples, so MyInventory stays the same as the checkpoints.

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

Solution: [Inventory.App/Controls.cs](../Inventory.App/Controls.cs). Native entry: [Activity01.csproj](Chapter01/Activity01.csproj).

Run: Controls and platform; Android/Apple gradient and reconnect checks. Button: Controls and platform, in Open chapter activities.

Check: no portable test covers the gradient handler; check the gradient button on each target you run.

## Chapter 2

Platform/environment verification

Solution: [Inventory.App/ActivityPages.cs](../Inventory.App/ActivityPages.cs). Native entry: [Activity02.csproj](Chapter02/Activity02.csproj).

Run: Controls and platform; background, platform/runtime labels and explicit target builds. Button: Controls and platform, in Open chapter activities. The background is purple on Android and blue on Apple and Windows.

Check: the SDK, workload and native build output for each target.

## Chapter 3

Profile navigation and theming

Solution: [Inventory.App/ActivityPages.cs](../Inventory.App/ActivityPages.cs). Native entry: [Activity03.csproj](Chapter03/Activity03.csproj).

Run: Profile and theming; route, injected theme service, platform label, return visit. Button: Profile and theming, in Open chapter activities.

Check: InventoryTests.Invalid_route_id_is_rejected covers the invalid IDs the page can't send, and Route_round_trip covers a valid one; both test [Inventory.Core/ProductRoute.cs](../Inventory.Core/ProductRoute.cs). Check the navigation itself on a native target.

## Chapter 4

Adaptive settings

Solution: [Inventory.App/AdaptiveSettingsPage.xaml.cs](../Inventory.App/AdaptiveSettingsPage.xaml.cs), with its layout in [AdaptiveSettingsPage.xaml](../Inventory.App/AdaptiveSettingsPage.xaml). Native entry: [Activity04.csproj](Chapter04/Activity04.csproj).

Run: Adaptive settings; widths 599/600, safe area, keyboard and large text. Button: Adaptive settings, in Practice and in Open chapter activities.

Check: width and assistive behavior on a native target; there's no portable visual test.

## Chapter 5

Stock dashboard and paging

Solution: [Inventory.App/InventoryDashboardPage.xaml](../Inventory.App/InventoryDashboardPage.xaml), with its paging view model in [Inventory.Core/PagedProductsViewModel.cs](../Inventory.Core/PagedProductsViewModel.cs). Native entry: [Activity05.csproj](Chapter05/Activity05.csproj).

Run: Stock dashboard; 9/10/50/51 stock boundaries, threshold 8 and failed-page retry. Button: Stock dashboard, in Practice and in Open chapter activities.

Check: Failed_page_retry_does_not_skip_the_page_and_final_page_stops_requests.

## Chapter 6

MVVM detail and DI

Solution: [Inventory.Core/ActivityDetails.cs](../Inventory.Core/ActivityDetails.cs). Native entry: [Activity06.csproj](Chapter06/Activity06.csproj).

Run: MVVM detail; positive ID, missing/absent product, refresh and departure. Button: MVVM detail, in Open chapter activities.

Check: ActivityTests.Detail_rejects_invalid_and_reports_missing_without_a_raw_error covers the invalid and missing IDs.

## Chapter 7

Guarded navigation and safe restart

Solution: [Inventory.App/WorkflowPages.cs](../Inventory.App/WorkflowPages.cs). Native entry: [Activity07.csproj](Chapter07/Activity07.csproj).

Buttons: Checkout workflow in Practice, and Navigation state in Open chapter activities. Run: Real Shell Cart/Checkout/Confirmation; invalid totals, own confirmation/back guard, explicit reset and safe Cart restart from a non-sensitive hint.

Check: portable parameter and dialog tests; check Shell and lifecycle behavior on a native target.

## Chapter 8

Categories and CSV export

Solution: [Inventory.Core/CategoryRepository.cs](../Inventory.Core/CategoryRepository.cs), with the CSV export in [Inventory.Core/Category.cs](../Inventory.Core/Category.cs) and the page in [Inventory.App/CategoriesPage.cs](../Inventory.App/CategoriesPage.cs). Native entry: [Activity08.csproj](Chapter08/Activity08.csproj).

Run: Categories; add/reload/reopen, escaped CSV, safe timestamp after successful export. The page's Export file: line shows the path of categories.csv, so you can open the exported file. Buttons: Categories in Practice, and Category persistence and CSV in Open chapter activities.

Check: InventoryTests.Csv_handles_quotes_commas_newlines_and_formula_like_names and Failed_export_has_no_success_timestamp cover the CSV cases. For the reopen, add a category, close the app, run `dotnet run --project Activities/Chapter08` again and find the category still listed.

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

Solution: [Inventory.App/InspectionPage.cs](../Inventory.App/InspectionPage.cs), with the location logic in [Inventory.Core/LocationCoordinator.cs](../Inventory.Core/LocationCoordinator.cs). Native entry: [Activity12.csproj](Chapter12/Activity12.csproj).

Run: Site inspection; permission/capability, cancel, 2.5g peaks/500ms, 2s debounce, confirm-only list clear. Buttons: Site inspection in Practice, and Shake-to-clear in Open chapter activities.

Check: ShakeDetector and LocationCoordinator portable tests, plus device checks on real hardware.

## Chapter 13

Public-client auth and proactive renewal

Solution: [Inventory.App/RenewalPracticePage.cs](../Inventory.App/RenewalPracticePage.cs), with the renewal logic in [Inventory.Core/AccessTokenCoordinator.cs](../Inventory.Core/AccessTokenCoordinator.cs). Native entry: [Activity13.csproj](Chapter13/Activity13.csproj).

Buttons: Token renewal in Practice, and Controlled token renewal in Open chapter activities. Run: Controlled expiry/renewal clock and failure UI plus portable session tests; real sign-in requires your own registration and native setup.

Check: token, vault, cooldown and cancellation tests and the identity guide; no real provider or account is involved.

## Chapter 14

Localized accessible settings and public config

Solution: [Inventory.App/LocalizedSettingsPage.cs](../Inventory.App/LocalizedSettingsPage.cs). Native entry: [Activity14.csproj](Chapter14/Activity14.csproj).

Run: English/Spanish settings, heading label, decorative shape, large text, keyboard, contrast, and the Activity's Footer line from both .resx files. The compact view switch uses the same `compact_view` preference as the Settings page. Buttons: Localized settings and Configuración español in Practice, and Settings English and Configuración español in Open chapter activities.

Check: RESX and configuration checks, plus assistive-technology tests on your targets.

## Chapter 15

Paging/message lifetime and measurement

Solution: [Inventory.App/BrowsingPage.cs](../Inventory.App/BrowsingPage.cs). Native entry: [Activity15.csproj](Chapter15/Activity15.csproj).

Run: Browser lifetimes; 20-row pages, departure cancellation, return visit, one active message callback. Buttons: Browsing in Practice, and Browser lifetimes in Open chapter activities.

Check: paging and cancellation tests, plus native lifetime and profiler checks.

## Chapter 16

Version/pipeline/release preparation

Solution: [Tools/update-version.ps1](../Tools/update-version.ps1). Native entry: [Activity16.csproj](Chapter16/Activity16.csproj).

Run: The update-version.ps1 commands and an ordinary Release build; signing, publishing and store steps are separate.

Check: install PowerShell 7, then from Samples copy your project with `Copy-Item MyInventory ReleaseCopy -Recurse` and run `pwsh Tools/update-version.ps1 -ProjectPath ReleaseCopy/Inventory.App/Inventory.App.csproj -DisplayVersion 1.0.1 -BuildNumber 2`. The project file then shows 1.0.1 and build 2. Run it again with `-BuildNumber 1`; it stops with "Build number must not decrease." Then check the target build output.
