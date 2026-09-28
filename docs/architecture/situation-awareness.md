# Operational Situation Awareness Architecture

## 1. Overview & Operational Principles

An **Operational Situation** is an aggregate operational construct representing a collection of active emergency incidents that appear to belong to the same broader crisis event.

For example:
```
INC-101: Flooded arterial road
INC-102: Stranded commuter vehicle
INC-103: Power substation water intrusion
INC-104: Water entering residential settlement
               ↓
SIT-20260927-01: "Urban Flash Flooding & Infrastructure Hazard - Sector 4"
```

The system explicitly distinguishes between individual incident reports and the correlated situation:
> *"These reports appear operationally related."*

The architecture provides multi-incident coordination, aggregate impact assessment, and automated situational intelligence while strictly preserving human operator command.

---

## 2. Advisory Boundaries & Safety Guarantees

The operational situation engine adheres strictly to the platform's non-destructive, advisory-only safety principles:

| Safety Guardrail | Implementation |
| :--- | :--- |
| **No Automated Dispatch** | Situations provide aggregate resource demand analysis and shortage flags, but never automatically issue dispatch commands. |
| **Non-Destructive Lifecycle** | Resolving or dismissing an operational situation **never** closes, rejects, or alters member incidents. Incidents retain their independent lifecycle. |
| **No Unilateral Severity Changes** | Situations compute an aggregate situational score and escalation level for situational awareness; member incident severities remain unaffected. |
| **Advisory Map Envelopes** | Visual radius envelopes on maps are strictly operational awareness indicators ($1.20\times$ max member distance, min 500m), **never** official evacuation zones, legal perimeters, or dispatch borders. |
| **Advisory AI Summaries** | AI situational summaries and recommended focuses are advisory only to support incident commanders. No public announcements or evacuation orders are issued automatically. |

---

## 3. Pure Spatiotemporal Clustering

The [`SituationClusteringEngine`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Application/Situations/SituationClusteringEngine.cs) performs pure, deterministic spatial and temporal clustering:

1. **Continuous Chain Linking**:
   - Two incidents $A$ and $B$ are grouped if distance $(A, B) \le \text{MaxClusterDistanceKm}$ (default 10 km) and $|\text{Time}(A) - \text{Time}(B)| \le \text{MaxTimeWindowHours}$ (default 6 hours).
   - If incident $C$ is close to $B$, the chain links $A \leftrightarrow B \leftrightarrow C$, forming a cohesive operational corridor without requiring a single geometric centroid.
2. **Minimum Incident Threshold**:
   - A cluster is only formed when it contains at least $\text{MinIncidentCount}$ (default 3) active incidents.
3. **Primary Incident Determination**:
   - The primary incident (focal point) is deterministically selected as the incident with:
     1. Highest Severity
     2. Highest Priority
     3. Earliest Reported Timestamp

---

## 4. Deterministic Scoring & Level Mapping

Situations compute a composite severity score ($0.0 - 100.0$) via [`SituationScoreCalculator`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Application/Situations/SituationScoreCalculator.cs):

$$\text{Score} = 100 \times \left( 0.40 \cdot S_{\text{critical}} + 0.25 \cdot S_{\text{high}} + 0.15 \cdot S_{\text{count}} + 0.10 \cdot S_{\text{people}} + 0.10 \cdot S_{\text{casualties}} \right)$$

Where:
- $S_{\text{critical}} = \min(1.0, \frac{\text{CriticalCount}}{5})$
- $S_{\text{high}} = \min(1.0, \frac{\text{HighCount}}{10})$
- $S_{\text{count}} = \min(1.0, \frac{\text{IncidentCount}}{20})$
- $S_{\text{people}} = \min(1.0, \frac{\text{PeopleAffected}}{100})$
- $S_{\text{casualties}} = \min(1.0, \frac{\text{Casualties}}{20})$

The composite score is deterministically mapped to an [`EscalationLevel`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Domain/Enums/EscalationLevel.cs) via [`SituationLevelCalculator`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Application/Situations/SituationLevelCalculator.cs):
- $\text{Score} \ge 75.0 \implies \text{Critical}$
- $\text{Score} \ge 50.0 \implies \text{High}$
- $\text{Score} \ge 25.0 \implies \text{Elevated}$
- $\text{Score} < 25.0 \implies \text{Advisory}$

---

## 5. Anti-Duplication & Overlap Model

To prevent duplicate situation records when new incidents arrive or clusters shift:
- Any detected cluster sharing $\ge 50\%$ member incident overlap (`OverlapThreshold`) with an existing open situation is merged into the existing situation record.
- New member incidents are appended to [`OperationalSituationIncident`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Domain/Entities/OperationalSituationIncident.cs) associations.
- The situation metrics, score, and level are recalculated.

---

## 6. AI Situational Intelligence & Snapshot Versioning

When a situation is created or undergoes significant change, the [`SituationAnalysisService`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Infrastructure/Situations/SituationAnalysisService.cs) calls Azure OpenAI (`gpt-5-mini`) using the `SituationSummary_v1` prompt to generate structured output conforming to [`SituationAnalysisResult`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Application/Situations/SituationAnalysisResult.cs):
- `SituationTitle`
- `SituationSummary`
- `KeyObservations`
- `MainHazards`
- `OperationalConcerns`
- `RecommendedActions`
- `ConfidenceExplanation`

### Event-Driven Re-Analysis Triggers (`RequiresNewAIAnalysis`)
AI re-analysis is executed only when:
- Initial situation creation (`LastAIAnalysisAtUtc == null`)
- Significant incident count growth ($\Delta \ge 2$ incidents)
- Escalation level increases (e.g. `Elevated` $\to$ `High`)
- New casualties reported
- Operational time elapsed $\ge 2$ hours since last snapshot

Snapshots and AI outputs are immutably archived in [`OperationalSituationAnalyses`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Domain/Entities/OperationalSituationAnalysis.cs) for complete forensic auditability.

---

## 7. Command Center UI & State Machine

### State Machine Lifecycle
```
             ┌─────────────┐
             │    Open     │
             └──────┬──────┘
                    │ Acknowledge
                    ▼
             ┌─────────────┐
             │Acknowledged │
             └──────┬──────┘
       ┌────────────┴────────────┐
       │ Resolve                 │ Dismiss
       ▼                         ▼
┌─────────────┐           ┌─────────────┐
│  Resolved   │           │  Dismissed  │
└─────────────┘           └─────────────┘
```

1. **Command Center Grid** (`/Situations`):
   - Syncfusion DataGrid displaying open situations with frozen columns, sorting, filtering, and page navigation.
   - Interactive KPI cards:
     - **Active Situations** (Total open)
     - **Critical Situations** (Clickable filter: `?level=Critical`)
     - **Shortages Detected** (Clickable filter: `?resourceShortage=true`)
     - **Human Review Required** (Clickable filter: `?reviewRequired=true`)
   - Inline Quick "Acknowledge" button without leaving the grid.
2. **Situation Details View** (`/Situations/Details/{id}`):
   - Comprehensive situational overview, progress bar for score, AI intelligence card, and member incident list.
   - Quick navigation to `/Map?situationId={id}` for localized spatial view.
   - Operator actions: **Acknowledge**, **Review**, **Resolve**, **Dismiss**.
3. **SignalR Broadcasting**:
   - `SituationUpdated` event broadcast over `/hubs/incidents` to `DashboardOperators` group on create, expand, re-analyze, acknowledge, review, resolve, and dismiss events.
   - Client views consume live events and asynchronously fetch authoritative state via `/Situations/Snapshot` and `/Situations/MapSnapshot`.

---

## 8. Leaflet Live Map Layer & Visualization Envelope

The Leaflet map (`/Map`) features a dedicated operational situation awareness layer driven by [`emergency-situations-map.js`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Web/wwwroot/js/emergency-situations-map.js):

1. **Spatial Envelope Math** via [`SituationEnvelopeCalculator`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Application/Situations/SituationEnvelopeCalculator.cs):
   - **Centroid**: Mathematical average of member incident coordinates $(\bar{\phi}, \bar{\lambda})$.
   - **Radius**: Calculated using Haversine distance from centroid to the farthest member incident:
     $$\text{RadiusMeters} = \max(\text{maxDistanceKm} \times 1.20, 0.50) \times 1000$$
2. **Visual Styling**:
   - Dashed circle boundary (`dashArray: "8 6"`) with 12% fill opacity.
   - Color coded by escalation level:
     - Critical: `#dc3545`
     - High: `#fd7e14`
     - Elevated: `#ffc107`
     - Advisory: `#0dcaf0`
   - Custom pulse centroid marker with incident count badge.
3. **Layer Control**:
   - Interactive checkbox toggle on Map index view: `☑ Show operational situations`.
   - Independent layer group toggled without disturbing active incident markers or clusters.
4. **Popup Interactivity**:
   - Lists member incidents with direct clickable links to `/Incidents/Details/{id}`.
   - Displays resource shortage warnings, people affected, casualties, and composite score.
   - Links directly to Situation Details (`/Situations/Details/{id}`).
   - Displays mandatory disclaimer: *"Operational awareness grouping only. Not an evacuation zone or legal boundary."*

---

## 9. 3-Way Cross-System Navigation

The platform guarantees complete operator continuity across three distinct operational entry points:

1. **Dashboard $\to$ Situation $\to$ Incident**:
   - Main Executive Dashboard (`/Dashboard`) features high-level Situation KPI cards.
   - Clicking a card navigates with filtered query parameters (e.g. `/Situations?level=Critical`).
   - Command Center lists situations and links to `/Situations/Details/{id}`.
   - Situation Details lists all member incidents with links to `/Incidents/Details/{id}`.
2. **Map $\to$ Situation $\to$ Incident**:
   - Clicking a situation marker or circle on `/Map` opens the popup listing member incidents.
   - Operators can jump directly to any member incident or the situation details page.
3. **Incident $\to$ Situation**:
   - Incident Details view (`/Incidents/Details/{id}`) includes the **Part of Operational Situation** banner.
   - Shows parent situation number, title, level, and resource shortage alert.
   - Direct button: `View Situation Command Center →` routing back to `/Situations/Details/{id}`.
