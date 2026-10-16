# Phase 18 Parts 2.3.1–2.3.2 — Operations shell and dashboard

## Phase 18 Part 2.3.4 — Live map and resource command center (2026-10-16)

**Implementation checkpoint: code changes complete; visual and authorized runtime verification blocked.** No screenshots, commit, push, merge, or deployment were made.

### Changes

- The map and resource command center now show the server-retrieved UTC snapshot time. Manual refresh reports loading, success, and failure; failed refreshes retain the last successful values and timestamp.
- The map no longer advertises a live connection by default. Its connection badge follows SignalR start, reconnect, and close outcomes. Empty/no-coordinate results and an unavailable map configuration have explicit messages. The Leaflet map height adapts across viewport sizes, its legend includes the situation marker, the incident drawer expands to the mobile viewport, and map animation respects reduced-motion preferences.
- The resource grid remains Syncfusion-backed with its original fields, paging, sorting, filters, column chooser, and operational actions. Resource names and departments are HTML-encoded in templates; action buttons use escaped data attributes and delegated handlers instead of inline JavaScript. API error messages now render as text.
- Resource details now have a breadcrumb, a clearer resource heading/status/action layout, and display an unknown department as `Unknown` when missing. Role rules, antiforgery tokens, lifecycle commands, and server-side transition checks were not changed.

### Files changed

`src/EmergencyResponse.Web/Controllers/MapController.cs`, `src/EmergencyResponse.Web/Controllers/ResourcesController.cs`, `src/EmergencyResponse.Web/Models/Map/EmergencyMapViewModel.cs`, `src/EmergencyResponse.Web/Models/Resources/ResourceCommandCenterViewModel.cs`, `src/EmergencyResponse.Web/Views/Map/Index.cshtml`, `src/EmergencyResponse.Web/Views/Resources/Index.cshtml`, `src/EmergencyResponse.Web/Views/Resources/Details.cshtml`, `src/EmergencyResponse.Web/wwwroot/css/emergency-map.css`, `src/EmergencyResponse.Web/wwwroot/css/site.css`, `src/EmergencyResponse.Web/wwwroot/js/emergency-map.js`, `src/EmergencyResponse.Web/wwwroot/js/incident-realtime.js`, and `scripts/tests/resource-command-center.test.cjs`.

### Checks actually run

| Check | Result |
|---|---|
| `dotnet build AI-Emergency-Response.slnx --configuration Release --no-restore` | Passed, 0 errors, 18 warnings (existing NuGet vulnerability advisories and nullable warnings). |
| `dotnet test tests/EmergencyResponse.UnitTests/EmergencyResponse.UnitTests.csproj --configuration Release --no-build` | Passed: 569 passed, 0 failed, 0 skipped. |
| `node --test scripts/tests/resource-command-center.test.cjs scripts/tests/dashboard-ui.test.cjs` | Passed: 28 checks after the final resource rendering assertion. |
| `node --check` for `emergency-map.js` and `incident-realtime.js` | Passed. |
| `git diff --check` | Passed; Git printed line-ending normalization notices only. |

The focused resource test first reproduced three failures before the resource fixes: a false initial refresh timestamp, no visible failed-refresh status, and raw model interpolation into the Syncfusion templates.

### Unverified / blocked

- Browser policy denied access to the open `https://localhost:7201/` page and required no retries through alternate surfaces. Therefore no visual or responsive inspection was completed at 1440×900, 1024×768, or 390×844. Map filters/layers, drawer interactions, grid sorting/filtering/paging, keyboard focus, console/network behavior, and actual SignalR recovery were not browser-verified. No screenshots were captured.
- Authorized MVC/snapshot checks were not run because they require a reachable local application and authorized demo session; accessing the denied app URL through another route was not attempted.
- SQL integration tests were not run: `ConnectionStrings__TestDatabase` is unset in this environment. The integration harness requires the isolated `EmergencyResponse_IntegrationTests` database.
- No live resource lifecycle action was issued, so lifecycle outcomes remain unverified in this pass.

Recommended commit after the outstanding runtime checks: `feat: polish live map and resource command center`.

Date: 2026-10-15. Branch: `docs/phase18-portfolio-release`; existing PR #17.

**Status: COMPLETE — engineering checkpoint accepted by the project owner on 2026-10-15, with browser verification explicitly deferred.** Implementation and local regression results are recorded below. Browser rendering and responsive acceptance have not been verified; their execution status remains BLOCKED.

## Checkpoint acceptance

The owner requested: “mark this phase parts as complete and make the necessary git commit and push.” This closes Parts 2.3.1–2.3.2 as the accepted development checkpoint and authorizes publication to the existing PR #17 branch. It does not convert pending visual, keyboard/touch, chart/grid interaction or SignalR recovery checks into passing results.

The browser/viewport checklist and remaining issues below are carried forward to the next verification pass. No screenshots or Azure deployments occurred. PR #17 remains a review checkpoint; no merge or operational launch is authorized by this closure.

## Baseline and scope

The local branch started clean at `7cf818f` (`fix: stabilize demo workflows and record phase 2.2 verification`). Git status, recent commits, the relevant views/styles/scripts, dashboard service/model/client, role rules and existing authorized MVC checks were inspected. Earlier DEMO18 work remains intact. Local configuration and passwords were neither replaced nor included in evidence.

This pass changes the shared shell, reusable styles and dashboard. Incident details, maps, resource management and Copilot have not received a screen redesign. Shared typography, surfaces and form styles also apply to those existing pages, so their visual compatibility still needs browser review.

## Files changed

| Area | Files |
|---|---|
| Shared theme | `src/EmergencyResponse.Web/wwwroot/css/site.css`, `src/EmergencyResponse.Web/Views/Shared/_Layout.cshtml.css` |
| Application shell | `src/EmergencyResponse.Web/Views/Shared/_Layout.cshtml` |
| Dashboard view | `src/EmergencyResponse.Web/Views/Dashboard/Index.cshtml` |
| Client behavior | `src/EmergencyResponse.Web/wwwroot/js/incident-realtime.js`, `notifications.js`, new `dashboard-outlook.js` in the same directory |
| Existing authenticated client/bridge | `src/EmergencyResponse.Web/Controllers/DashboardController.cs`, `src/EmergencyResponse.Web/Services/DashboardApiClient.cs`, `IDashboardApiClient.cs` |
| Regression checks | `scripts/verify-demo-pages.ps1`, new `scripts/tests/dashboard-ui.test.cjs`, new `tests/EmergencyResponse.UnitTests/Web/DashboardForecastTests.cs` |
| Verification report | `docs/portfolio/ui-ux-verification.md` |

All local orchestration scripts, logs, TRX and runtime-result files are under ignored `artifacts/` or `TestResults/`. No local `.env` or secret file is part of this change.

## Design system and conventions

- `--er-*` CSS tokens define navy brand colors, canvas/surfaces, text, borders, spacing, corner radii, focus rings and restrained semantic severity colors.
- System fonts, a light canvas, white cards and small shadows. No image/font service is required for the dashboard. The shell's shield and bell are inline SVG; the existing Bootstrap Icons CDN reference remains for other pages.
- `er-page-header`, `er-page-actions`, `er-section-title`, `er-panel`, `er-metric`, `er-summary-line`, `er-empty-state` and `er-severity-*` provide reusable components.
- Severity and review tags include text; critical severity, high severity/priority and AI/duplicate/visual review remain distinct.
- Existing Bootstrap assets and Syncfusion Fluent 2 are retained. Custom tokens load after the vendor theme. Existing component scripts, `ejs-scripts`, jQuery, Bootstrap, SignalR, notifications and page sections retain their order.
- The absolute-positioned template footer and isolated template color overrides were removed. A flex page shell keeps the footer in normal document flow.

## Application shell

The visible product name is **Emergency Response**. Navigation has an active state and `aria-current="page"` derived from controller/action. Incident details/review routes select Incidents; Create and My Reports retain their own states.

The Bootstrap navigation collapses below 1200px, with one matching `primaryNavigation` target, labelled toggle, wrapping auth controls and a viewport-bounded notification dropdown. The existing role conditions for Citizen, Responder, Operator, EmergencyAdmin and SystemAdmin are retained. No custom navigation JavaScript was introduced.

A skip link targets the main landmark. Focus outlines remain visible. Notifications have an accessible bell name that includes the actual unread count or explicitly says the count is unavailable. HTTP failures in the dropdown now leave a failure message rather than an endless loading state. Logout, antiforgery, login/register, flash messages, unread badge and notification actions remain in place.

## Dashboard structure and data semantics

1. **Emergency Operations Center:** heading, explanatory subtitle, live-map link, explicit overview refresh, separate connection and snapshot status.
2. **Four primary metrics:** Critical incidents, High-priority incidents, Active incidents and Unassigned incidents.
3. **Operational review:** four compact groups for human review, escalations, situations and notifications. All fifteen previous secondary metric IDs/counts and existing links are retained.
4. **Incident queue:** existing Syncfusion grid, search, column chooser, filter menus, paging, sorting, resizing, reordering, frozen incident column, detail/map actions and selected-incident hook. A keyboard-focusable scroll region contains the wide grid on mobile.
5. **Secondary insights:** existing severity distribution, seven-day incident trend, review queue, operational notes and predictive outlook.

Important boundaries:

- `Summary.HighIncidents` means **high severity**, not high priority. The primary high-priority card counts `Priority == High` in the authoritative displayed queue, explicitly labelled **up to 100 reports**. High severity overall remains visible and keeps `highIncidentCount`. This is not a new global API aggregate.
- The current queue source returns the latest reports, including terminal states. Its subtitle says so and directs the operator to filter Status. Critical/high severity summary counts also include all reported incidents; Active excludes Closed/Rejected. No API query semantics were changed.
- Notification dashboard totals come from the existing operational summary. They are not presented as the signed-in user's inbox count; the bell/inbox retain recipient authorization. A recipient-scoped dashboard aggregate would be separate work.
- A zero pending review count does not establish that analysis ran. Pending-review item totals may overlap. DEMO18 severity/priority values remain intake defaults; this pass does not fabricate triage.
- There is no reliable application-wide demo-mode flag in the inspected dashboard contract. No demo badge is inferred from incident titles.
- Connection success is separate from snapshot freshness. Failed/incomplete snapshots retain prior data, label it stale and do not invent a new retrieval timestamp. Manual refresh works independently of a live SignalR connection.
- Grid/chart components remain instantiated when empty so later valid snapshots can populate them. No-incidents/no-reviews messages are neutral rather than green success alerts.

## Direct functional fixes

The former forecast widget fetched `/api/predictive/...` from the **Web** origin, which has no corresponding controller or reverse proxy. Two role-protected Web routes now reuse the existing `IDashboardApiClient` registration and its bearer-token handler:

- `/Dashboard/IncidentVolume` → existing API incident-volume endpoint, 24-hour horizon.
- `/Dashboard/Hotspots` → existing API hotspot endpoint, 24-hour window.

Responses are not cached. Authorization failures remain failures; network/JSON failures return a sanitized unavailable response. Tokens and connection details are not sent to browser scripts. API business logic, permissions and startup behavior were not changed.

The widget validates forecast availability, numeric points, generation time and hotspot trend data. Failure clears stale forecast numbers; resource shortages show **Not assessed** because no resource-demand query was made. A real hotspot trend can remain visible when the volume model is unavailable. Hotspots are labelled as recent report activity, **not a model forecast**.

The existing API forecast GET can persist forecast snapshots. This is existing application behavior, not DEMO18 fixture ownership; demo cleanup safeguards remain unchanged.

## Executed checks and results

| Check | Actual result | Local evidence |
|---|---|---|
| Initial Release build | PASS, 0 errors, 18 existing warnings | `artifacts/portfolio/local-e2e/ui-build.log` |
| Final Release solution build | PASS, 0 errors, 15 existing warnings | `artifacts/portfolio/local-e2e/ui-complete-build.log` |
| Full unit suite | PASS, 569 passed, 0 failed/skipped; includes 12 new forecast client/controller cases | `TestResults/PortfolioUI/UnitComplete/unit-tests.trx` |
| UI JavaScript logic/DOM contracts | PASS, 22 passed, 0 failed/skipped | `artifacts/portfolio/local-e2e/ui-js-tests.log` |
| SQL suite on retained test DB | FAIL, 88 passed, 1 situation-worker timeout | `TestResults/PortfolioUI/Integration/integration-tests.trx` |
| SQL suite on fresh isolated test DB | PASS, 89 passed, 0 failed/skipped | `TestResults/PortfolioUI/IntegrationFresh/integration-tests.trx` |
| Authorized MVC/snapshot/access checks | PASS, 30 checks using actual cookies, CSRF and Web-to-API JWT | `artifacts/portfolio/local-e2e/ui-mvc.json` |
| Runtime routes/health/assets | PASS, 10 checks; all HTTP 200, payload details below | `artifacts/portfolio/local-e2e/ui-runtime.json` |
| `git diff --check` | PASS | Final working-tree check |

The initial SQL failure was a 30-second timeout waiting for exactly three situation members on a database containing prior test records. A clean run passed. This supports a retained-fixture interference explanation; it does not prove restart/durable-worker recovery. The failed result is retained. Only the disposable `EmergencyResponse_IntegrationTests` database on the verified local SQL Server 2025 container was recreated. `EmergencyResponse_PortfolioDemo`, LOADTEST data and other databases were not reset. An initial harness target guard refused to reset anything until its PowerShell connection-string parser was corrected.

Build warnings include existing cryptography package advisories and unrelated nullable warnings. Dependency remediation was not expanded into this UI pass.

### Observed server responses

- Citizen, Operator and SystemAdmin real-cookie sessions passed the existing demo checks. Operator/SystemAdmin dashboard HTML contains the new shell, selected navigation, KPI IDs, grid region and all chart hooks.
- Citizen is denied at `/Dashboard`, `/Dashboard/Snapshot`, `/Dashboard/IncidentVolume` and `/Dashboard/Hotspots`. Existing anonymous login and Citizen resource restrictions still pass.
- SystemAdmin dashboard snapshot contains at least the six DEMO18 reports, the summary and a real retrieval timestamp. Anonymous login HTML has login/register navigation and no notification bell.
- API `/health/live` and `/health/ready`: HTTP 200.
- Real volume endpoint: HTTP 200, `isAvailable=false`, zero points, reason **at least 336 observations and 20 non-zero observations are required**. This is an explicit unavailable model, not a forecast of zero incidents.
- Real hotspot endpoint: HTTP 200, one Increasing cluster. Source inspection shows this is a deterministic recent/historical report heuristic, not a successfully trained volume model.
- Custom CSS/JS, local Bootstrap bundle, Syncfusion Fluent 2 CSS and grid script are served successfully. Delivery does not verify component rendering.

The JavaScript suite executes failure/success behavior in a small DOM harness: independent priority/severity counts, retained grid instance, selected-row restoration, reduced-motion scrolling, invalid snapshots, stale values, partial forecast availability, missing demand assessment, notification error states, script order and all legacy IDs. Seven design-token foreground/background pairs meet the calculated WCAG AA 4.5:1 normal-text threshold. This is **not** a browser contrast or complete accessibility audit.

Execution used `dotnet build AI-Emergency-Response.slnx --configuration Release --no-restore`, full `dotnet test` runs with `--configuration Release --no-build` and TRX output, `node --test scripts/tests/dashboard-ui.test.cjs`, the existing `scripts/verify-demo-pages.ps1` with the optional Operator checks, and `git diff --check`. The runtime harness made real-cookie requests to the two forecast bridges, health routes and local assets; it did not substitute test authentication. Test/host orchestration scripts are saved locally as `validate-ui-pass.ps1`, `rerun-ui-sql.ps1`, `finalize-ui-pass.ps1`, `restart-ui.ps1` and `check-ui-runtime.ps1` under the evidence directory.

## Reproduction

Prerequisites: .NET 10, existing restored packages, Node (tested with v24.12.0; no npm packages added), SQL Server 2025 with the native-vector schema, the exact isolated integration database, and existing private local configuration. Follow [the local setup](demo-script.md) and [DEMO18 prerequisites](synthetic-demo-data.md). Do not print credentials or use shared/production databases.

```powershell
dotnet build AI-Emergency-Response.slnx --configuration Release --no-restore
dotnet test tests/EmergencyResponse.UnitTests --configuration Release --no-build
# Configure ConnectionStrings__TestDatabase privately for the isolated test DB first.
dotnet test tests/EmergencyResponse.IntegrationTests --configuration Release --no-build
node --test scripts/tests/dashboard-ui.test.cjs
git diff --check

# Start the normal local API/Web with documented configuration and private credentials.
$reporterPassword = Read-Host 'Synthetic Citizen password' -AsSecureString
$reviewerPassword = Read-Host 'Synthetic SystemAdmin password' -AsSecureString
$operatorPassword = Read-Host 'Synthetic Operator password' -AsSecureString
./scripts/verify-demo-pages.ps1 -WebUrl https://localhost:7201 `
  -ReporterPassword $reporterPassword -ReviewerEmail portfolio-admin@test.local `
  -ReviewerPassword $reviewerPassword -OperatorPassword $operatorPassword `
  -SeedResultPath artifacts/portfolio/local-e2e/seed-restored.json `
  -OutputPath artifacts/portfolio/ui-mvc.json
```

Use the seed-result path produced by your own documented import. No new seeding or cleanup was required for this UI pass. On Windows, stop only the verified development host processes if their loaded DLLs prevent rebuilding. The local verification hosts were restarted after the final build.

## Browser and viewport acceptance

The connected Chrome inventory failed with **`Codex auth token is unavailable`**. No existing repository Playwright/browser-testing setup was found. No browser dependency was installed or global browser configuration changed. No rendering, console/network observation, interactive grid/chart action or screenshot from this UI pass is claimed.

| Viewport | Intended layout | Observed result |
|---|---|---|
| 1440 × 900 | Expanded top navigation, four KPIs, four review groups, balanced insights | BLOCKED — browser unavailable |
| 1024 × 768 | Collapsed navigation, four KPIs, two review groups, stacked insights | BLOCKED — browser unavailable |
| 390 × 844 | Collapsed navigation, two KPIs per row, two review groups, queue scroll region | BLOCKED — browser unavailable |

Precise manual checks, without capturing portfolio screenshots:

1. Open `https://localhost:7201/Account/Login` in Chrome. Sign in with an existing synthetic account and its privately stored password. API health URLs are `https://localhost:7191/health/live` and `/health/ready`; ports are from the repository HTTPS launch profiles.
2. At each viewport above, use keyboard Tab/Shift+Tab: skip link, mobile toggle, visible navigation, bell, logout and dashboard actions. Check that the toggle opens/closes the matching menu, Escape closes notifications, focus is visible and content does not overlap or overflow the page.
3. For Operator/SystemAdmin, open `/Dashboard`. Verify active navigation, four readable KPI cards and the queue-scope caveat. Citizen must receive AccessDenied; Responder and EmergencyAdmin navigation still need browser review with those existing roles.
4. In the queue, search `DEMO18-`, filter Status and Severity, sort, resize, reorder, choose columns and page. Scroll inside the mobile queue to View/Map actions; confirm no horizontal **page** overflow. Follow View and Map, and test `/Dashboard?incidentId=07cca8ac-89d8-721f-5559-3a42230bbfa7`. Selection should restore only when the row is on the current page.
5. Check chart legends/tooltips and empty-to-populated refresh behavior. Use a separate disposable database if you need an empty dataset; do not delete the exercised demo records to make an empty screenshot.
6. Verify initial connection versus snapshot status. Click Refresh overview; disconnect/reconnect through DevTools and ensure old values are labelled stale after failed requests and recovery uses a fresh snapshot.
7. Refresh forecasts. In this local dataset, volume must read Unavailable, resource shortages Not assessed, and the valid hotspot result must be described as report-based. No green success claim or fake generation time should appear for unavailable data.
8. Open notifications, inspect unread accessibility naming and real recipient scope; exercise read actions only on synthetic reminders. Inspect Console and Network for failed assets, invalid JSON, JavaScript exceptions and unexpected authorization responses.

## Remaining issues and boundaries

- All actual browser/layout/keyboard/touch observations remain blocked, including Syncfusion frozen columns, popups, chart resizing and narrow-screen scrolling. CSS intent and HTML checks are not substitutes.
- Live SignalR delivery, reconnect/snapshot recovery and multi-user recipient isolation were not exercised in a browser here.
- The three existing unassociated Map filter heading labels from Phase 2.2 remain a deferred map accessibility issue; this pass does not redesign Map.
- Shared tokens may interact with existing per-page styles. Check the other screens when their dedicated UI passes begin.
- The global notification-summary scope, latest-100 queue cap, terminal-state inclusion and default DEMO18 severity values remain existing data-contract limitations, explicitly disclosed above.
- No genuine available volume forecast, resource-demand forecast, live AI/Copilot, media processing or Azure workflow is verified by this pass.

Recommended commit: `feat: polish operations shell and dashboard design system`.

## Phase 18 Part 2.3.3 — Incident reporting and details UI

Date: 2026-10-16. Branch: `docs/phase18-portfolio-release`; PR #17 remains open and unchanged.

## Implementation status

The existing incident UI changes cover Report Emergency, the paged incident queue, My Reports, and Incident Details. The list and My Reports share the same incident table and pagination partials. The create action reloads incident types when a failed create returns the form. Incident details retain the existing escalation, situation, impact, prediction, status, resource, duplicate, history, and visual-evidence sections and their role-gated actions. Unknown impact counts are shown as `Unknown`; zero remains a distinct value. A Release build exposed a Razor parsing issue in the new pagination partial (`page` conflicted with the Razor directive); the loop variable is now `pageNumber`.

Files changed for this part: `src/EmergencyResponse.Web/Controllers/IncidentsController.cs`, the four incident views under `src/EmergencyResponse.Web/Views/Incidents/`, `src/EmergencyResponse.Web/wwwroot/css/site.css`, and the new `_IncidentTable.cshtml` / `_IncidentPagination.cshtml` partials.

## Build and test results

- `dotnet build AI-Emergency-Response.slnx -c Release`: **passed**, 0 errors, 29 warnings. Warnings include existing NuGet vulnerability advisories for `System.Security.Cryptography.Pkcs` 9.0.18 and `System.Security.Cryptography.Xml` 8.0.4, plus existing nullable-reference warnings.
- `dotnet test tests/EmergencyResponse.UnitTests/EmergencyResponse.UnitTests.csproj --configuration Release --no-restore`: **passed**, 569 passed, 0 failed, 0 skipped.
- SQL integration suite: **blocked at fixture setup**. The run reported 89 failures and 0 passes because `ConnectionStrings:TestDatabase` is unset. `SqlServerReadiness` requires the dedicated `EmergencyResponse_IntegrationTests` database and refuses other database names; no database was changed.
- `git diff --check`: passed with no whitespace errors.

## Browser and responsive verification

The requested visual checks at 1440 × 900, 1024 × 768, and 390 × 844 remain **blocked**. Navigating the available browser to `https://localhost:7201/Incidents` returned `net::ERR_CONNECTION_REFUSED`; the local Web/API hosts were not running. No layout, keyboard, touch, validation rendering, or screenshot inspection is claimed. Screenshot capture remains pending until genuine application screens can be opened and inspected.

## Remaining verification

Configure `ConnectionStrings:TestDatabase` for the isolated `EmergencyResponse_IntegrationTests` SQL database, then rerun the integration suite and the authorized MVC page check with the existing synthetic DEMO18 seed and accounts. Start the Web and API against that isolated environment and inspect all four incident pages at each requested viewport. Verify keyboard focus and validation-message association on the form; search, severity/status filters, clear-filter behavior and pagination on the queue; empty and paged states on My Reports; and operator/citizen visibility, alert actions, and navigation on Details. Check mobile overflow and action wrapping. Capture screenshots only after viewing the actual rendered screens.

Recommended commit after remaining checks: `feat: polish incident reporting and details UI`.

## Phase 18 Part 2.3.5 — Copilot, situations, and notifications

Date: 2026-10-17. Branch: `docs/phase18-portfolio-release`; PR #17. Changes remain local and uncommitted.

### Implementation

- Copilot now uses the shared page heading and panels, keeps the question/context/antiforgery bindings, and presents the answer, grounding state, citations, evidence gaps, and response-specific verification notes separately. Empty citations are explicitly described as unverified. Provider and empty-response failures preserve the submitted question and show an unavailable state.
- Proposal cards show their actual type and summary, expiration, approval requirement, and separate antiforgery-protected approval/rejection forms with labeled comments. Only proposals marked as requiring approval expose those controls; server routes and API revalidation remain authoritative.
- Situation Command Center no longer claims a permanent Live connection. It labels the initial data as a snapshot, offers a manual snapshot refresh with success/failure status, and preserves active server filters when refreshing. If the initial request fails, it shows unavailable rather than zero-valued KPIs. Scores/evaluation time and shortage state show Not evaluated when no evaluation timestamp exists. Review state no longer claims an item was reviewed merely because no review is pending.
- Situation evaluation, acknowledge, review, resolve, and dismiss controls follow the existing role permission map and endpoint role requirements. Linked-report caveat remains visible; aggregate values are labeled as reported totals and a zero casualty count is not presented as proof of no casualties.
- Notification Center now has five clearly labeled summary cards, GET filters for priority/read status/type, visible active filters, a clear action, and a distinct data-unavailable state. Empty filtered results no longer say all notifications are caught up. Summary authorization failures are no longer converted to fake zero counts; a missing summary is calculated from the recipient-scoped loaded set and only unread rows contribute to the priority metrics. Reading remains an explicit user action. Arbitrary external `ActionUrl` values are not used as notification links; the existing incident/situation destination is used when the action URL is not local.
- Added small-screen layout rules using existing design tokens. No new dependencies, fake response data, or live-connection claims were added.

### Validation performed

- `dotnet build AI-Emergency-Response.slnx -c Release --no-restore`: passed, 0 errors, 14 warnings. The warnings are existing package vulnerability advisories (including `System.Security.Cryptography.Pkcs` and `System.Security.Cryptography.Xml`); no new nullable Razor warning remains in the touched situation view.
- `dotnet test tests/EmergencyResponse.UnitTests/EmergencyResponse.UnitTests.csproj --configuration Release --no-restore`: passed, 572 passed, 0 failed, 0 skipped.
- `node --test scripts/tests/dashboard-ui.test.cjs scripts/tests/resource-command-center.test.cjs`: passed, 28 passed, 0 failed.
- `git diff --check`: passed after the final edits.
- `ConnectionStrings__TestDatabase` is unset. SQL integration tests were not run; the test harness requires the isolated `EmergencyResponse_IntegrationTests` database and will not use another database.
- No focused MVC/browser snapshot test was available for these Razor views. Existing situation and notification API integration tests were not run because the isolated SQL test database is unavailable.

### Browser and responsive verification

Visual rendering, keyboard interaction, and the requested 1440 × 900, 1024 × 768, and 390 × 844 viewport checks remain blocked. The available browser automation was denied access to the local application URL by its security policy, and no alternate browser route was used. Build and source inspection do not count as visual verification.

Manual verification once browser access is available:

1. Sign in with existing synthetic Responder, Operator, EmergencyAdmin, and SystemAdmin accounts. Confirm unauthorized users do not see Copilot, situation mutation, evaluation, or resolution actions they cannot execute.
2. Submit a Copilot question with and without hidden incident/situation context. Check validation, preserved question after provider error, grounded/partial/insufficient/unavailable states, citation metadata, absent-citation messaging, and response-specific verification notes. Check proposal types, expiration, approval requirement, comments, separate approval/rejection forms, and confirmation that approval is a server-authorized workflow.
3. At each viewport, inspect situation filters and manual refresh. Verify a successful snapshot preserves active filters, a failed refresh retains prior rows with a stale/unavailable label, default evaluation timestamps show Not evaluated, and the linked-report caveat remains visible. Test the existing modal/state transitions with the appropriate role.
4. Inspect notification priority/status/type filtering and clear behavior, all five metric meanings, long-message wrapping, empty and unavailable states, unread/read controls, and mark-all behavior. Confirm displaying a notification does not change its read state. Check keyboard focus and horizontal page overflow.

Recommended commit: `feat: polish copilot situations and notifications UI`.

## CI follow-up — portfolio demo cleanup ownership

The CI error about `OperationalSituationAnalysis` was verified against the current code. The portfolio seeder owns the situation through its audit manifest; analysis rows are created later by `OperationalSituationService`, and previously had no ownership marker. Cleanup correctly rejected those unowned dependents.

The fix adds nullable `PortfolioDemoOwnershipKey` fields to situations and analyses. The demo situation carries `DEMO18-v1`; generated analyses inherit that key. Cleanup queries analyses only through the manifest-owned situation IDs, refuses any row whose key does not match, and includes only explicitly owned analyses in the deletion set. A regression test confirms unmarked analyses still block deletion; owned analyses are counted in dry-run output and removed before their parent.

The EF migration is `20261016170848_AddPortfolioDemoOwnership`. A focused regression test first failed because the analysis entity had no ownership field; after the fix, all 20 portfolio seeder unit tests passed. The complete unit suite then passed 572 tests. The Release solution build passed with 14 warnings and no errors, and the CI EF pending-model-changes check reported no model changes after the migration. SQL Server integration execution remains unavailable locally because `ConnectionStrings__TestDatabase` is unset; the integration test now explicitly reanalyzes the seeded situation and asserts that the generated record inherits the demo ownership key before testing cleanup.
