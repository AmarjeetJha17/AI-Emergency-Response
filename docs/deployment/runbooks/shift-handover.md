# Shift Handover Runbook

Owner: Operations lead. Status: Draft; operator handover rehearsal pending.

## 1. Symptoms

A session/shift ends, operator responsibility changes, unresolved critical incidents/proposals/resources remain, or the next operator cannot verify current state.

## 2. Impact and severity

Unassigned work, stale information and repeated commands can cause unsafe decisions. Critical incidents, active assignments, unacknowledged escalations and interrupted approvals require explicit ownership.

## 3. How to confirm the issue

Outgoing operator refreshes authorized incident/dashboard/resource/escalation/situation/notification views. Identify pending/failed AI, unresolved duplicate candidates, proposals not executed and degraded services. Incoming operator confirms access and compares the same authoritative records; a copied screenshot is insufficient if state changed.

## 4. Immediate containment

Pause new rehearsal commands while responsibilities transfer. Do not share passwords/tokens or assume a verbal instruction is formal action approval. Outside staffed sessions, submit no new test work; retain assigned pending work in the handover register.

## 5. Authorized recovery steps

Outgoing operator records each open item by ID, verified state/time, urgency, unresolved decision, pending job/proposal, current resource assignment and next responsible account. Incoming operator signs in independently, refreshes and acknowledges receipt. Operations lead resolves missing ownership/support and schedules follow-up. EmergencyAdmin/SystemAdmin completes supported approvals/resolutions through authenticated workflows; SystemAdmin reviews audit where needed.

## 6. Verification after recovery

Both operators compare current state and verify next-owner acknowledgment. Confirm no resource command/action was repeated, notifications are reconciled and degraded/pending work is labeled. If SignalR was interrupted, verify snapshot refresh; if authoritative refresh fails, stop dependent decisions.

## 7. Escalation contacts

Operations lead → pilot owner; support for stale/failed access or unverified state; security reviewer for improper access/shared credentials. Assign names and backups in [pilot scope](../pilot-scope.md).

## 8. Audit and evidence requirements

| Item/scenario ID | Verified state/time | Priority and unresolved decision | Pending job/action and next step | Outgoing/incoming account | Acknowledged time |
|---|---|---|---|---|---|
| Pending | Pending | Pending | Pending | Pending | Pending |

Link restricted application history/audit and incident/defect records. Use identifiers rather than sensitive narrative, media or credentials. Record start/end times, excluded workflows, support availability and outstanding corrective actions.

## 9. Conditions for returning to normal operation

Every critical open item has a verified state and assigned next owner, incoming role/access is correct, support is staffed and unresolved degraded workflows remain paused/excluded. Operations lead releases routine handover; a formal suspension still requires [owner resumption](../pilot-go-no-go.md#stop-and-resumption-authority).
