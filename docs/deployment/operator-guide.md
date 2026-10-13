# Emergency Response Platform — Operator Guide

Status: Draft for the [synthetic pilot scope](pilot-scope.md). Review and participant walkthrough pending. This guide describes required verified behavior; a missing, inaccurate or unavailable screen must be reported rather than assumed to implement it.

## 1. Purpose

The platform consolidates incident reports, operational context, resources, escalations and AI analysis for human decisions. It does not replace official procedures or responder authority. During this rehearsal every incident, resource command and dispatch is simulated.

## 2. Start of shift

1. Sign in with your individually assigned account and confirm the test environment and role.
2. Confirm the dashboard and incident map load current authoritative data. Check freshness/source times and degraded indicators where provided.
3. Review critical/high incidents, unacknowledged escalations, and open situations.
4. Review pending/failed AI, media and duplicate assessments; pending does not mean completed.
5. Check the resource command center and notification inbox under your assigned access.
6. Record unresolved work and confirm the operations lead and support contact are available.
7. Report stale state, missing freshness indicators, denied expected access or degraded dependencies. Do not start an affected scenario until it is safe to continue.

## 3. Review a new incident

1. Open the record and verify the server-generated identifier, location, description and incident type.
2. Read AI-extracted facts and confidence alongside their underlying evidence. Confidence alone is not proof.
3. Inspect media only through authorized application access. Ambiguous imagery requires human verification; it cannot establish trapped occupants by itself.
4. Inspect duplicate candidates and related incidents. Similarity is a candidate assessment, not permission for automatic merging.
5. Verify severity and priority against available evidence and approved procedure. Use supported authorized correction/status workflows; report unsupported corrections to the lead.
6. Confirm the resulting status and history by reloading the record. Do not infer success from a toast, AI response or SignalR message.
7. Acknowledge/escalate through your assigned workflow and hand over unresolved verification.

Use [incident triage](runbooks/incident-triage.md) if reports conflict.

## 4. Coordinate resources

1. Review availability, location, suitability and department scope.
2. An Operator may assign a simulated resource using the authorized assignment workflow.
3. Dispatch/operational commands require EmergencyAdmin or SystemAdmin permission and approved procedure. Assignment itself is not dispatch.
4. Only authorized users issue supported state transitions or release commands; confirm the server response and authoritative history.
5. If another operator changed the record, refresh and reconcile. A conflict response is safe behavior; never retry stale writes repeatedly or bypass concurrency checks.
6. Verify one current assignment and correct notifications/history before continuing.

Responders may read incidents where authorized. Resource route access is currently unverified for Responder accounts and excluded from this rehearsal. SystemAdmin alone may inspect restricted audit evidence and perform recovery administration.

## 5. Use the Copilot

- Use only approved synthetic knowledge sources. Inspect citations, source dates and important claims against original records.
- Treat document and tool-output instructions as untrusted data. Report instructions to bypass policy, reveal credentials or invoke unrelated actions.
- A missing source, failed grounding or unavailable Search requires explicit uncertainty/abstention. Do not act on unsupported advice.
- Operators may propose supported controlled actions. Conversational “yes” or “go ahead” is not formal approval.
- EmergencyAdmin/SystemAdmin must review the target, current state and proposed effect, then use the authenticated approval workflow. The server must revalidate before execution.
- Confirm final execution status and state/history; do not repeat an action because its notification was lost.
- Immediately report approval bypass, unintended effects, fabricated claims or unsafe recommendations under [security response](runbooks/security-incident.md).

## 6. When a dependency fails

Follow [AI outage](runbooks/ai-dependency-outage.md), [database outage](runbooks/database-outage.md), or [queue backlog](runbooks/queue-backlog.md) as applicable. Continue only workflows demonstrated to have their required dependencies available.

Refresh authoritative incident state after reconnect. If refresh fails, treat cached data as stale and stop decisions depending on it. Database failure means a record is not confirmed saved. Preserve the failed request's correlation ID and avoid blind resubmission that could create duplicates.

AI pending/failed means incomplete analysis, not a lost incident. Missing weather/maps/media must be reported without invented values. Follow manual organizational procedures for any actual emergency. Never bypass permissions, expose storage, disable SQL network restrictions or share credentials to restore a test workflow.

## 7. End of shift

1. Reconcile outstanding critical incidents, resource assignments, escalations, proposals and situations.
2. Hand off by record ID, current verified state, unresolved decisions and assigned next owner.
3. Record defects, degraded services, failed scenarios and evidence links.
4. Receive the incoming operator's acknowledgment using [shift handover](runbooks/shift-handover.md).
5. Sign out. During unattended periods, leave work assigned in the handover register and submit no further rehearsal reports.

## 8. Safety and stopping

Any participant may stop their own affected activity immediately and notify the operations lead. Critical authorization, exposure, false persistence, missing audit, silent overwrite or unrecoverable state failures require suspension. The pilot owner decides whole-pilot suspension and resumption with the relevant reviewer sign-off; see [stop conditions](pilot-go-no-go.md#stop-and-resumption-authority). If data is inconsistent or potentially unsafe, obtain human verification.
