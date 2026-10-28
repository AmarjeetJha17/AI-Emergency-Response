# Synthetic Screenshot Index

Only images captured from the actual local application belong here. No generated UI mockups or fabricated AI answers. Screenshots must contain synthetic records, no secrets or real citizen data. Use only clearly synthetic actor labels; omit private machine/provider identifiers and browser account details.

Phase 18 Part 2.2 was accepted as complete with remaining verification deferred by the owner on 2026-10-15. Pending/captured-before-correction statuses in this inventory remain unchanged; see [the deferred backlog](../local-demo-verification.md#phase-closure--2026-10-15).

**2026-10-14:** capture was explicitly deferred by the owner while browser automation was unavailable. **2026-10-15:** the owner supplied twelve real Chrome screenshots in three batches: map controls/tiles/Console, flood details, duplicate unavailable state, resource details/history, map Issues, fire details/location and situation details/links. Reviewed copies have documented cropping/redaction. Fire/situation captures precede the resulting presentation/resolve corrections; corrected rendering and other screens remain pending. No live-AI output was fabricated.

**Phase 18 Part 2.4 status (updated 2026-10-18):** the owner supplied current local-app screenshots at 13:57–14:31. Twelve unaltered copies are indexed below. They show a signed-in Test Operator session and current incident, dashboard, map, duplicate-review, notification, report-form, Copilot-question, Resource Command Center, and Situation Center views. The image dimensions are approximately 1858–1899 pixels wide; the exact browser viewport, source commit, and data-seed identity were not supplied. These are bounded desktop-rendering evidence, not responsive acceptance or a completed end-to-end walkthrough. The Resource Command Center capture is still loading eligible incidents; the Situation Center shows zero open situations; the Copilot capture has no answer and a separate Visual Studio capture documents the EF tracking exception. Do not present these three views as successful workflows. The earlier 2026-10-17 Map capture with the open DevTools Console and visible `access_token` was not archived and remains unsafe to share. The new 2026-10-18 map screenshots show the application without DevTools; they do not independently prove token-safe console logging or reconnect behavior.

Phase 18 Part 2.1 verified DEMO18 data through authenticated local MVC responses and snapshots, including dashboard, map, incident details, resources and situations. That is server-response evidence only. The manual Part 2.2B captures add the bounded visual results below; interactions and unshown screens remain pending. See [actual verification](../synthetic-demo-data.md).

| View | File | Status | Observed evidence |
|---|---|---|---|
| Dashboard | [dashboard-2026-10-18.png](dashboard-2026-10-18.png) | Captured — bounded rendering evidence | Operations overview shows 12 active/unassigned incidents, eight critical, six open escalation alerts, and eight unread alerts in the supplied local view; dataset identity and source revision were not supplied. |
| Incident detail and review | [incident-details-rockslide-2026-10-18.png](incident-details-rockslide-2026-10-18.png), [incident-visual-review-2026-10-18.png](incident-visual-review-2026-10-18.png), [incident-visual-review-findings-2026-10-18.png](incident-visual-review-findings-2026-10-18.png) | Captured — evidence requires reconciliation | Report text mentions trapped passengers while visual analysis says no people are visible; the screen reports 18 affected people and four vehicles. These are conflicting report/model claims, not verified facts. Operator verification is required before any operational use. |
| Live map | [live-map.png](live-map.png) | Historical capture, inspected — limited evidence only | 2026-10-15 image shows map tiles, markers/layers, six incidents and six resources before Part 2.3.4 polish; no selected-report drawer or toggled-layer evidence; attribution partly clipped |
| Flood incident details | [incident-details.png](incident-details.png) | Historical capture, inspected — data subset only | 2026-10-15 image shows DEMO18-FLOOD-01 description, UnderReview and prediction unavailable; older shell/UI; lower assessment/history not shown |
| Duplicate unavailable state | [duplicate-assessment-unavailable.png](duplicate-assessment-unavailable.png) | Historical capture, inspected — state subset only | 2026-10-15 image shows NotStarted, no conclusion and no merge; no scored match fabricated; predates later UI polish |
| Completed resource details/history | [resource-details.png](resource-details.png) | Historical capture, inspected — data subset only | 2026-10-15 image shows DEMO18 police Available/Unassigned and retained road lifecycle history; older shell/UI; actor IDs redacted |
| Incident queue | [incident-queue-2026-10-18.png](incident-queue-2026-10-18.png), [incident-queue-continued-2026-10-18.png](incident-queue-continued-2026-10-18.png) | Captured — bounded rendering evidence | 12 matching reports and visible status/severity/priority/date fields; only a subset of the queue is shown. |
| Incident report form | [report-form-2026-10-18.png](report-form-2026-10-18.png) | Captured — form layout only | Required title/type/description fields and guidance are visible; no report was submitted. |
| Live map | [live-map-overview-2026-10-18.png](live-map-overview-2026-10-18.png), [live-map-filters-2026-10-18.png](live-map-filters-2026-10-18.png) | Captured — bounded rendering evidence | Map screenshot shows 12/12 filtered incidents, three clusters (2, 6, 4), tiles and legend; companion screenshot shows filter controls and “Live updates connected.” No layer toggle, selected incident drawer, or reconnect sequence is evidenced. |
| Duplicate review | [duplicate-review-2026-10-18.png](duplicate-review-2026-10-18.png), [duplicate-review-decision-2026-10-18.png](duplicate-review-decision-2026-10-18.png) | Captured — advisory/review-form evidence | Candidate comparison and review decision form are visible. A 94% candidate score is not a confirmed duplicate; no decision was saved in the evidence. |
| Notifications | [home-notifications-2026-10-18.png](home-notifications-2026-10-18.png) | Captured — inbox layout/state only | Eight unread badge and notification menu visible; displayed items are about 15 days old. No delivery or recipient-isolation result is shown. |
| Resource command center | [resource-command-center-loading-2026-10-18.png](resource-command-center-loading-2026-10-18.png) | Captured — **loading defect/pending** | Page shows zero tracked units and remains at “Loading eligible incidents…”. This is defect evidence, not a successful resource workflow. |
| Copilot question form | [copilot-question-form-2026-10-18.png](copilot-question-form-2026-10-18.png) | Captured — **answer pending** | Question is present in the form; no response, citation, or grounding state is visible. Separate Visual Studio capture shows the duplicate tracked `CopilotConversation` exception; regression fix is pending successful build/test. |
| Situation Center | [situation-command-center-2026-10-18.png](situation-command-center-2026-10-18.png) | Captured — **empty current state** | Snapshot reports zero open/critical situations and zero resource shortages; it does not show the DEMO18 situation detail or linked reports. |
| Fire detail/manual escalation | [fire-details-before-correction.png](fire-details-before-correction.png) | Captured before correction — limited data PASS | Explicit unknown occupants, manual reason and acknowledged escalation; forbidden Operator Resolve button is defect evidence |
| Situation detail | [situation-details-before-correction.png](situation-details-before-correction.png) | Captured before correction — data PASS / presentation FAIL | Manual acknowledged summary/count caveat; unsupported evaluation/Resolve presentation prompted fixes |
| Situation links | [situation-links-before-correction.png](situation-links-before-correction.png) | Captured before correction — data PASS / provenance FAIL | Three named links with casualties unknown; false scheduled/grouping captions prompted fixes |
| Copilot citations | copilot-citations.png | Pending | Actual permitted retrieval and response |
| Owner-provided current Map (not stored) | Not archived | Inspected, unsafe for portfolio use | 2026-10-17 Chrome view at 1230 × 1285 emulated viewport; Test Operator, 12 filtered incidents, zero resources, snapshot retrieved, SignalR connected. DevTools Console displayed an access token in the SignalR URL. A newer clean-window capture is indexed above, but logger redaction and reconnect are still not proven by a screenshot. |
| Owner-provided Login page (not stored) | Not archived | Inspected, limited layout evidence | 2026-10-17 Chrome view of `/Account/Login`; does not prove authentication or any operational workflow. Console showed a blocked Google script request whose caller was not identified. |

For each captured image record date, source commit, environment, actor role, synthetic dataset, provider mode, and observations in the feature register. A UI rendering is not proof of authorization, grounding, recovery, or Azure staging verification. Pending screens remain visible until observed.

## Manual capture checklist

1. Start the local API and Web using the reviewed [setup guide](../demo-script.md). Confirm the intended local database name and apply reviewed migrations before startup. Confirm the current worktree/commit and synthetic provider mode.
2. Open Chrome at 1440 × 900 and the actual local application. Use a separate synthetic role session as appropriate. Never capture credentials, tokens, personal information, or browser account details.
3. Capture the real dashboard, corrected incident details, map, resource command center, Copilot state, and situation/notifications workflow. Capture the current status text exactly as rendered. If a provider or service is unavailable, capture that truthful unavailable state rather than retrying until a fabricated success appears.
4. Inspect each image at full size for clipping, illegible content, errors, personal data, tokens, and misleading status. Record the role, date, source revision, environment, and what the image proves. Preserve the original capture; any redaction must be documented and must not change the UI state.
5. Repeat key screens at 1024 × 768 and 390 × 844 for responsive inspection, even if the portfolio index keeps one 1440 × 900 representative per view.

## Map evidence provenance

- Source: owner's Chrome screenshots `002625`, `002632`, `002701` on 2026-10-15; source revision `bf63d0e` plus uncommitted Part 2.2/2.2B fixes.
- Environment: normal local Development Release Web/API hosts; isolated DEMO18 database; authenticated Operator; no live AI assessment was generated.
- [live-map-controls.png](live-map-controls.png): browser chrome and navbar cropped from source `002625`; map controls/counters retained.
- [live-map.png](live-map.png): browser toolbar cropped from source `002632`; map tiles/markers and legend retained. Attribution is partly clipped by the original capture; full attribution verification remains pending.
- [live-map-console-redacted.png](live-map-console-redacted.png): browser toolbar cropped from source `002701`; WebSocket URL and wrapped continuation visibly redacted to remove the access-token value. Two actual connection messages remain visible. Its three unexpanded Issues were identified in the later capture below; no Network trace was supplied.
- Source images were preserved. No map, metric, assessment or connection message was synthesized. See [manual browser results and limits](../local-demo-verification.md#inspected-manual-screenshot-evidence--2026-10-15).

## Additional evidence provenance

Same 2026-10-15 local hosts, source revision and synthetic Operator session described above:

- `incident-details.png`: source `005616`, original viewport retained; no browser toolbar in source. Prediction unavailable is actual rendered text, not a model result.
- `duplicate-assessment-unavailable.png`: source `005621`, browser toolbar cropped; warning retained unchanged.
- `resource-details.png`: source `005629`, browser toolbar cropped; actor GUID column values visibly redacted. Visible final state/history were created by the earlier simulated verification workflow, not new manual dispatch commands.
- [map-issues-redacted.png](map-issues-redacted.png): source `005725`, browser toolbar cropped; token-bearing Console URL/continuation redacted. Three missing form-label associations are visible and remain an open accessibility finding.
- [resource-console-redacted.png](resource-console-redacted.png): source `005731`, cropped to Console pane to omit browser chrome and local actor-ID table; token-bearing Console URL/continuation redacted. Initial connection messages and No issues are visible; no Network or reconnect proof.

All copies were visually inspected after cropping/redaction. Original source files remain unchanged. Full flood/fire assessment/history, collapse, dashboard, incident filtering, command centers, inbox, map interactions and Copilot evidence remain pending. See [second-batch findings](../local-demo-verification.md#second-manual-evidence-batch--flood-duplicate-resource-and-map-issues).

## Fire/situation evidence before correction

Same local Operator session, source `bf63d0e` plus the then-current uncommitted fixes; source originals preserved:

- `fire-details-before-correction.png`: source `010901`; synthetic acknowledgement actor GUID visibly redacted. Unknown report facts and manual acknowledged escalation remain visible, along with the original Resolve affordance defect.
- [fire-location.png](fire-location.png): source `010914`; actual synthetic FIRE-01 coordinates and available status/recommendation controls; no command result or history row is shown.
- `situation-details-before-correction.png`: source `010920`; count warning, manual group and acknowledgement, plus original misleading evaluated/availability display and Resolve control.
- `situation-links-before-correction.png`: source `010927`; three real linked report rows and unknown casualty labels, plus original unsupported scheduled/automatic-grouping captions.

These are original rendered results retained as defect evidence, not retouched illustrations of the fix. Corrected text/Operator resolve guards passed real-cookie MVC checks; refreshed browser screenshots are still needed. See [third-batch findings and actual regression results](../local-demo-verification.md#third-manual-evidence-batch--fire-and-linked-situation-before-correction).
