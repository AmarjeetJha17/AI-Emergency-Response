# Incident Escalation & Situation Awareness Architecture

## 1. Overview & Operational Principles

The Escalation Engine in the AI Emergency Response platform provides deterministic, auditable, and transparent situation awareness. When emergency incidents develop, critical factors such as visual hazard detections, reported casualty counts, sudden severity shifts, or resource exhaustion can quickly overwhelm standard workflows.

The engine functions strictly as an **advisory decision-support system**.

### The Advisory Boundary
- **What the Engine Does**:
  - Automatically evaluates incident state, AI analyses, visual detections, duplicate report volumes, and available department resources.
  - Generates structured, immutable `IncidentEscalation` records that cite specific evidence, trigger reasons, and suggested priority levels (`Advisory`, `Elevated`, `High`, `Critical`).
  - Broadcasts real-time SignalR notifications (`EscalationUpdated`) to operators and updates dashboard situation awareness counters.
  - Maintains a complete historical audit trail of each alert from opening through operator acknowledgment and resolution.
- **What the Engine DOES NOT Do**:
  - **No Automatic Dispatch**: Emergency units are never dispatched automatically by the engine. Resource dispatch requires explicit human operator action.
  - **No Unilateral Severity Alteration**: The engine raises an advisory escalation alert rather than mutating the primary incident severity behind the scenes.
  - **No Public Warning Broadcasts**: No external broadcast is sent without operator confirmation.
  - **No Automatic Incident Closure or Rejection**: All terminal actions remain exclusively within operator command.

---

## 2. Deterministic Trigger Rules

The `EscalationEngine` is pure, testable, and deterministic without nondeterministic LLM side-effects:

| Escalation Type | Level | Evaluation Criteria |
| :--- | :--- | :--- |
| `SeverityIncrease` | `High` | Previous severity score < Current severity score, or severity score &ge; 80. |
| `PeopleTrapped` | `Critical` | AI visual detection or NLP entity extraction flags trapped civilians. |
| `ImmediateDanger` | `Critical` | Visual evidence or verified description notes active fire, collapsing structure, or flash flood threat to life. |
| `CriticalInfrastructure` | `High` | Critical infrastructure affected (hospital, power station, primary bridge, school, water facility). |
| `MassCasualtyRisk` | `Critical` | Reported or verified casualty count &ge; 5. |
| `IncidentCluster` | `High` | Cluster of 3 or more active emergency incidents detected within a 1-kilometer radius. |
| `DuplicateCluster` | `Elevated` | Incident receives 3 or more duplicate citizen reports within an operational window, indicating high public visibility. |
| `ResourceShortage` | `High` | Required units of an essential resource type exceed the count of eligible, available units. |

---

## 3. Anti-Spam & Deduplication Model

To prevent alert fatigue and operator distraction, the platform enforces strict deduplication:

1. **Unique Active Condition**:
   - For any given incident (`IncidentId`), there can be **at most one** `Open` escalation of each `EscalationType`.
2. **Evaluation Skipping**:
   - When an event triggers an escalation evaluation, the engine queries existing active escalations for the incident. If an `Open` alert of that `Type` already exists, a duplicate alert is not inserted.
3. **Re-Evaluation After Terminal States**:
   - Once an escalation is `Resolved` or `Dismissed`, subsequent new events with renewed danger can trigger a new `Open` alert if conditions warrant.

---

## 4. State Machine & Operator Lifecycle

Escalations follow an explicit finite state machine validated by `EscalationStatusRules`:

```
                 ┌───────────────┐
                 │     Open      │
                 └───────┬───────┘
                         │
             ┌───────────┴───────────┐
             │                       │
             ▼                       ▼
     ┌───────────────┐       ┌───────────────┐
     │ Acknowledged  │       │   Dismissed   │ (Terminal)
     └───────┬───────┘       └───────────────┘
             │                       ▲
             ├───────────────────────┘
             ▼
     ┌───────────────┐
     │   Resolved    │ (Terminal)
     └───────────────┘
```

- **Open**: Newly generated alert requiring review.
- **Acknowledged**: Operator has reviewed the escalation, recording optional operational notes.
- **Resolved**: Incident condition mitigated (e.g. required resources arrived on scene, hazards cleared).
- **Dismissed**: Operator evaluated the alert as false alarm or duplicate context, recording mandatory/advisory rationale.
- **Terminal Invariance**: Transitions out of `Resolved` or `Dismissed` are illegal and rejected.

---

## 5. System Integration & Real-Time Flow

1. **Event Ingestion**:
   - Post-AI Analysis: `IncidentAIJobProcessor` triggers evaluation when visual or NLP processing finishes.
   - Resource Updates: `ResourceService` triggers evaluation when units are assigned or status changes.
2. **Authoritative Persistence**:
   - Created in `ApplicationDbContext.IncidentEscalations` with foreign key to `Incident` and indexes on `(IncidentId, Status)` and `(Status, Level)`.
   - Logged in `AuditLogs` for regulatory compliance and post-incident review.
3. **Real-Time Notification**:
   - Dispatched via `IIncidentNotificationService.NotifyEscalationUpdatedAsync` to the `/hubs/incidents` SignalR hub.
   - Triggers dashboard metric updates (`OpenEscalations`, `CriticalEscalations`, `UnacknowledgedEscalations`) and updates the incident command center in real time.
