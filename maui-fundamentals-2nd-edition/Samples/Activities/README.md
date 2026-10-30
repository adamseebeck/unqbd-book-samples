# Chapter activity solutions

Keep the complete Samples tree. Each ActivityNN.csproj is a native entry configuration over the same reference source; its BookActivity metadata opens the matching exercise. The default application also exposes the exercises through its Practice page. Each entry is a complete solution; run it on the targets you have.

From Samples, this runs Chapter 3's activity on Windows; change the chapter number for another activity:

```powershell
dotnet run --project Activities/Chapter03/Activity03.csproj -p:BookTargetFrameworks=net11.0-windows10.0.19041.0 -f net11.0-windows10.0.19041.0
```

Each activity project installs as its own app with its own saved data, so relaunch the same project when an activity asks you to restart. In the finished Inventory.App, Practice > Open chapter activities lists the same exercises; each chapter below names its button. Portable tests use `dotnet test Inventory.Tests/Inventory.Tests.csproj`; to run one test, add `--filter` and its name, for example `--filter Invalid_route_id_is_rejected`.

Physical sensors, camera, permissions, assistive tools, Apple scene behavior, signing and packaging need a real target. If you can't run a target, treat its checks as untested, not passed.

## Chapter 1

Gradient handler and native lifecycle

Solution: [Inventory.App/Controls.cs](../Inventory.App/Controls.cs). Native entry: [Activity01.csproj](Chapter01/Activity01.csproj).

Run: Controls and platform; Android/Apple gradient and reconnect checks. Button: Controls and platform.

Check: no portable test covers the gradient handler; check the gradient button on each target you run.

## Chapter 2

Platform/environment verification

Solution: [Inventory.App/ActivityPages.cs](../Inventory.App/ActivityPages.cs). Native entry: [Activity02.csproj](Chapter02/Activity02.csproj).

Run: Controls and platform; background, platform/runtime labels and explicit target builds. Button: Controls and platform. The background is purple on Android and blue on Apple and Windows.

Check: the SDK, workload and native build output for each target.

## Chapter 3

Profile navigation and theming

Solution: [Inventory.App/ActivityPages.cs](../Inventory.App/ActivityPages.cs). Native entry: [Activity03.csproj](Chapter03/Activity03.csproj).

Run: Profile and theming; route, injected theme service, platform label, return visit. Button: Profile and theming.

Check: InventoryTests.Invalid_route_id_is_rejected covers the invalid IDs the page can't send, and Route_round_trip covers a valid one; check the navigation itself on a native target.

## Chapter 4

Adaptive settings

Solution: [Inventory.App/AdaptiveSettingsPage.cs](../Inventory.App/AdaptiveSettingsPage.cs). Native entry: [Activity04.csproj](Chapter04/Activity04.csproj).

Run: Adaptive settings; widths 599/600, safe area, keyboard and large text. Button: Adaptive settings.

Check: width and assistive behavior on a native target; there's no portable visual test.

## Chapter 5

Stock dashboard and paging

Solution: [Inventory.App/InventoryDashboardPage.xaml](../Inventory.App/InventoryDashboardPage.xaml). Native entry: [Activity05.csproj](Chapter05/Activity05.csproj).

Run: Stock dashboard; 9/10/50/51 stock boundaries, threshold 8 and failed-page retry. Button: Stock dashboard.

Check: Failed_page_retry_does_not_skip_the_page_and_final_page_stops_requests.

## Chapter 6

MVVM detail and DI

Solution: [Inventory.Core/ActivityDetails.cs](../Inventory.Core/ActivityDetails.cs). Native entry: [Activity06.csproj](Chapter06/Activity06.csproj).

Run: MVVM detail; positive ID, missing/absent product, refresh and departure. Button: MVVM detail.

Check: ActivityTests.Detail_rejects_invalid_and_reports_missing_without_a_raw_error covers the invalid and missing IDs.

## Chapter 7

Guarded navigation and safe restart

Solution: [Inventory.App/WorkflowPages.cs](../Inventory.App/WorkflowPages.cs). Native entry: [Activity07.csproj](Chapter07/Activity07.csproj).

Button: Navigation state. Run: Real Shell Cart/Checkout/Confirmation; invalid totals, own confirmation/back guard, explicit reset and safe Cart restart from a non-sensitive hint.

Check: portable parameter and dialog tests; check Shell and lifecycle behavior on a native target.

## Chapter 8

Categories and CSV export

Solution: [Inventory.Core/CategoryRepository.cs](../Inventory.Core/CategoryRepository.cs). Native entry: [Activity08.csproj](Chapter08/Activity08.csproj).

Run: Categories; add/reload/reopen, escaped CSV, safe timestamp after successful export. Button: Category persistence and CSV.

Check: InventoryTests.Csv_handles_quotes_commas_newlines_and_formula_like_names and Failed_export_has_no_success_timestamp cover the CSV cases. For the reopen, add a category, close the app, run Activity08 again and find the category still listed.

## Chapter 9

Durable synchronization queue

Solution: [Inventory.Core/Activities.cs](../Inventory.Core/Activities.cs). Native entry: [Activity09.csproj](Chapter09/Activity09.csproj).

Run: Offline queue; controlled failure, due-time/backoff, pending count, newer-revision acknowledgment. Button: Offline queue.

Check: Queue real SQLite persistence/backoff/revision tests.

## Chapter 10

Order viewmodel testing

Solution: [Inventory.Tests/ActivityTests.cs](../Inventory.Tests/ActivityTests.cs). Native entry: [Activity10.csproj](Chapter10/Activity10.csproj).

Run: Portable project; fictional total, one overlapping save, returned ID, invalid/canceled/failure paths.

Check: Order activity tests. They don't cover server-side idempotency, and no real order, payment or message is involved.

## Chapter 11

Metric card and scoped native shadow

Solution: [Inventory.App/MetricCard.cs](../Inventory.App/MetricCard.cs). Native entry: [Activity11.csproj](Chapter11/Activity11.csproj).

Run: Metric cards; bindable values, independent cards, theme and current native handler types. Button: Metric cards.

Check: Native build plus native rendering/assistive checks. To see value coercion in the chapter's QuantityPicker, add `new QuantityPicker { Value = -5 }` to a page in your copy of Samples; it shows 0, because its coerceValue applies `Math.Max(0, value)`.

## Chapter 12

Location/photo/shake confirmation

Solution: [Inventory.App/InspectionPage.cs](../Inventory.App/InspectionPage.cs). Native entry: [Activity12.csproj](Chapter12/Activity12.csproj).

Run: Site inspection; permission/capability, cancel, 2.5g peaks/500ms, 2s debounce, confirm-only list clear. Button: Shake-to-clear.

Check: ShakeDetector and LocationCoordinator portable tests, plus device checks on real hardware.

## Chapter 13

Public-client auth and proactive renewal

Solution: [Inventory.App/RenewalPracticePage.cs](../Inventory.App/RenewalPracticePage.cs). Native entry: [Activity13.csproj](Chapter13/Activity13.csproj).

Button: Controlled token renewal. Run: Controlled expiry/renewal clock and failure UI plus portable session tests; real sign-in requires your own registration and native setup.

Check: token, vault, cooldown and cancellation tests and the identity guide; no real provider or account is involved.

## Chapter 14

Localized accessible settings and public config

Solution: [Inventory.App/LocalizedSettingsPage.cs](../Inventory.App/LocalizedSettingsPage.cs). Native entry: [Activity14.csproj](Chapter14/Activity14.csproj).

Run: English/Spanish settings, heading label, decorative shape, large text, keyboard, contrast. Buttons: Settings English and Configuración español.

Check: RESX and configuration checks, plus assistive-technology tests on your targets.

## Chapter 15

Paging/message lifetime and measurement

Solution: [Inventory.App/BrowsingPage.cs](../Inventory.App/BrowsingPage.cs). Native entry: [Activity15.csproj](Chapter15/Activity15.csproj).

Run: Browser lifetimes; 20-row pages, departure cancellation, return visit, one active message callback. Button: Browser lifetimes.

Check: paging and cancellation tests, plus native lifetime and profiler checks.

## Chapter 16

Version/pipeline/release preparation

Solution: [Tools/update-version.ps1](../Tools/update-version.ps1). Native entry: [Activity16.csproj](Chapter16/Activity16.csproj).

Run: Disposable XML helper tests and an ordinary Release build; signing, publishing, store and calendar steps are separate.

Check: install PowerShell 7, then from your copy of Samples run `pwsh Tools/update-version.ps1 -ProjectPath Inventory.App/Inventory.App.csproj -DisplayVersion 1.0.1 -BuildNumber 2`. The project file then shows 1.0.1 and build 2. Run it again with `-BuildNumber 1`; it stops with "Build number must not decrease." Then check the target build output.
