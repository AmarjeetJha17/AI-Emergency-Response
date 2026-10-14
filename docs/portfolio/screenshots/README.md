# Synthetic Screenshot Index

Only images captured from the actual local application belong here. No generated UI mockups or fabricated AI answers. Screenshots must contain synthetic records, no secrets or real citizen data. Use only clearly synthetic actor labels; omit private machine/provider identifiers and browser account details.

Phase 18 Part 2.2 was accepted as complete with remaining verification deferred by the owner on 2026-10-15. Pending/captured-before-correction statuses in this inventory remain unchanged; see [the deferred backlog](../local-demo-verification.md#phase-closure--2026-10-15).

**2026-10-14:** capture was explicitly deferred by the owner while browser automation was unavailable. **2026-10-15:** the owner supplied twelve real Chrome screenshots in three batches: map controls/tiles/Console, flood details, duplicate unavailable state, resource details/history, map Issues, fire details/location and situation details/links. Reviewed copies have documented cropping/redaction. Fire/situation captures precede the resulting presentation/resolve corrections; corrected rendering and other screens remain pending. No live-AI output was fabricated.

Phase 18 Part 2.1 verified DEMO18 data through authenticated local MVC responses and snapshots, including dashboard, map, incident details, resources and situations. That is server-response evidence only. The manual Part 2.2B captures add the bounded visual results below; interactions and unshown screens remain pending. See [actual verification](../synthetic-demo-data.md).

| View | File | Status | Observed evidence |
|---|---|---|---|
| Dashboard | dashboard.png | Pending | Local authenticated dashboard |
| Live map | [live-map.png](live-map.png) | Captured and inspected — limited PASS | Real tiles/markers/clusters, six incidents and six resources; no selected-report drawer or toggled-layer evidence |
| Flood incident details | [incident-details.png](incident-details.png) | Captured and inspected — limited PASS | DEMO18-FLOOD-01 description, UnderReview and explicit prediction unavailable; lower assessment/history not shown |
| Duplicate unavailable state | [duplicate-assessment-unavailable.png](duplicate-assessment-unavailable.png) | Captured and inspected — limited PASS | NotStarted, no conclusion and no merge; no scored match fabricated |
| Completed resource details/history | [resource-details.png](resource-details.png) | Captured and inspected — limited PASS | DEMO18 police Available/Unassigned; retained road lifecycle history; actor IDs redacted |
| Resource command center | resources.png | Pending | Synthetic inventory/assignment |
| Fire detail/manual escalation | [fire-details-before-correction.png](fire-details-before-correction.png) | Captured before correction — limited data PASS | Explicit unknown occupants, manual reason and acknowledged escalation; forbidden Operator Resolve button is defect evidence |
| Situation detail | [situation-details-before-correction.png](situation-details-before-correction.png) | Captured before correction — data PASS / presentation FAIL | Manual acknowledged summary/count caveat; unsupported evaluation/Resolve presentation prompted fixes |
| Situation links | [situation-links-before-correction.png](situation-links-before-correction.png) | Captured before correction — data PASS / provenance FAIL | Three named links with casualties unknown; false scheduled/grouping captions prompted fixes |
| Copilot citations | copilot-citations.png | Pending | Actual permitted retrieval and response |

For each captured image record date, source commit, environment, actor role, synthetic dataset, provider mode, and observations in the feature register. A UI rendering is not proof of authorization, grounding, recovery, or Azure staging verification. Pending screens remain visible until observed.

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
