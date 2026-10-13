# Tabletop Exercise — Flash Flooding During Peak Traffic

Status: **PLANNED — NOT CONDUCTED**. Synthetic records only. Duration 45–60 minutes. Scheduled time, facilitator, observer, participants and build/environment: pending owner assignment.

## Prerequisites and schedule

Use the [six-account cohort](pilot-scope.md), approved runbooks, role/scope matrix, known limitations and a clean fabricated incident/resource dataset. Discussion may proceed while staging is blocked, but actual application observations remain unexecuted and cannot pass technical acceptance. An application-backed rehearsal requires passing staging gates, approved fault methods, staffed support and isolated data.

Minutes 0–5: goals, roles and stop procedure. Minutes 5–40: seven injects. Minutes 40–50: recovery/handover. Minutes 50–60: evidence review, defects and decisions. The operations lead facilitates; a separate assigned observer records actual responses. SystemAdmin gathers restricted audit evidence. EmergencyAdmin handles authorized simulated commands.

## Injects and expected outcomes

| Inject | What participants should see/do | Authority and safe continuation | Required state/evidence |
|---|---|---|---|
| 1. Three nearby similar reports | Open three persisted records; review duplicate candidates and distinct facts | Citizen reports; Operator reviews; no automatic merge | Three identifiers; candidate assessments; ownership/scope checks; no unintended combined record |
| 2. Ambiguous road-block image | Inspect authorized media; distinguish suggestion from verified fact | Operator requests human verification; no inference of trapped occupants solely from image | Private media access, confidence/uncertainty, original evidence and review note |
| 3. AI provider unavailable | Pending/failed/degraded analysis; no fabricated completion | Core incident create/retrieve continues only if SQL is healthy; manual review | Persisted incident, retained/retryable durable job, failure category, recovery outcome |
| 4. Operators assign the same resource | One valid assignment; competing/stale action conflicts or revalidates safely | Two Operators refresh/reconcile; administrator dispatch remains separate | Current assignment, versions/history; no silent overwrite or duplicate assignment effect |
| 5. Unauthorized approval attempt | Denial with no execution; conversational approval also has no effect | Operator may propose; only authorized EmergencyAdmin/SystemAdmin approves | Denied attempt plus zero state effects; later authorized approval revalidates and executes once |
| 6. SignalR disconnect during escalation change | Degraded/stale connection; no reliance on missed notification | Authorized actor changes escalation; disconnected Operator refreshes snapshot after reconnect | DB escalation state/history; correct group auth; snapshot converges to authoritative state |
| 7. Service returns | Verify incident/resource/escalation/proposal/job state before resuming | Support performs verification; owner authorizes resumption with required sign-offs | No missing required state, duplicate effects or audit gaps; outstanding work handed over |

For every inject answer: what was visible, which actor/action was permitted, what continued safely, which audit/notification records were required, and how authoritative state was established. Predictions remain advisory throughout.

## Results sheet

For each row use ISO-8601 timestamps and a restricted evidence link. “Discussed” is different from “observed in staging”; a technical pass requires the latter.

| Inject | Evidence type | Expected vs actual | Start/detected/recovery-start/verified-restored times | Incorrect assumptions/defects | Corrective owner/date/retest | Result |
|---|---|---|---|---|---|---|
| 1 | Pending | Not observed | Pending | Pending | Pending | Not executed |
| 2 | Pending | Not observed | Pending | Pending | Pending | Not executed |
| 3 | Pending | Not observed | Pending | Pending | Pending | Not executed |
| 4 | Pending | Not observed | Pending | Pending | Pending | Not executed |
| 5 | Pending | Not observed | Pending | Pending | Pending | Not executed |
| 6 | Pending | Not observed | Pending | Pending | Pending | Not executed |
| 7 | Pending | Not observed | Pending | Pending | Pending | Not executed |

Record scenario IDs, build/images, dataset, actor roles, relevant request/job/proposal IDs, correlated audit, notification recipients and deduplication, snapshots and sanitized diagnostic output. Never include passwords, tokens, cookies, full confidential prompts or personal media.

## Acceptance and follow-up

Pass operational discussion only when participants correctly identify all critical permissions, stop boundaries and fallback procedures. Record discussion-only results separately. Technical injects pass only with correct persistence/scope, denied unauthorized action, explicit degraded state, safe concurrency, authoritative reconnect recovery and complete required audit.

Any bypass, data exposure, false save, silent overwrite or unrecoverable required state fails the exercise and triggers [stop rules](pilot-go-no-go.md#stop-and-resumption-authority). Missing evidence remains pending. The exercise does not substitute for automated safety tests or actual SQL/Blob restore and queue recovery drills.

Debrief owner/date, participant acknowledgments, security review, open blockers and review date: pending. Add every corrective action to the evidence register; retest the affected inject and dependent workflows before approval.
