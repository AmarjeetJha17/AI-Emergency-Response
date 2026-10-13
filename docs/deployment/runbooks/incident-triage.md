# Incident Triage Runbook

Owner: Operations lead. Status: Draft; operator/security review and rehearsal pending. Synthetic incidents only under [pilot scope](../pilot-scope.md).

## 1. Symptoms

A new critical/high report, conflicting facts/location, ambiguous image, duplicate candidates, unexpected severity/priority or incomplete AI assessment needs review.

## 2. Impact and severity

Urgent manual review for possible critical harm in the scenario. Inconsistent data or uncertain evidence can lead to incorrect coordination; AI confidence does not establish fact. Suspected exposure or unauthorized action invokes [security response](security-incident.md).

## 3. How to confirm the issue

Open the authorized incident record, verify its identifier, location, report time, type and description. Compare AI/visual output to evidence, related reports and available source timestamps. Refresh authoritative state; inspect supported history. Use SystemAdmin for restricted audit inspection. Do not treat a duplicate candidate as a merged incident.

## 4. Immediate containment

Pause commands depending on disputed facts. Record what needs verification and notify the operations lead. Keep reports distinct while reviewing duplicates. Use organizational manual procedures for an actual emergency outside this synthetic rehearsal.

## 5. Authorized recovery steps

Operator reviews and uses permitted incident updates/acknowledgment workflows. Confirm any correction or status change against current state. Operator may assign a simulated resource; EmergencyAdmin/SystemAdmin performs authorized dispatch/resolve/approval. If a needed correction is unsupported, record a defect and seek the lead's review; do not alter SQL directly or add a role to bypass the interface.

## 6. Verification after recovery

Reload incident, assignment, escalation and situation records; check evidence consistency and history. Confirm expected notification recipients and no unintended duplicate effects. Verify subsequent action uses current facts and permitted scope.

## 7. Escalation contacts

Operations lead → pilot owner; support lead for defects; security reviewer for scope/safety violations. Names, channel and backups must be assigned in [scope ownership](../pilot-scope.md#participants-and-accountability). Missing staffed support prevents continuation.

## 8. Audit and evidence requirements

Record incident/scenario IDs, uncertain versus verified facts, actor role, decision, timestamps, correlated request/history and notification evidence. SystemAdmin checks required audit. Exclude personal narratives, media contents and credentials from operational evidence.

## 9. Conditions for returning to normal operation

Relevant facts verified or clearly marked uncertain, current state reconciled, authorized workflow confirmed and outstanding decisions handed over. The operations lead releases a local triage hold; any formal pilot suspension follows [owner-approved resumption](../pilot-go-no-go.md#stop-and-resumption-authority).
