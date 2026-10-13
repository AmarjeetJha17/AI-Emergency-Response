# Database Outage Runbook

Owner: Technical support lead/SystemAdmin. Status: Draft; private runtime identity/connectivity and staged outage checks pending.

## 1. Symptoms

Readiness fails, incident/read/write requests fail or time out, SQL connection errors appear, or state cannot be refreshed. A UI success message without persisted state is a critical correctness failure.

## 2. Impact and severity

Critical for every workflow requiring authoritative records. Stop new state-changing rehearsal commands until persistence can be verified. Cached dashboard/SignalR payloads cannot establish current state.

## 3. How to confirm the issue

Check API liveness and readiness separately, SQL service status and sanitized correlated errors. From an approved VNet-connected workload verify DNS resolves the normal SQL FQDN to the private endpoint, TCP reachability, Entra identity/user and required database permissions. An administrator's successful connection does not establish runtime identity access.

## 4. Immediate containment

Pause writes and decisions based on stale records; preserve failure/request IDs and notify the owner. Avoid repeated submissions, consumer replay and deployment changes that could compound failures. Never say a failed transaction saved an incident. Do not enable public SQL, grant runtime db_owner, share credentials or restart repeatedly without diagnosis.

## 5. Authorized recovery steps

Support checks Azure/private endpoint/DNS and network configuration, runtime identity and contained-user grants, capacity/locks/timeouts and schema compatibility. Apply reviewed minimal correction through authorized deployment identities. If corruption/data loss is suspected, follow [restore and recovery](restore-and-recovery.md) instead of destructive repair. Schema changes remain separate from API startup.

## 6. Verification after recovery

Using runtime identity, confirm database name/user, required reads/writes and schema. Submit/retrieve one synthetic incident and verify server ID/history; reconcile uncertain requests before any resubmission. Check assignments, proposals/audit and durable jobs for partial effects. Verify readiness, unauthorized denial and browser snapshot refresh.

## 7. Escalation contacts

Support/SystemAdmin → operations lead → pilot owner. Include security/data owner for suspected loss/exposure or unauthorized permissions. Names and backups are pending in [pilot scope](../pilot-scope.md).

## 8. Audit and evidence requirements

Record UTC timestamps, correlation IDs, failed versus committed requests, SQL/DNS/identity diagnostics, schema/revision identifiers, recovery changes and verification outcomes. Preserve audit and estimate affected state/data loss. Exclude connection strings, access tokens and request payloads.

## 9. Conditions for returning to normal operation

Required state and uncertain requests reconciled; runtime permissions and writes/reads verified; critical audit intact; readiness stable; no false-success behavior. Technical support records verification, relevant reviewers sign findings, and the pilot owner approves resumption under [stop rules](../pilot-go-no-go.md#stop-and-resumption-authority).
