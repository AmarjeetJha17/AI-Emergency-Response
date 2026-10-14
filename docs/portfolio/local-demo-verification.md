# Phase 18 Part 2.2 — Local Demo Verification

## Phase closure — 2026-10-15

**Phase status: COMPLETE — engineering checkpoint accepted by the project owner, with remaining verification explicitly deferred.** The owner instructed: "Mark this phase 2.2 complete ... We'll do the rest of the verification later" and authorized commit/push to the existing PR #17 branch.

This closes Phase 18 Part 2.2/2.2B for project progression. It does not change individual test results to PASS or establish full browser, live-provider, staging or pilot verification. Earlier PARTIAL/BLOCKED statements below describe the verification state before this closure decision.

Accepted evidence: Release build (0 errors/15 warnings), 557 unit tests, two latest focused SQL tests, 24 authenticated MVC/snapshot/access checks, EF model alignment and health checks passed. The most recent full SQL suite passed 89 tests in the preceding run. Twelve inspected manual screenshots provide the bounded results and pre-correction defect evidence described below. No new test/coverage run is claimed by this documentation checkpoint.

### Deferred verification backlog

- [ ] Refreshed fire/situation screens confirm the final presentation/Resolve corrections (BR-08-REFRESH / C-RENDER).
- [ ] Remaining roles, dashboard, six-report filtering, complete incident assessment/history, collapse, command centers and reviewer inbox rendering (BR-01/02/03/04-HISTORY/05-HISTORY/07-ACTION/08-COLLAPSE/09).
- [ ] Map selection, filter/layer interactions, full attribution and the three open label-association Issues (BR-10-INTERACT / VISUAL-LABEL-01).
- [ ] SignalR live delivery, recipient isolation, reconnect and authoritative snapshot recovery (BR-12).
- [ ] Browser Console/Network checks beyond the supplied static captures.
- [ ] Genuine duplicate assessment/candidate behavior, live AI, media and configured Search/Copilot evidence; missing prerequisites remain recorded (BR-06-ACTION / BR-11).

Keep this backlog with the subsequent frontend/demo-verification work. Owner acceptance permits moving forward; unresolved functional failures must still be fixed when observed. Operational launch remains at its existing NO-GO status. No Azure resources are deployed as part of phase closure.

## Part 2.2B follow-up — 2026-10-15

**Status: PARTIAL — Operator login, flood/fire details, acknowledged manual escalation, linked situation/count warning, duplicate unavailable state, completed resource detail/history, map rendering and initial SignalR connection have manual evidence. The third screenshot batch exposed misleading situation claims and Operator Resolve controls, corrected and verified through authenticated server checks; corrected browser rendering remains pending. Full browser sign-off is incomplete.** Existing Part 2.2 changes were preserved. Part 2.1 remains committed at `bf63d0e`; the initial 13 uncommitted files belonged to Part 2.2. No commit, push, Azure deployment, local configuration rewrite or demo cleanup occurred in this follow-up. The verification-owned demo SQL container and synthetic review history were retained.

### Browser approach and tooling evidence

Repository inspection found no Playwright, Selenium, Cypress or Puppeteer setup or browser-test package manifest. `scripts/verify-demo-pages.ps1` performs authenticated HTTP/HTML/snapshot checks; it is **not** a browser renderer. No browser-testing dependency or global configuration was installed.

The prior app-server missing-path error occurred before application navigation. This follow-up refreshed the browser tool successfully, then Chrome creation failed with **`Codex auth token is unavailable`**. Inventory detected the connected Chrome extension/profile but returned the same authentication error without any tabs. Both errors concern automation infrastructure; neither demonstrates a Web/API failure. No live tab was controlled by the agent. Twelve manually supplied screenshots below were subsequently inspected, including static Console/Issues captures; a browser Network panel or live console was not inspected.

The owner confirmed successful login and supplied three map screenshots, five flood/duplicate/resource/map-issue captures, then four fire/situation captures on 2026-10-15. This supports the limited results below, not all-role or full-workflow acceptance. Official [browser-extension troubleshooting](https://learn.chatgpt.com/docs/chrome-extension) describes app updates, the active extension profile, Manage/connection checks, a fresh chat and support feedback. It does not establish a verified fix for this specific missing-token error. No app authentication/security setting was changed by the agent.

### Local URLs and manual Chrome prerequisites

URLs come from the checked-in HTTPS launch profiles, not guessed ports:

- Web login: <https://localhost:7201/Account/Login>.
- API liveness: <https://localhost:7191/health/live>.
- API readiness: <https://localhost:7191/health/ready>.
- Optional HTTP profiles are Web `http://localhost:5021` and API `http://localhost:5264`; they are **not running** in this verification session. Keep the tested HTTPS/JWT bridge configuration.

The normal Release Web/API DLL hosts were started with Development settings and the preserved ignored `.env.portfolio.ps1`, after checking the exact local demo database and loopback SQL target. Both API health endpoints and Web login returned 200. Ports 7191/7201 were free before startup. Initial owned process IDs/start time are in ignored `artifacts/portfolio/local-e2e/part22b-hosts.json`; after the third-batch fixes the two verified hosts were rebuilt/restarted and current IDs are in `manual-followup-hosts.json`. Hosts are retained temporarily for the owner's manual walkthrough; verify their current process identities before stopping them. Normal startup behavior and configuration are unchanged.

Use a separate Chrome profile/session per role, or sign out before switching accounts. Existing actors are `DEMO18-citizen@test.local` (Citizen), `DEMO18-operator@test.local` (Operator), and `portfolio-admin@test.local` (SystemAdmin). Use the existing private local passwords. No account was reset or elevated in this follow-up. A failed login should be reported; do not weaken authentication or publish credentials. If hosts have stopped, follow [the existing startup commands](demo-script.md#2-explicit-migrations-and-startup) with the designated private demo configuration loaded. No migration or reseed is required for the retained dataset.

In Chrome, open DevTools (F12), select **Console** and **Network**, then reload each screen. Record actual red errors, failed requests, HTTP status/content type, missing/overlapping controls, empty states and map tile attribution. Inspect failures without capturing cookies, passwords, Authorization headers, tokens, personal browser tabs or unredacted HAR files. Only store screenshots actually captured and inspected. If HTTPS produces a certificate warning, resolve the local certificate prerequisite as the user; do not disable validation to obtain a pass.

### Rendered-screen checklist — manual evidence and remaining observations

All rows require a working local host and the retained synthetic demo database. Expected results are acceptance expectations; only observations explicitly tied to the supplied evidence are marked passed.

| Test ID / scenario | Role | Action / Web route | Expected result | Actual browser result | Status |
|---|---|---|---|---|---|
| BR-01 Login/navigation | Citizen, Operator, SystemAdmin separately | `/Account/Login`; inspect navigation and role-restricted routes | Real login works; Citizen gets own reports, operational routes denied; Operator can review/assign but not dispatch/resolve/approve; admin can use authorized commands | Operator login observed separately; Citizen/admin browser checks and restricted action attempts pending | NOT RUN — remaining roles/actions |
| BR-01-OP Operator login/navigation rendering | Operator | Owner signs in, then opens incident-specific `/Map` | Authenticated Operator session and navigation render | Owner confirms login; screenshot header shows DEMO18 Operator and authenticated navigation/Logout | PASS — manual |
| BR-02 Dashboard | Operator/admin | `/Dashboard` | Charts/counts render and agree with the current unfiltered authorized snapshot; do not equate total database incidents with the six DEMO18 reports | No chart rendering observed | BLOCKED |
| BR-03 List/filter | Citizen/Operator/admin | `/Incidents?search=DEMO18-&pageSize=50`; Citizen `/Incidents/MyReports?pageSize=50` | Exactly six matching reports; filtering and links work; own-report scope respected | No rendered grid/filter observed | BLOCKED |
| BR-04 Flood persisted details | Operator | `/Incidents/Details/07cca8ac-89d8-721f-5559-3a42230bbfa7` | Stored synthetic description/type/state render without inventing assessment results | `005616` shows DEMO18-FLOOD-01, Flood, UnderReview, two reported occupants and unverified rescue/injuries; prediction explicitly unavailable | PASS — manual visible detail subset |
| BR-04-HISTORY Flood assessment/history and own-report scope | Operator, then Citizen comparison | Scroll the same detail page to assessment/history; compare own-report access | Actual analysis state and retained status history render; Citizen scope respected | Lower assessment/history panels and Citizen comparison not supplied; no media upload performed | NOT RUN |
| BR-05 Fire uncertainty/manual escalation | Operator | `/Incidents/Details/ffb1ba8a-cce0-ab58-d2d8-b184d4885520` | Unknown occupants/casualties and manual escalation/acknowledgement render | `010901` shows explicit unknown description, manual reason, Elevated/Acknowledged and timestamp; not an AI/rule-engine conclusion | PASS — manual visible subset |
| BR-05-HISTORY Fire analysis/history/recommendations | Operator | Scroll fire detail to complete assessment/history; inspect available recommendations | Actual history and assessment state, no invented completion | `010914` shows location and recommendation button; Status History heading only, no rows or recommendation result | NOT RUN — remaining panels/actions |
| BR-06 Duplicate unavailable rendering | Operator | `/Incidents/Duplicates/4e46d5a5-fd74-3651-51df-6762f1b22c45` | Explicit unavailable assessment, no invented score/automatic merge; API 409 is expected for these unembedded reports | `005621` shows NotStarted, no duplicate conclusion, manual review warning and reports not merged | PASS — manual unavailable state |
| BR-06-ACTION Computed candidate/action behavior | Operator/admin | Review an actually assessed report if one becomes available; repeat unavailable view on flood | Genuine candidate evidence, proper permissions and no automatic merge | No embedding/scored match exists in DEMO18 data; candidate/action behavior not demonstrated | BLOCKED — assessment prerequisite |
| BR-07 Resource completed assignment/history | Operator | `/Resources/Details/7984672a-a219-d3bc-d4ee-b55713961497` | DEMO18 police resource Available; completed assignment not active; retained lifecycle history | `005629`/`005731` show Available, Unassigned, coordinates, DEMO18-ROAD-01 history and Returning → Available; visible Console has initial connection messages and No issues | PASS — manual detail/history subset |
| BR-07-ACTION Resource command center/permissions | Operator/admin | `/Resources`; inspect inventory and permitted commands without repeating completed lifecycle | Inventory/status agree; Operator dispatch denied; commands follow real role/state rules | Command center and manual action/denial evidence not supplied; screenshots show retained earlier simulated transitions only | NOT RUN |
| BR-08 Situation linked-data/uncertainty | Operator | `/Situations/Details/ada4cb0e-f746-7998-2a6d-a0a021002750` | Acknowledged manual group, three reports, unknown/overlap caveat | `010920`/`010927` show Acknowledged, authored summary, three DEMO18 flood/duplicate links, two people each and casualties unknown | PASS — manual linked-data subset |
| BR-08-BEFORE Situation assessment/provenance and Resolve controls | Operator | Same situation and fire detail before correction | No unsupported job/grouping claims or forbidden Resolve control | Screens show scheduled/in-progress AI, automatic grouping, unevaluated Adequate/0 scores and Operator Resolve buttons; API resolve was already denied | FAIL — captured before correction |
| BR-08-REFRESH Corrected situation/fire rendering | Operator | Refresh same two pages after host restart | AI unavailable, truthful grouping, Unknown casualties/Not evaluated, no Resolve; Dismiss retained | Corrected real-cookie HTML/route checks passed; refreshed screenshot not supplied | NOT RUN — corrected browser evidence |
| BR-08-COLLAPSE Collapse/situation command center | Operator/admin | `/Incidents/Details/75ab3889-3a39-ac5a-399f-411b11b25cf7`; `/Situations` | Unknown collapse facts/manual escalation; command center renders | No rendered collapse or command-center view supplied | NOT RUN |
| BR-09 Notifications | SystemAdmin, Operator comparison | `/Notifications` | Three owned DEMO18 reminders visible to reviewer; wrong actor cannot access/read them; current read state retained | No rendered inbox observed | BLOCKED |
| BR-10 Map rendering | Operator | `/Map?incidentId=07cca8ac-89d8-721f-5559-3a42230bbfa7` | Map tiles, counters, markers and controls render | Three supplied screenshots show real tiles, six incidents/six resources, markers/clusters, legend and filter/layer controls | PASS — manual rendering |
| BR-10-INTERACT Map filters/layers/selected report | Operator/admin | Click a report pin/cluster; change a filter/layer; inspect selected report and attribution | Actual selected DEMO18 report, toggles and filter results are correct | No popup/drawer, changed filter or layer-toggle result supplied; attribution at screenshot edge is clipped | NOT RUN |
| BR-11 Copilot | Operator/admin | `/Copilot` | Page renders; absence of local Search is clear; no claim of a grounded/cited answer without configured retrieval/provider evidence | Rendering pending; Search endpoint absent; no model request made | BLOCKED |
| BR-12 SignalR recovery | Operator/admin; Citizen scope comparison | With Console/Network open, briefly toggle Offline then restore and refresh authoritative snapshots | Reconnect restores current authorized state; no unrelated citizen incident event payloads | No actual browser disconnect/message delivery captured | NOT RUN |
| BR-12-CONNECT Initial SignalR connection | Operator | Inspect Console after map load | Browser establishes real hub connection | Screenshot shows WebSocket connected and `[SignalR] Incident realtime connected.` | PASS — initial connection only |

#### Inspected manual screenshot evidence — 2026-10-15

The owner stated "yes login done" and supplied captures named `Screenshot 2026-10-15 002625.png`, `002632.png` and `002701.png`. All three are the **same incident-specific map route**, not incident-list or flood-detail screenshots. Source files remain unchanged outside the repository. Reviewed copies:

- [Map controls](screenshots/live-map-controls.png): cropped browser chrome/navbar; shows rendered severity/priority/status/type/department/review controls and six-incident/six-resource counts.
- [Map and markers](screenshots/live-map.png): cropped browser toolbar; real map tiles, an incident cluster, resource/situation-style icons, incident marker and legend. Layer controls are displayed, but toggling them was not demonstrated.
- [Console connection evidence](screenshots/live-map-console-redacted.png): cropped browser toolbar and visibly redacted the entire WebSocket URL row/continuation because it contained an access-token query value. The real connection messages remain visible; no application output was added or fabricated.

The first Console capture shows two informational connection messages and no red error in the visible area. Its three unexpanded Issues were identified in the later `005725` capture as **missing form-label associations**, described below. Failed requests remain unverified; an empty visible error area does not establish an error-free session. The Network tab was not supplied. Initial connection does not prove message delivery, recipient isolation, disconnect recovery or authoritative refresh.

The counters show Critical/High/Medium zero and Low six. This agrees with the stored synthetic **intake severity defaults**, not a successful AI assessment or proof the scenarios are low risk. No flood name/detail popup is visible. Source inspection confirms the query parameter centers the map; it does not automatically open the incident drawer. Click a report marker/cluster and confirm the DEMO18 identifier before claiming selected-report verification. No obvious overlapping map controls are visible in these static captures; broader layout/responsiveness remains untested. The simulated DevTools dimensions are a display setting, not a separately verified browser/device.

Escalations are displayed through incident details. The fire/collapse escalations are already Acknowledged: absence from an Open-only endpoint is not proof that an escalation was lost. The flood situation is acknowledged, with a manually authored summary; its aggregate counts may include overlapping reports. The map controller accepts `incidentId`/`resourceId`; selection of a situation through a `situationId` URL is not established by the controller and remains a UI verification question.

#### Second manual evidence batch — flood, duplicate, resource and map Issues

The owner supplied these five additional actual Chrome captures and chose to continue manually. The eight reviewed copies use the same local Development Release hosts, authenticated DEMO18 Operator and retained isolated dataset; source is `bf63d0e` plus uncommitted Part 2.2/2.2B fixes. Original files remain unchanged.

- [Flood details](screenshots/incident-details.png), source `Screenshot 2026-10-15 005616.png`: saved description and UnderReview state render. The reported two occupants are synthetic witness information; rescue/injuries are unverified. Low/Normal and both 0/100 scores are intake defaults. The visible predictive panel says no prediction is available; its advisory warning prohibits automatic operational changes. The lower AI assessment/history panels are outside the capture. The upload control's text is not evidence of successful upload or analysis.
- [Duplicate unavailable state](screenshots/duplicate-assessment-unavailable.png), source `005621`: browser toolbar cropped. The warning explicitly reports NotStarted, no duplicate conclusion and no merge. There is no scored candidate or action execution evidence.
- [Completed resource detail/history](screenshots/resource-details.png), source `005629`: browser toolbar cropped and local synthetic actor GUID values visibly redacted. Available/Unassigned, police specification, coordinates and retained road-incident history are real displayed state. The top visible rows include Returning → Available, OnScene → Returning and EnRoute → OnScene with synthetic lifecycle remarks. No new resource command was performed in this manual batch.
- [Map Console and Issues](screenshots/map-issues-redacted.png), source `005725`: browser toolbar cropped; complete token-bearing WebSocket URL/continuation visibly redacted. The actual Issues panel identifies three resources with **No label associated with a form field**. The resource list itself is collapsed. Source inspection finds three review heading labels without `for`, adjacent to `mapAIReviewFilter`, `mapDuplicateFilter` and `mapVisualFilter`. This is an accessibility finding, not evidence of failed map requests or AI failure.
- [Resource Console](screenshots/resource-console-redacted.png), source `005731`: cropped to the Console pane, omitting browser chrome and the actor-ID table; complete WebSocket URL/continuation visibly redacted. WebSocket/SignalR initial connection messages and **No issues** are visible. No red error appears in the visible area; Network responses, message delivery and reconnect recovery were not captured.

No screenshot content was synthesized. Static cropping/redaction used the already available Pillow installation; no browser automation package was added. No application code changed during this evidence-only batch, and the previously reported 547-unit/89-integration results below were not rerun or relabelled as new results. `git diff --check` was rerun after documentation changes.

**Remaining visual/accessibility finding — VISUAL-LABEL-01:** the three map heading labels need association with their existing checkbox IDs (or appropriate non-label grouping markup). No inability to operate the map is demonstrated. This minor accessibility correction is deferred to frontend polishing; it is not marked fixed or verified clear. Raw SignalR Console URLs include application access tokens: sanitize them before publishing screenshots or logs. Repository evidence copies were inspected after redaction.

#### Third manual evidence batch — fire and linked situation, before correction

Source captures `Screenshot 2026-10-15 010901.png`, `010914.png`, `010920.png` and `010927.png` were inspected on the same local Operator session and preserved. These screenshots precede the new presentation/resolve corrections; they must not be presented as proof those corrections rendered successfully.

- [Fire detail before correction](screenshots/fire-details-before-correction.png): manual escalation reason explicitly says occupancy/casualties unknown and not an AI/rule-engine conclusion. Elevated/Acknowledged, creation and acknowledgement times are visible. Synthetic actor GUID visibly redacted in the repository copy. The forbidden Operator Resolve affordance is preserved as defect evidence; Dismiss is permitted by current rules.
- [Fire location](screenshots/fire-location.png): synthetic FIRE-01 address and coordinates 18.535000, 73.870000, status form and View Recommended Units button are visible. No button activation/result or status update is demonstrated. Status History heading appears, but its rows are outside the capture. The large blank left column and implementation-phase badge are visual polish findings, not a proved functional failure.
- [Situation detail before correction](screenshots/situation-details-before-correction.png): Acknowledged/Advisory, manual-group summary, three members, people sum six, last evaluation/AI snapshot Never, and count warning. The summary correctly states that no AI/automatic clustering ran, but other cards showed unevaluated 0/Adequate and a forbidden Resolve control. The discrepancy prompted DEF-SITUATION-TEXT-03/DEF-RESOLVE-UI-03.
- [Situation links before correction](screenshots/situation-links-before-correction.png): three named reports, primary/member labels, coordinates and casualties unknown render. The AI scheduled/in-progress and automatic-grouping captions contradicted the manual summary and lack of analysis/job evidence; these were corrected in source and authenticated HTML checks, not retouched in this screenshot.

All four copies were inspected after preparation. No content was synthesized; source originals remain unchanged. No Console/Network panels were supplied in this batch. Navbar badge five is a displayed inbox count, not proof of the three demo reminders or their recipient scope.

### Third-batch correction validation — actual results

| Test ID | Preconditions / action | Expected / actual observed result | Status | Evidence |
|---|---|---|---|---|
| C-BUILD | Verified owned demo PIDs/DLLs/listening ports; stop them and rebuild Release, no restore | Build succeeds: **0 errors / 15 warnings** | PASS | `artifacts/portfolio/local-e2e/manual-followup-build.log` |
| C-UNIT | Complete unit suite including ten new resolve role cases | **557 passed**, zero failed/skipped | PASS | `TestResults/LocalE2E/ManualFollowup/Unit/unit-tests.trx` |
| C-SQL | Existing focused SQL dispatch-policy tests on designated IntegrationTests database | **2 passed**, zero failed/skipped; no reset | PASS | `TestResults/LocalE2E/ManualFollowup/Integration/integration-tests.trx` |
| C-MVC | Restart normal hosts; existing verifier plus Operator optional checks using real cookies/CSRF | **24 passed**: corrected unavailable/provenance/Unknown/Not evaluated text; admin situation Resolve present; Operator situation/fire Resolve absent, Dismiss retained; two direct Web resolve requests follow AccessDenied | PASS — server | `manual-followup-mvc.json`, `manual-followup-mvc.log` |
| C-STATE | Real Operator JWT; read retained situation after denied requests | Still Acknowledged, three links, three unknown casualty counts, no LatestAnalysis or evaluation timestamp | PASS — server | `manual-followup-health-state.json` |
| C-HEALTH | Rebuilt API live/ready and Web login | All return 200 | PASS — server | `manual-followup-health-state.json`, current `manual-followup-hosts.json` |
| C-EF | Pending-model verification, no build | No model changes since last migration; existing package advisory warning remains | PASS | `manual-followup-ef.log` |
| C-RENDER | Reload fire/situation in the owner's Chrome after these changes | Corrected rendering must be captured; no post-correction screenshot yet | NOT RUN | Await owner refresh; do not use before-correction captures as proof |

The preceding full **89 SQL** run and **547 unit** run remain earlier evidence in table B below; the new complete unit total is 557. Only the two focused SQL tests were rerun in this batch. There was no new restore, coverage, full SQL run or GitHub workflow claim. The first host-stop attempt refused relative DLL paths before stopping anything; exact relative DLLs plus owned ports/PIDs were subsequently checked. Build/tests and normal host restart then succeeded. Demo configuration/data/history were preserved; no demo import, cleanup, migration application, Azure deployment, commit or push occurred. No live provider reprocessing/Copilot request was deliberately triggered. Operator detail reads retain the existing on-demand prediction path; no prediction quality or provider success is claimed.

Commands executed (private SecureString/environment values omitted):

```powershell
dotnet build AI-Emergency-Response.slnx --configuration Release --no-restore
dotnet test tests/EmergencyResponse.UnitTests --configuration Release --no-build --logger 'trx;LogFileName=unit-tests.trx' --results-directory TestResults/LocalE2E/ManualFollowup/Unit
dotnet test tests/EmergencyResponse.IntegrationTests --configuration Release --no-build --filter 'FullyQualifiedName~ResourceStatusAuthorizationTests' --logger 'trx;LogFileName=integration-tests.trx' --results-directory TestResults/LocalE2E/ManualFollowup/Integration
./scripts/verify-demo-pages.ps1 -ReporterPassword $demoPassword -ReviewerEmail portfolio-admin@test.local -ReviewerPassword $demoPassword -OperatorPassword $demoPassword -SeedResultPath artifacts/portfolio/local-e2e/seed-restored.json -OutputPath artifacts/portfolio/local-e2e/manual-followup-mvc.json
dotnet ef migrations has-pending-model-changes --project src/EmergencyResponse.Infrastructure --startup-project src/EmergencyResponse.Api --context ApplicationDbContext --configuration Release --no-build
git diff --check
```

Additional third-batch changes: Web `Controllers/EscalationsController.cs`, `Controllers/SituationsController.cs`, `Views/Incidents/Details.cshtml`, existing `Views/Situations/Details.cshtml`; new `tests/EmergencyResponse.UnitTests/Web/DemoResolveAuthorizationTests.cs`; extended existing MVC verifier; portfolio docs and four actual screenshot copies. No normal startup, domain model, database schema, package or deployment infrastructure changed.

### Next manual evidence, read-only

Stay signed in as Operator and refresh the fire and linked flood situation after the correction:

1. <https://localhost:7201/Incidents/Details/ffb1ba8a-cce0-ab58-d2d8-b184d4885520> — confirm Resolve is absent and the manual reason/acknowledgement is unchanged. Dismiss is permitted but do not click it merely for evidence. Scroll to complete history/assessment without reprocessing or uploading.
2. <https://localhost:7201/Situations/Details/ada4cb0e-f746-7998-2a6d-a0a021002750> — confirm Resolve is absent, AI unavailable text, Unknown casualty card, Not evaluated score/resource status and truthful linking caption. The three links/count warning should remain.
3. <https://localhost:7201/Incidents?search=DEMO18-&pageSize=50> — confirm the six matching reports and open flood details; capture the visible grid/filter result.

Then inspect `/Dashboard` and `/Resources`; notifications require the designated reviewer comparison described in BR-09. For each, record PASS/FAIL and any sanitized Console/Network error. Do not repeat dispatch/acknowledgement writes or force cleanup merely to recreate screenshots. Keep passwords, cookies, token URLs and Authorization headers out of captures.

### Repeatable flood walkthrough (read-only first)

1. Sign in as Operator and open the six-report DEMO18 filtered list. Record the current displayed count.
2. Open the flood ID above. Read the persisted synthetic title/description/location, UnderReview state and operator remarks/history. Do not imply that Low/Normal intake values were AI-evaluated.
3. Inspect the actual assessment panel. These fixtures were not given embeddings or AI/visual assessments; an unavailable/pending indication is expected. Do not click reprocess to manufacture portfolio evidence.
4. Open the incident-specific map URL. Confirm the selected location, real tiles and available layers. Record console/network errors if selection or layers fail.
5. Inspect the linked manual flood situation and its three reports. Read the warning that sums can overlap and zero totals do not establish no casualties. The available coordination/history is a synthetic exercise, not emergency dispatch.
6. Inspect the DEMO18 police resource detail separately: final Available state, no active incident, completed historical timings/history. Do not repeat status commands on an already completed lifecycle to force a pass.
7. Open duplicate review separately: expected unavailable assessment, not a computed match or zero-match conclusion. Check the fire/collapse manual escalations through their detail pages.
8. Capture the final visible state/history and sanitized errors, recording role, route, timestamp and PASS/FAIL. No write, cleanup or reseed is needed merely to inspect the retained results. Further simulated writes must follow actual role/state rules and be recorded explicitly.

### Targeted defects and follow-up engineering evidence

- **DEF-REALTIME-02:** `IncidentUpdated` previously used `Clients.All`, exposing incident identifiers/status/assessment fields to unrelated authenticated citizens. It now selects the persisted reporter and sends to the existing dashboard-operator group plus that reporter's user group. No new join API or role bypass was added. Three Moq/SQLite cases verify exact recipients, absent/empty owner handling, payload/token forwarding, one group send and no All broadcast. Actual browser delivery/reconnect remains unverified. Responder-specific assignment subscriptions are not introduced by this narrow fix.
- **DEF-COUNTS-02:** incident context and linked-situation incident DTOs now preserve nullable reported counts. Six normal API context checks and SQL fixture assertions validate null casualties and matching reported counts. Situation totals retain their existing summed semantics; both situation views now explain unknown exclusion and overlap, and linked rows show `casualties unknown`. This fixes the immediate zero-as-known presentation in those paths; it does not certify every downstream scoring, tool, dashboard or forecasting count consumer.
- **DEF-RESOLVE-UI-03:** third-batch screenshots confirmed Resolve buttons shown to the Operator although existing API permission policies deny resolution. Both views now consult the existing role-permission map; Web Resolve actions allow only EmergencyAdmin/SystemAdmin. API policies and antiforgery remain intact. Ten unit role cases and real Operator cookie/CSRF denial checks passed. Dismiss is permitted by the actual role-based API/Web rules and remains available; the earlier shorthand treating both actions as forbidden was incorrect. No dismissal was executed.
- **DEF-SITUATION-TEXT-03:** null analysis no longer implies queued/running work; linked reports no longer imply automatic clustering. An unevaluated situation displays Not evaluated for score/resource evaluation. When every linked casualty count is unknown the card displays Unknown, while the persisted aggregate remains unchanged. Normal authenticated response checks verify the corrected text. This is an honest presentation fix, not job execution, model evaluation, or automatic grouping evidence.

| Test ID | Preconditions / action | Actual result | Status | Evidence |
|---|---|---|---|---|
| B-GIT | Preserve current branch/work; inspect diff | Original 13 Part 2.2 files retained; Part 2.1 committed; no commit/push | PASS | Git status/stat/check |
| B-BUILD | Release solution build, no restore | 0 errors / 18 warnings | PASS | `artifacts/portfolio/local-e2e/part22b-build.log` |
| B-UNIT | Complete current unit suite | **547 passed**, 0 failed/skipped | PASS | `TestResults/LocalE2E/Part22B/Unit/unit-tests.trx` |
| B-SQL-FIRST | Focused SQL regressions on retained test database | 2 passed / 1 failed; ownership cleanup refused foreign OperationalSituationIncident dependency | FAIL | `TestResults/LocalE2E/Part22B/Integration/integration-tests.trx`, `part22b-integration.log` |
| B-SQL-FRESH | Fresh designated isolated integration database; complete suite | **89 passed**, 0 failed/skipped | PASS | `TestResults/LocalE2E/Part22B/IntegrationFresh/integration-tests.trx`, `part22b-integration-fresh.log` |
| B-HEALTH | Normal guarded demo hosts | API live 200, ready 200; Web login 200 | PASS — server | `part22b-health.json` |
| B-MVC | Existing real-cookie verifier with new count-warning markers | **19 passed**; including snapshot/access checks; no browser rendering claim | PASS — server | `part22b-mvc-pages.json`, `part22b-mvc.log` |
| B-API | Normal Operator JWT; six context/duplicate checks, situation and completed resource | **8 passed**; unknown counts preserved, duplicates 409, three links, Available/null active incident | PASS — server | `part22b-api-checks.json` |
| B-EF | Pending-model check against current code | No changes since last migration | PASS | `part22b-ef-model.log` |
| B-DIFF | `git diff --check` after follow-up and manual-evidence changes | No whitespace errors; existing changes preserved, reviewed screenshots/docs added | PASS | Git check |
| B-DATA | Read-only retained demo snapshot | Six reports, six resources; zero AI analyses and zero duplicate matches | PASS | `part22b-data.json` |
| B-LOG | Inspect owned normal host logs without publishing secrets | Both stderr files empty; API logged nine UnauthorizedException categories, Web had no failure entries; exact browser requests not captured | PASS — log inspection only | `part22b-runtime-summary.json`, local host logs |

Detailed local evidence files are ignored; reviewed, sanitized screenshot copies above are included in the proposed repository changes. Do not publish credentials/raw sensitive logs. Full Part 2.2 results below remain dated historical evidence, not a replacement for the remaining browser checks. No new coverage or GitHub Actions result is claimed. Full browser sign-off and Part 2.2 exit criteria remain unmet despite the verified Operator/flood/duplicate-unavailable/resource/map/initial-connection subset.

The failed retained-database run was preserved. Only the verification-owned **EmergencyResponse_IntegrationTests** database on the known loopback SQL container was recreated, after the test process finished and the exact target was checked. The full 89-case rerun passed. No cleanup guard was weakened, and the live demo/LOADTEST databases were not reset. This remains a repeat-run integration-fixture isolation limitation.

The API unauthorized exception category originates from existing login validation (invalid credentials or inactive account); the global handler maps it to 401. This is actual server-log evidence, not an observed browser console error. The owner reported difficulty finding login credentials and was directed privately to the preserved local configuration. No password was displayed in tool output, changed or committed. The normal password-authenticated API/MVC checks above succeeded. Correlation of the owner's manual login attempts and visual results remains pending; do not label every logged denial as a successful browser check.

Commands actually executed for this follow-up (private environment was loaded separately; secret values omitted):

```powershell
git status --short
git diff --stat
git diff --check
docker port er-portfolio-sql2025
dotnet build AI-Emergency-Response.slnx --configuration Release --no-restore
dotnet test tests/EmergencyResponse.UnitTests --configuration Release --no-build --logger 'trx;LogFileName=unit-tests.trx' --results-directory TestResults/LocalE2E/Part22B/Unit
# First, retained target: three filtered cases; one safe cleanup failure was retained.
dotnet test tests/EmergencyResponse.IntegrationTests --configuration Release --no-build --filter 'FullyQualifiedName~PortfolioDemoSeederIntegrationTests|FullyQualifiedName~ResourceStatusAuthorizationTests' --logger 'trx;LogFileName=integration-tests.trx' --results-directory TestResults/LocalE2E/Part22B/Integration
# Complete rerun after guarded reset of the designated disposable integration database:
dotnet test tests/EmergencyResponse.IntegrationTests --configuration Release --no-build --logger 'trx;LogFileName=integration-tests.trx' --results-directory TestResults/LocalE2E/Part22B/IntegrationFresh
dotnet ef migrations has-pending-model-changes --project src/EmergencyResponse.Infrastructure --startup-project src/EmergencyResponse.Api --context ApplicationDbContext --configuration Release --no-build
# SecureString values supplied from preserved private local configuration:
./scripts/verify-demo-pages.ps1 -ReporterPassword $demoPassword -ReviewerEmail portfolio-admin@test.local -ReviewerPassword $demoPassword -SeedResultPath artifacts/portfolio/local-e2e/seed-restored.json -OutputPath artifacts/portfolio/local-e2e/part22b-mvc-pages.json
```

The eight targeted normal API reads and local read-only snapshot used ignored diagnostics, not a new seeding system. No import/reimport/cleanup was rerun on the demo in Part 2.2B; the completed Part 2.2 lifecycle evidence remains below.

Additional changed paths in Part 2.2B: `scripts/verify-demo-pages.ps1`, API `Services/IncidentNotificationService.cs`, Application `Intelligence/IncidentContextSnapshot.cs` and `Situations/DTOs/OperationalSituationResponse.cs`, Infrastructure `Situations/OperationalSituationService.cs`, Web `Views/Situations/Details.cshtml` and `Index.cshtml`, and `tests/EmergencyResponse.UnitTests/Realtime/IncidentNotificationServiceTests.cs`. Existing API incident, SQL portfolio test and portfolio documentation changes were extended; all original fixes were preserved. No model, migration, package, startup or deployment file changed.

Verified **2026-10-14**, on `docs/phase18-portfolio-release`, based on `bf63d0ec1a9ff18fff39a2ac5303ff45afec780a` plus the functional fixes accompanying this report. [PR #17](https://github.com/AmarjeetJha17/AI-Emergency-Response/pull/17) remains the existing review target. These changes have not been committed or pushed by this verification session.

**Status: PARTIAL / BLOCKED.** Local authenticated server workflows pass after three fixes. Chrome rendering, screenshots and interactive SignalR recovery could not be verified, so this phase is not marked complete. No Azure resources were deployed. This is a synthetic engineering exercise, not operational or pilot approval.

## Environment and baseline

- The starting working tree was clean; Phase 2.1 was committed as `bf63d0e`. README, demo documentation, existing verifier, models, seeders, role/permission rules and integration fixtures were inspected before edits.
- Normal Development Web/API hosts were used, with real password authentication, cookies, antiforgery protection and the existing JWT bridge. No integration-test headers, fake AI registrations or authorization bypasses were used in these hosts.
- HTTPS endpoints: Web `https://localhost:7201`; API `https://localhost:7191`. The trusted local development certificate was used without disabling certificate validation.
- SQL Server 2025: the verification-owned disposable Docker container, with its existing loopback port mapping. Demo writes were restricted to **`EmergencyResponse_PortfolioDemo`**. Full integration tests used **`EmergencyResponse_IntegrationTests`** in that same disposable instance. Only that integration database was reset between full suites; the demo, LOADTEST and other SQL containers were not dropped.
- Existing ignored local environment configuration and API/Web User Secrets were retained. No connection string, password, token or secret value is included in this report. Follow [local setup](demo-script.md) and [demo prerequisites](synthetic-demo-data.md).
- Actors: `DEMO18-citizen@test.local` (Citizen), `DEMO18-operator@test.local` (Operator), and `portfolio-admin@test.local` (SystemAdmin). The Operator was missing initially and was created by explicitly invoking the **existing `TestUsersSeeder` and real `UserManager`** from an ignored diagnostic harness, guarded to the isolated local demo database. Existing accounts were not elevated or reset. No identity bootstrap was added to the demo importer or normal startup. A fresh clone still needs a separately provisioned authorized Operator account.
- Baseline: Release build passed with 0 errors / 18 warnings; **536 unit and 87 SQL integration tests passed**. EF reported no pending model changes.

## Final engineering checks

| Test ID | Preconditions / role | Action | Expected | Observed | Status | Evidence |
|---|---|---|---|---|---|---|
| BASE-GIT | Repository, before edits | Inspect status and latest commit | Committed Phase 2.1, no unrelated changes | Clean tree at `bf63d0e` | PASS | Git inspection |
| BUILD-FINAL | .NET 10, repository tooling | Release restore and build | Successful restore; zero build errors | 0 errors, 17 warnings | PASS | `artifacts/portfolio/local-e2e/final-restore.log`, `final-build.log` |
| UNIT-FINAL | Final compiled code | Run complete unit suite | All cases pass | **544 passed**, 0 failed/skipped | PASS | `TestResults/LocalE2E/Final/Unit/unit-tests.trx` |
| SQL-FINAL | Fresh designated SQL 2025 integration database | Run complete integration suite | All cases pass | **89 passed**, 0 failed/skipped | PASS | `TestResults/LocalE2E/Final/Integration/integration-tests.trx` |
| EF-FINAL | Final model and migrations | Check pending model changes | None | No changes since last migration | PASS | `artifacts/portfolio/local-e2e/final-ef-model.log` |
| DIFF-FINAL | Task changes only | `git diff --check` | No whitespace errors | Passed; Git also emits line-ending normalization notices | PASS | Git command output |

Coverage was not recollected in Part 2.2. The earlier 79.59% result belongs to Part 2.1; it is not a new measurement for these fixes. Existing vulnerability/nullable warnings remain unresolved.

## Changed files

- `src/EmergencyResponse.Api/Controllers/IncidentsController.cs` — explicit no-embedding duplicate response.
- `src/EmergencyResponse.Api/Controllers/ResourcesController.cs` — dispatch policy on generic status changes.
- `src/EmergencyResponse.Infrastructure/Resources/ResourceService.cs` — distinguish active assignment fields from historical timing.
- `src/EmergencyResponse.Web/Controllers/IncidentsController.cs` — duplicate-page assessment-state handling.
- `src/EmergencyResponse.Web/Views/Incidents/Duplicates.cshtml` — clear unavailable assessment notice.
- `tests/EmergencyResponse.UnitTests/Web/DuplicatePageTests.cs` — eight new state/error cases.
- `tests/EmergencyResponse.UnitTests/Resources/ResourceServiceTests.cs` — completed-assignment projection assertions.
- `tests/EmergencyResponse.IntegrationTests/Resources/ResourceStatusAuthorizationTests.cs` — two new real-JWT role cases.
- `tests/EmergencyResponse.IntegrationTests/Portfolio/PortfolioDemoSeederIntegrationTests.cs` — six no-embedding/no-match-write checks.
- `docs/portfolio/local-demo-verification.md` — this report and per-check register.
- `docs/portfolio/demo-script.md` — current walkthrough results and prerequisites.
- `docs/portfolio/synthetic-demo-data.md` — follow-up lifecycle/state evidence.
- `docs/portfolio/feature-verification.md` — observed feature status and remaining limits.

No normal startup, fixture definitions, database model/migration, package, frontend framework, Docker, CI or Azure infrastructure file was changed. Ignored diagnostics and private configuration are not part of the proposed commit.

## Demo lifecycle and isolation

All destructive fixture operations below occurred **before** operational mutations, while the normal demo hosts were stopped. The importer and cleanup use the existing `DatabaseSeeder` ownership manifest; no competing seeder or public seeding API was introduced.

| Test ID | Preconditions / role | Action | Expected | Observed | Status | Evidence |
|---|---|---|---|---|---|---|
| DATA-01 | Isolated migrated demo database; existing Citizen/admin | Import | Six reports/resources, two manual escalations, one situation, three reviewer reminders | Expected owned records present; 6 locations, 6 initial histories, 6 notes, 3 situation links | PASS | `seed-first.json`, `data-before.json` under local evidence directory |
| DATA-02 | Existing ownership manifest, same actors | Reimport | Same IDs; no duplicate fixtures | `Created=false`, same six IDs | PASS | `seed-repeat.json` |
| DATA-03 | Fixture state, before operations | Inspect relationships/identifiers | Valid existing model relationships; DEMO18 markers | Native SQL reads and authorized APIs succeeded; all incident casualty fields null | PASS | `data-before.json`, SQL demo integration case |
| DATA-04 | Hosts stopped; no foreign dependents | Cleanup preview | Exact owned IDs/counts only | 39 fixtures + 1 ownership marker; preview applied nothing | PASS | `cleanup-preview.json` |
| DATA-05 | Reviewed preview, same isolated target | Apply cleanup | Dependents removed safely; shared rows retained | Owned incidents/resources removed; accounts/types/departments and unrelated forecast notification retained | PASS | `cleanup-apply.json`, `data-cleaned.json` |
| DATA-06 | Owned fixtures absent | Repeat cleanup / reseed | Cleanup no-op; restore stable IDs | No-op cleanup; reseed `Created=true`, same IDs | PASS | `cleanup-repeat.json`, `seed-restored.json`, `data-restored.json` |
| DATA-07 | Before/after cleanup and reseed | Compare shared-data fingerprint | Shared accounts/reference data/non-demo incident/resource projections unchanged | SHA-256 fingerprint unchanged; unrelated notification retained | PASS | `data-before.json`, `data-cleaned.json`, `data-restored.json` |
| DATA-08 | Operator work and service-created records now exist | Preview cleanup | Refuse deletion of application-owned dependents | Exit 1: `IncidentEscalation contains dependent records not owned by DEMO18`; no apply attempted | PASS | `post-workflow-cleanup-preview.log` |
| DATA-09 | Existing edited fixtures, same actors | Reimport after operations | Preserve review/read state and IDs | `Created=false`; incident edits and notification state preserved | PASS | `post-workflow-reimport.json`, `post-workflow-data-checks.json`, `data-final.json` |
| DATA-10 | Designated LOADTEST database, read only | Compare counts/checksums around final guarded demo cleanup/reimport | LOADTEST records unchanged | **47 tables / 1,675 rows**, identical row counts and SQL checksum aggregates | PASS | `load-before-final.json`, `load-after-final.json`, `load-isolation.json` |
| DATA-11 | Demo fixture incidents after operations | Inspect analysis/match persistence | No fabricated assessments | **0 AI analyses, 0 duplicate matches** | PASS | `data-final.json` |

The LOADTEST comparison covers the final guarded cleanup/reimport interval. SQL checksums are diagnostic, not cryptographic proof of every field; they supplement exact database-target guards and the seeder isolation tests. No LOADTEST seed or AI evaluation dataset was edited. Shared-data fingerprinting covers the listed projections, not every column in every table.

The final dataset intentionally retains human review history, completed resource assignments, acknowledgements, read reminders and application-created audit/escalation records. Cleanup is correctly blocked by this evidence. **Do not erase foreign records, remove the manifest or drop the demo database to force cleanup.** The fixture importer preserves borrowed accounts, including the test-only Operator. Verification-owned Web/API processes were stopped; the isolated SQL container and synthetic records were retained for review.

## Scenario outcomes

| Scenario | Role / preconditions | Action | Expected | Observed | Status |
|---|---|---|---|---|---|
| DEMO18-FLOOD-01 | Citizen/Operator/admin, imported report | Open details; Operator marks UnderReview | Stored report and explicit uncertainty; persisted review history | Six-detail API/MVC checks pass; flood UnderReview and history persist; casualties null | PASS — server |
| DEMO18-FIRE-01 | Imported report and manual escalation | Inspect uncertain occupants; Operator acknowledges | Unknown counts remain unknown; manual review state persists | Occupancy/casualties null; description and authored reason visible; Acknowledged timestamp recorded | PASS — server |
| DEMO18-ROAD-01 | Operator, available DEMO18 police resource | Assign through Web; admin executes lifecycle | Authorized assignment/transitions; correct conflict and final snapshot | Web CSRF/JWT assignment succeeded; repeat assignment rejected; five admin commands and six history entries per exercise; final Available, no active incident | PASS — server |
| DEMO18-FLOOD-02 / DEMO18-DUPLICATE-01 | No embeddings or assessed matches | Open duplicate-review route | Clear unavailable state; no invented similarity or merge | MVC 200 with unavailable warning; direct API 409; no scored matches/merge | PASS — unavailable-state handling |
| DEMO18-COLLAPSE-01 | Imported report, manual escalation | Inspect unknown casualties and acknowledge escalation | Human review, not inferred casualty/AI conclusions | Null casualty/occupancy fields; manual escalation acknowledged; no analysis fabricated | PASS — server |
| DEMO18-SITUATION-FLOOD | Three manually linked flood reports | Open and acknowledge situation | Linked state and aggregate caveats preserved | Three links, Acknowledged state; manual summary still warns about overlapping counts and unknown casualties | PASS — server |

These are stored synthetic descriptions and authored manual escalations/situation/reminders. Low/Normal intake values are not evaluated severity. Resource changes and acknowledgements above are genuine application transitions. They are not evidence of AI classification, automatic clustering, computed duplicate detection, live RAG or browser interaction.

## Functional defects discovered and fixed

| Defect ID | Reproduction / impact | Fix | Regression evidence | Result |
|---|---|---|---|---|
| DEF-DUP-01 | Authorized GET duplicate candidates without an embedding returned 500; MVC could not open the exercise | API returns explicit 409/no-conclusion indication. MVC checks assessment state and shows unavailable/pending/failed state; only the known 409 is handled as unavailable | Eight `DuplicatePageTests` cases; SQL demo test verifies all six no-embedding API responses and absence of match writes | Fixed locally |
| DEF-AUTH-01 | Operator command endpoint returned 403, but generic resource status reached business logic and returned 400 for an invalid transition | Apply the existing `ResourcesDispatch` policy to generic status endpoint | Two real-JWT SQL role cases; local assigned-resource probe returns 403 and leaves Assigned state/dispatch timestamp unchanged | Fixed locally |
| DEF-RESOURCE-01 | MarkAvailable completed the database assignment, but detail DTO still reported its historical incident as active | Separate active assignment fields from historical timing selection | Resource lifecycle unit assertions clear active fields while retaining timing; actual local lifecycle rerun confirms final Available with null active incident | Fixed locally |

Temporary verification runners also needed corrections: PowerShell loop-variable shadowing, stale `$LASTEXITCODE` after a PowerShell script, enum expectations, Open-only escalation queries, and invalid repeated state transitions. Those initial runner failures are not counted as independent application defects. Final runs below use corrected checks. Acknowledgement state was verified through incident-specific escalation retrieval and the normal situation detail endpoint; repeating an already completed transition is not expected to succeed.

## Remaining blockers and checks not executed

| Test ID | Preconditions / role | Action / expected | Observed / blocker | Status | Proposed next step |
|---|---|---|---|---|---|
| BROWSER-01 | User-connected Chrome | Enumerate/open actual local screens | After user restart, `cua.createBrowserTab("chrome", ...)` still fails: `failed to start codex app-server: The system cannot find the path specified. (os error 3)` | BLOCKED | Repair the desktop browser automation connection, then rerun visible workflows |
| VISUAL-01 | Working browser, synthetic roles | Capture and inspect actual dashboard/map/details/resources/situation/Copilot screenshots | Initial Part 2.2 capture unavailable; 2026-10-15 manual follow-up verifies flood/duplicate-unavailable/resource details and map; other views/interactions pending | NOT RUN — remaining views | See current Part 2.2B screenshot register above |
| SIGNALR-01 | Working authenticated browser | Disconnect/reconnect; refresh authoritative snapshots | Initial Part 2.2 connection unavailable; follow-up manual Console confirms initial connection only; actual message delivery/recovery not exercised | NOT RUN — recovery | Run browser reconnect exercise; initial connection is recorded separately |
| AI-01 | Valid live provider identity/deployment | Verify real assessment and failure/degraded state | Endpoint configured, live credential access not validated; no incident reprocessing/model request made | NOT RUN | Bounded real-provider exercise with measured output/failure; preserve evidence |
| RAG-01 | Permitted synthetic document, working Search/index/provider | Query Copilot and verify citations | `KnowledgeSearch:Endpoint` absent from local configuration; page response only | BLOCKED | Configure authorized Search and ingest an approved synthetic document |
| ACTION-01 | Valid proposal and authorized approver | Real proposal, approval, revalidation, audit/execution | No synthetic proposal fabricated; live controlled-action chain not exercised | NOT RUN | Exercise only a supported controlled action after prerequisites |
| MEDIA-01 | Authorized test image/video, storage/scanner/tools | Upload, analysis, review, private access | Dataset deliberately contains no media; no upload/frame/provider exercise performed | NOT RUN | Use approved synthetic media and available processing dependencies |
| ENV-01 | Weather provider and UI | Validate freshness/degraded weather representation | No live weather call or browser inspection performed | NOT RUN | Verify separately with configured dependency |
| UNCERTAINTY-01 | Operational context consumers | Preserve unknown counts throughout downstream context | Part 2.2B preserves context/linked incident nulls and labels situation aggregate limits; other downstream scoring/forecast aggregation remains limited | PASS for targeted paths; broader review NOT RUN | See Part 2.2B regression evidence; do not claim fully verified aggregate semantics |
| SECURITY-REST | Full system security scope | Realtime recipient isolation and full action-path security | Part 2.2B scopes IncidentUpdated to existing operators/reporter groups with unit regressions; live delivery and broader action security remain unverified | PASS for recipient selection; browser NOT RUN | Continue targeted security verification before pilot acceptance |

Browser charts, grids, map tiles, visual layout, accessibility, interactive controls and supported-browser compatibility remain unverified. Do not infer successful rendering from an HTTP 200 or describe a missing assessment as a successful zero-match analysis.

Source inspection also shows some incident escalation buttons rendered for Operators even though resolve permissions are denied by the API. Align visible action affordances with the permission matrix during later frontend work; server checks remain enforced. This is a markup observation, not a captured browser result.

## Commands and reproducibility

From the repository root, privately configure the exact local database targets and actors described above. This session loaded the existing ignored `.env.portfolio.ps1`; it is **not a tracked prerequisite for fresh clones**, and its secret values must not be published. No local configuration was overwritten.

```powershell
dotnet tool restore
dotnet restore AI-Emergency-Response.slnx
dotnet build AI-Emergency-Response.slnx --configuration Release --no-restore
dotnet test tests/EmergencyResponse.UnitTests --configuration Release --no-build --logger 'trx;LogFileName=unit-tests.trx' --results-directory TestResults/LocalE2E/Final/Unit
dotnet test tests/EmergencyResponse.IntegrationTests --configuration Release --no-build --logger 'trx;LogFileName=integration-tests.trx' --results-directory TestResults/LocalE2E/Final/Integration
dotnet ef migrations has-pending-model-changes --project src/EmergencyResponse.Infrastructure --startup-project src/EmergencyResponse.Api --context ApplicationDbContext --configuration Release --no-build

$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --project tools/EmergencyResponse.DemoData --configuration Release --no-build -- seed --reporter-email DEMO18-citizen@test.local --operator-email portfolio-admin@test.local
dotnet run --project tools/EmergencyResponse.DemoData --configuration Release --no-build -- cleanup
# Only before operational edits, after a successful preview on the isolated demo target:
dotnet run --project tools/EmergencyResponse.DemoData --configuration Release --no-build -- cleanup --apply
# Reseed using the same actors; do not apply cleanup after the expected dependent-record refusal.

$reporterPassword = Read-Host 'Synthetic Citizen password' -AsSecureString
$reviewerPassword = Read-Host 'Synthetic SystemAdmin password' -AsSecureString
./scripts/verify-demo-pages.ps1 -ReporterPassword $reporterPassword -ReviewerEmail portfolio-admin@test.local -ReviewerPassword $reviewerPassword -SeedResultPath artifacts/portfolio/local-e2e/seed-restored.json -OutputPath artifacts/portfolio/local-e2e/final-mvc-pages.json
git diff --check
```

Start normal Web/API as documented in [demo-script.md](demo-script.md). Set Development for both hosts, use the private isolated demo connection, and preserve the trusted HTTPS certificate and normal JWT configuration. Supply the Operator through the existing identity seeder/approved account-provisioning process, not direct role edits or test headers.

Two ignored diagnostic runners (`artifacts/portfolio/local-e2e/read-checks.ps1` and `operational-checks.ps1`) and an ignored C# harness supplied the additional evidence. They remain local diagnostics, not new application startup/seed infrastructure. They use this session's private environment/actors and are not advertised as general fresh-clone setup scripts. The committed MVC verifier and regression suites remain the reproducible repository checks. Detailed local outputs are ignored and must be reviewed for secrets before external sharing.

The following registers enumerate every final HTTP check. Preconditions are the authenticated local roles and migrated DEMO18 dataset above; read checks occurred before operational mutation, operational checks used normal Web cookies/CSRF/JWT, and the final MVC checks ran against the preserved edited dataset. Evidence paths are local ignored artifacts, not GitHub CI or staging claims.

## API read / authorization register — 52 checks

Evidence: `artifacts/portfolio/local-e2e/api-read-checks.json`; actual normal API JWTs, without test headers.

| Test ID | Role | Action | Expected | Actual observed result | Status |
|---|---|---|---|---|---|
| AUTH-Citizen | Citizen | Actual API password login and JWT role | Real login with expected role | Login succeeded; roles: Citizen | PASS |
| AUTH-Operator | Operator | Actual API password login and JWT role | Real login with expected role | Login succeeded; roles: Operator | PASS |
| AUTH-SystemAdmin | SystemAdmin | Actual API password login and JWT role | Real login with expected role | Login succeeded; roles: SystemAdmin | PASS |
| HEALTH-live | Anonymous | /health/live | 200; expected health or DEMO18 content | HTTP 200 | PASS |
| HEALTH-ready | Anonymous | /health/ready | 200; expected health or DEMO18 content | HTTP 200 | PASS |
| INC-Citizen-07cca8ac-89d8-721f-5559-3a42230bbfa7 | Citizen | GET /api/incidents/07cca8ac-89d8-721f-5559-3a42230bbfa7 | Stored DEMO18 incident; uncertainty preserved | DEMO18-FLOOD-01; casualty field null; description stored; no assessment inferred | PASS |
| INC-Citizen-4e46d5a5-fd74-3651-51df-6762f1b22c45 | Citizen | GET /api/incidents/4e46d5a5-fd74-3651-51df-6762f1b22c45 | Stored DEMO18 incident; uncertainty preserved | DEMO18-FLOOD-02; casualty field null; description stored; no assessment inferred | PASS |
| INC-Citizen-75ab3889-3a39-ac5a-399f-411b11b25cf7 | Citizen | GET /api/incidents/75ab3889-3a39-ac5a-399f-411b11b25cf7 | Stored DEMO18 incident; uncertainty preserved | DEMO18-COLLAPSE-01; casualty field null; description stored; no assessment inferred | PASS |
| INC-Citizen-a33aaca0-111f-06c2-c20f-73633677e667 | Citizen | GET /api/incidents/a33aaca0-111f-06c2-c20f-73633677e667 | Stored DEMO18 incident; uncertainty preserved | DEMO18-DUPLICATE-01; casualty field null; description stored; no assessment inferred | PASS |
| INC-Citizen-cc45b6e5-fb01-0658-e482-9711052f2320 | Citizen | GET /api/incidents/cc45b6e5-fb01-0658-e482-9711052f2320 | Stored DEMO18 incident; uncertainty preserved | DEMO18-ROAD-01; casualty field null; description stored; no assessment inferred | PASS |
| INC-Citizen-ffb1ba8a-cce0-ab58-d2d8-b184d4885520 | Citizen | GET /api/incidents/ffb1ba8a-cce0-ab58-d2d8-b184d4885520 | Stored DEMO18 incident; uncertainty preserved | DEMO18-FIRE-01; casualty field null; description stored; no assessment inferred | PASS |
| INC-Operator-07cca8ac-89d8-721f-5559-3a42230bbfa7 | Operator | GET /api/incidents/07cca8ac-89d8-721f-5559-3a42230bbfa7 | Stored DEMO18 incident; uncertainty preserved | DEMO18-FLOOD-01; casualty field null; description stored; no assessment inferred | PASS |
| INC-Operator-4e46d5a5-fd74-3651-51df-6762f1b22c45 | Operator | GET /api/incidents/4e46d5a5-fd74-3651-51df-6762f1b22c45 | Stored DEMO18 incident; uncertainty preserved | DEMO18-FLOOD-02; casualty field null; description stored; no assessment inferred | PASS |
| INC-Operator-75ab3889-3a39-ac5a-399f-411b11b25cf7 | Operator | GET /api/incidents/75ab3889-3a39-ac5a-399f-411b11b25cf7 | Stored DEMO18 incident; uncertainty preserved | DEMO18-COLLAPSE-01; casualty field null; description stored; no assessment inferred | PASS |
| INC-Operator-a33aaca0-111f-06c2-c20f-73633677e667 | Operator | GET /api/incidents/a33aaca0-111f-06c2-c20f-73633677e667 | Stored DEMO18 incident; uncertainty preserved | DEMO18-DUPLICATE-01; casualty field null; description stored; no assessment inferred | PASS |
| INC-Operator-cc45b6e5-fb01-0658-e482-9711052f2320 | Operator | GET /api/incidents/cc45b6e5-fb01-0658-e482-9711052f2320 | Stored DEMO18 incident; uncertainty preserved | DEMO18-ROAD-01; casualty field null; description stored; no assessment inferred | PASS |
| INC-Operator-ffb1ba8a-cce0-ab58-d2d8-b184d4885520 | Operator | GET /api/incidents/ffb1ba8a-cce0-ab58-d2d8-b184d4885520 | Stored DEMO18 incident; uncertainty preserved | DEMO18-FIRE-01; casualty field null; description stored; no assessment inferred | PASS |
| INC-SystemAdmin-07cca8ac-89d8-721f-5559-3a42230bbfa7 | SystemAdmin | GET /api/incidents/07cca8ac-89d8-721f-5559-3a42230bbfa7 | Stored DEMO18 incident; uncertainty preserved | DEMO18-FLOOD-01; casualty field null; description stored; no assessment inferred | PASS |
| INC-SystemAdmin-4e46d5a5-fd74-3651-51df-6762f1b22c45 | SystemAdmin | GET /api/incidents/4e46d5a5-fd74-3651-51df-6762f1b22c45 | Stored DEMO18 incident; uncertainty preserved | DEMO18-FLOOD-02; casualty field null; description stored; no assessment inferred | PASS |
| INC-SystemAdmin-75ab3889-3a39-ac5a-399f-411b11b25cf7 | SystemAdmin | GET /api/incidents/75ab3889-3a39-ac5a-399f-411b11b25cf7 | Stored DEMO18 incident; uncertainty preserved | DEMO18-COLLAPSE-01; casualty field null; description stored; no assessment inferred | PASS |
| INC-SystemAdmin-a33aaca0-111f-06c2-c20f-73633677e667 | SystemAdmin | GET /api/incidents/a33aaca0-111f-06c2-c20f-73633677e667 | Stored DEMO18 incident; uncertainty preserved | DEMO18-DUPLICATE-01; casualty field null; description stored; no assessment inferred | PASS |
| INC-SystemAdmin-cc45b6e5-fb01-0658-e482-9711052f2320 | SystemAdmin | GET /api/incidents/cc45b6e5-fb01-0658-e482-9711052f2320 | Stored DEMO18 incident; uncertainty preserved | DEMO18-ROAD-01; casualty field null; description stored; no assessment inferred | PASS |
| INC-SystemAdmin-ffb1ba8a-cce0-ab58-d2d8-b184d4885520 | SystemAdmin | GET /api/incidents/ffb1ba8a-cce0-ab58-d2d8-b184d4885520 | Stored DEMO18 incident; uncertainty preserved | DEMO18-FIRE-01; casualty field null; description stored; no assessment inferred | PASS |
| READ-Operator-/api/resources | Operator | /api/resources | 200; expected health or DEMO18 content | HTTP 200; DEMO18 content present | PASS |
| READ-Operator-/api/resources/command-center | Operator | /api/resources/command-center | 200; expected health or DEMO18 content | HTTP 200; DEMO18 content present | PASS |
| READ-Operator-/api/situations/command-center | Operator | /api/situations/command-center | 200; expected health or DEMO18 content | HTTP 200; DEMO18 content present | PASS |
| READ-Operator-/api/situations/open/map | Operator | /api/situations/open/map | 200; expected health or DEMO18 content | HTTP 200; DEMO18 content present | PASS |
| READ-Operator-/api/escalations/open | Operator | /api/escalations/open | 200; expected health or DEMO18 content | HTTP 200; DEMO18 content present | PASS |
| READ-Operator-/api/map/incidents | Operator | /api/map/incidents | 200; expected health or DEMO18 content | HTTP 200; DEMO18 content present | PASS |
| READ-Operator-/api/map/resources | Operator | /api/map/resources | 200; expected health or DEMO18 content | HTTP 200; DEMO18 content present | PASS |
| READ-SystemAdmin-/api/resources | SystemAdmin | /api/resources | 200; expected health or DEMO18 content | HTTP 200; DEMO18 content present | PASS |
| READ-SystemAdmin-/api/resources/command-center | SystemAdmin | /api/resources/command-center | 200; expected health or DEMO18 content | HTTP 200; DEMO18 content present | PASS |
| READ-SystemAdmin-/api/situations/command-center | SystemAdmin | /api/situations/command-center | 200; expected health or DEMO18 content | HTTP 200; DEMO18 content present | PASS |
| READ-SystemAdmin-/api/situations/open/map | SystemAdmin | /api/situations/open/map | 200; expected health or DEMO18 content | HTTP 200; DEMO18 content present | PASS |
| READ-SystemAdmin-/api/escalations/open | SystemAdmin | /api/escalations/open | 200; expected health or DEMO18 content | HTTP 200; DEMO18 content present | PASS |
| READ-SystemAdmin-/api/map/incidents | SystemAdmin | /api/map/incidents | 200; expected health or DEMO18 content | HTTP 200; DEMO18 content present | PASS |
| READ-SystemAdmin-/api/map/resources | SystemAdmin | /api/map/resources | 200; expected health or DEMO18 content | HTTP 200; DEMO18 content present | PASS |
| DENY-Citizen-/api/resources | Citizen | /api/resources | Authorization rejects request | HTTP 403 | PASS |
| DENY-Citizen-/api/situations/command-center | Citizen | /api/situations/command-center | Authorization rejects request | HTTP 403 | PASS |
| DENY-Citizen-/api/escalations/open | Citizen | /api/escalations/open | Authorization rejects request | HTTP 403 | PASS |
| DENY-Anonymous-Incident | Anonymous | /api/incidents/{id} | Authorization rejects request | HTTP 401 | PASS |
| NOTIFY-Citizen | Citizen | /api/notifications | Three admin reminders; zero for other actors | 0 reviewer-scoped DEMO18 reminders | PASS |
| NOTIFY-Operator | Operator | /api/notifications | Three admin reminders; zero for other actors | 0 reviewer-scoped DEMO18 reminders | PASS |
| NOTIFY-SystemAdmin | SystemAdmin | /api/notifications | Three admin reminders; zero for other actors | 3 reviewer-scoped DEMO18 reminders | PASS |
| DUP-07cca8ac-89d8-721f-5559-3a42230bbfa7 | Operator | /api/incidents/07cca8ac-89d8-721f-5559-3a42230bbfa7/duplicates | 409; explicit unavailable assessment | HTTP 409; no assessment/merge claimed | PASS |
| DUP-4e46d5a5-fd74-3651-51df-6762f1b22c45 | Operator | /api/incidents/4e46d5a5-fd74-3651-51df-6762f1b22c45/duplicates | 409; explicit unavailable assessment | HTTP 409; no assessment/merge claimed | PASS |
| DUP-75ab3889-3a39-ac5a-399f-411b11b25cf7 | Operator | /api/incidents/75ab3889-3a39-ac5a-399f-411b11b25cf7/duplicates | 409; explicit unavailable assessment | HTTP 409; no assessment/merge claimed | PASS |
| DUP-a33aaca0-111f-06c2-c20f-73633677e667 | Operator | /api/incidents/a33aaca0-111f-06c2-c20f-73633677e667/duplicates | 409; explicit unavailable assessment | HTTP 409; no assessment/merge claimed | PASS |
| DUP-cc45b6e5-fb01-0658-e482-9711052f2320 | Operator | /api/incidents/cc45b6e5-fb01-0658-e482-9711052f2320/duplicates | 409; explicit unavailable assessment | HTTP 409; no assessment/merge claimed | PASS |
| DUP-ffb1ba8a-cce0-ab58-d2d8-b184d4885520 | Operator | /api/incidents/ffb1ba8a-cce0-ab58-d2d8-b184d4885520/duplicates | 409; explicit unavailable assessment | HTTP 409; no assessment/merge claimed | PASS |
| AUTH-Operator-command | Operator | POST resource command | Authorization rejects request | HTTP 403; dispatch rejected | PASS |
| AUTH-Operator-status | Operator | POST generic resource status | Authorization rejects request | HTTP 403; generic route rejected | PASS |

## Operational register — 29 checks

Evidence: `artifacts/portfolio/local-e2e/operational-checks.json`. Acknowledgement/review checks preserve existing completed transitions and confirm persisted history; resource commands were executed against the synthetic resource.

| Test ID | Role | Action | Expected | Actual observed result | Status |
|---|---|---|---|---|---|
| WEB-AUTH-Citizen | Citizen | Web login, cookie and JWT bridge | Real cookie login and JWT bridge | Actual antiforgery-protected Web login succeeded | PASS |
| WEB-AUTH-Operator | Operator | Web login, cookie and JWT bridge | Real cookie login and JWT bridge | Actual antiforgery-protected Web login succeeded | PASS |
| WEB-AUTH-SystemAdmin | SystemAdmin | Web login, cookie and JWT bridge | Real cookie login and JWT bridge | Actual antiforgery-protected Web login succeeded | PASS |
| WEB-DUP-07cca8ac-89d8-721f-5559-3a42230bbfa7 | Operator | /Incidents/Duplicates/{id} | Unavailable notice; no fabricated match/merge | HTTP 200; unavailable assessment notice; no invented score or merge | PASS |
| WEB-DUP-4e46d5a5-fd74-3651-51df-6762f1b22c45 | Operator | /Incidents/Duplicates/{id} | Unavailable notice; no fabricated match/merge | HTTP 200; unavailable assessment notice; no invented score or merge | PASS |
| WEB-DUP-75ab3889-3a39-ac5a-399f-411b11b25cf7 | Operator | /Incidents/Duplicates/{id} | Unavailable notice; no fabricated match/merge | HTTP 200; unavailable assessment notice; no invented score or merge | PASS |
| WEB-DUP-a33aaca0-111f-06c2-c20f-73633677e667 | Operator | /Incidents/Duplicates/{id} | Unavailable notice; no fabricated match/merge | HTTP 200; unavailable assessment notice; no invented score or merge | PASS |
| WEB-DUP-cc45b6e5-fb01-0658-e482-9711052f2320 | Operator | /Incidents/Duplicates/{id} | Unavailable notice; no fabricated match/merge | HTTP 200; unavailable assessment notice; no invented score or merge | PASS |
| WEB-DUP-ffb1ba8a-cce0-ab58-d2d8-b184d4885520 | Operator | /Incidents/Duplicates/{id} | Unavailable notice; no fabricated match/merge | HTTP 200; unavailable assessment notice; no invented score or merge | PASS |
| RESOURCE-WEB-ASSIGN | Operator | POST /Resources/Assign with actual CSRF/cookie/JWT bridge | Authorized state/content persists in snapshot/history | Assigned through Web bridge; API confirms Assigned with correct incident | PASS |
| RESOURCE-CONFLICT | Operator | Repeat assignment of the already assigned resource | Reject second assignment; preserve first | HTTP 400; existing assignment preserved (serialized conflict; SQL stale-writer race separately tested) | PASS |
| RESOURCE-DISPATCH-DENIED | Operator | Dispatch through generic status endpoint | 403; no unauthorized state mutation | HTTP 403; still Assigned; dispatch timestamp null | PASS |
| RESOURCE-COMMAND-1 | SystemAdmin | Authorized resource command and snapshot refresh | Authorized state/content persists in snapshot/history | HTTP 200; authoritative status 3 | PASS |
| RESOURCE-COMMAND-2 | SystemAdmin | Authorized resource command and snapshot refresh | Authorized state/content persists in snapshot/history | HTTP 200; authoritative status 4 | PASS |
| RESOURCE-COMMAND-3 | SystemAdmin | Authorized resource command and snapshot refresh | Authorized state/content persists in snapshot/history | HTTP 200; authoritative status 5 | PASS |
| RESOURCE-COMMAND-4 | SystemAdmin | Authorized resource command and snapshot refresh | Authorized state/content persists in snapshot/history | HTTP 200; authoritative status 6 | PASS |
| RESOURCE-COMMAND-5 | SystemAdmin | Authorized resource command and snapshot refresh | Authorized state/content persists in snapshot/history | HTTP 200; authoritative status 1 | PASS |
| RESOURCE-HISTORY | SystemAdmin | GET resource history | Authorized state/content persists in snapshot/history | All six assignment and command transitions recorded | PASS |
| INCIDENT-STATUS | Operator | PATCH Submitted to UnderReview then retrieve history | Authorized state/content persists in snapshot/history | UnderReview persisted with history; unknown casualties remain null | PASS |
| ESC-ACK-702ab65b-38c6-076c-933e-e0c1745499d6 | Operator | Acknowledge authored manual escalation | Authorized state/content persists in snapshot/history | Acknowledged; still requires human review; no AI score invented | PASS |
| ESC-RESOLVE-DENY-702ab65b-38c6-076c-933e-e0c1745499d6 | Operator | Attempt escalation resolve without permission | 403; no unauthorized state mutation | HTTP 403 | PASS |
| ESC-ACK-cecd03da-57d3-b13b-f0b5-5797e0077b23 | Operator | Acknowledge authored manual escalation | Authorized state/content persists in snapshot/history | Acknowledged; still requires human review; no AI score invented | PASS |
| ESC-RESOLVE-DENY-cecd03da-57d3-b13b-f0b5-5797e0077b23 | Operator | Attempt escalation resolve without permission | 403; no unauthorized state mutation | HTTP 403 | PASS |
| SITUATION-ACK | Operator | Acknowledge the manual flood situation | Authorized state/content persists in snapshot/history | Acknowledged with three linked reports; aggregate caveats retained | PASS |
| SITUATION-RESOLVE-DENY | Operator | Attempt situation resolve without permission | 403; no unauthorized state mutation | HTTP 403 | PASS |
| NOTIFY-READ-DENY | Operator | Attempt to read reviewer-owned notification | 403; no unauthorized state mutation | HTTP 403 | PASS |
| NOTIFY-READ | SystemAdmin | Mark owned review reminder read then refresh | Authorized state/content persists in snapshot/history | Owned notification marked read and persisted | PASS |
| COPILOT-PAGE-Operator | Operator | GET /Copilot | Authorized page; no model-output claim | HTTP 200; page response only, no model or citation claim | PASS |
| COPILOT-PAGE-SystemAdmin | SystemAdmin | GET /Copilot | Authorized page; no model-output claim | HTTP 200; page response only, no model or citation claim | PASS |

## Final MVC / snapshot register — 19 checks

Evidence: `artifacts/portfolio/local-e2e/final-mvc-pages.json`. These are real HTTPS responses through Web login and the API bridge, not browser rendering.

| Test ID | Role | Action | Expected | Actual observed result | Status |
|---|---|---|---|---|---|
| MVC-01 | Citizen | /Incidents?search=DEMO18-&pageSize=50 | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-FLOOD-01, DEMO18-FIRE-01, DEMO18-ROAD-01, DEMO18-COLLAPSE-01, DEMO18-FLOOD-02, DEMO18-DUPLICATE-01 | PASS |
| MVC-02 | Citizen | /Incidents/MyReports?pageSize=50 | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-FLOOD-01 | PASS |
| MVC-03 | Citizen | /Incidents/Details/07cca8ac-89d8-721f-5559-3a42230bbfa7 | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-, DEMO18 synthetic exercise only. | PASS |
| MVC-04 | Citizen | /Incidents/Details/4e46d5a5-fd74-3651-51df-6762f1b22c45 | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-, DEMO18 synthetic exercise only. | PASS |
| MVC-05 | Citizen | /Incidents/Details/75ab3889-3a39-ac5a-399f-411b11b25cf7 | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-, DEMO18 synthetic exercise only. | PASS |
| MVC-06 | Citizen | /Incidents/Details/a33aaca0-111f-06c2-c20f-73633677e667 | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-, DEMO18 synthetic exercise only. | PASS |
| MVC-07 | Citizen | /Incidents/Details/cc45b6e5-fb01-0658-e482-9711052f2320 | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-, DEMO18 synthetic exercise only. | PASS |
| MVC-08 | Citizen | /Incidents/Details/ffb1ba8a-cce0-ab58-d2d8-b184d4885520 | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-, DEMO18 synthetic exercise only. | PASS |
| MVC-09 | SystemAdmin | /Incidents?search=DEMO18-&pageSize=50 | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-FIRE-01, DEMO18-COLLAPSE-01 | PASS |
| MVC-10 | SystemAdmin | /Resources | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-BOAT, DEMO18-FIRE-TRUCK, DEMO18-AMBULANCE | PASS |
| MVC-11 | SystemAdmin | /Situations | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-SITUATION-FLOOD | PASS |
| MVC-12 | SystemAdmin | /Situations/Details/ada4cb0e-f746-7998-2a6d-a0a021002750 | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-SITUATION-FLOOD, may double-count | PASS |
| MVC-13 | SystemAdmin | /Notifications | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-FIRE-01, DEMO18-COLLAPSE-01, DEMO18-flood-group | PASS |
| MVC-14 | SystemAdmin | /Dashboard | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-FLOOD-01, Open Escalation Alerts | PASS |
| MVC-15 | SystemAdmin | /Map | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-FLOOD-01, DEMO18-BOAT | PASS |
| MVC-16 | SystemAdmin | /Map/Snapshot | 200 and expected DEMO18 content | HTTP 200; markers: DEMO18-FLOOD-01, DEMO18-BOAT | PASS |
| MVC-17 | Citizen | /Notifications | Reviewer reminders absent | HTTP 200; Reviewer reminders absent | PASS |
| MVC-18 | Citizen | /Resources | Redirected to AccessDenied | HTTP 200; Redirected to AccessDenied | PASS |
| MVC-19 | Anonymous | /Incidents | Redirected to Login | HTTP 200; Redirected to Login | PASS |
