# Restore and Recovery Runbook

Owner: SystemAdmin/authorized database deployment administrator with technical support lead. Status: Draft; actual staging SQL/Blob restore and recovery measurements pending.

## 1. Symptoms

Data corruption/loss, incorrect schema change, unavailable regional services or failed operational state recovery requires restoration rather than ordinary retry.

## 2. Impact and severity

Critical for incident, resource, identity and audit state. Restoring SQL without media, audit and queue reconciliation can produce inconsistent references or duplicate effects. Preserve the affected database/storage before deciding a recovery point.

## 3. How to confirm the issue

Establish incident start, newest reliably intact required state, available SQL restore points/backups, Blob versions/soft-delete state, migration history and region/network availability. Verify mechanisms actually configured; geo-replication/failover groups in architecture guidance are not assumed provisioned. Use [recovery objectives](../recovery-objectives.md) and [DR runbook](../disaster-recovery-runbook.md).

## 4. Immediate containment

Suspend writes and consumer replay; preserve audit, original datasets, queue/outbox state and affected versions. Agree recovery point/data-loss tolerance with pilot/data owner. Do not overwrite a developer, production or existing staging database in an automated drill. Never use an unverified backup as evidence that recovery will succeed.

## 5. Authorized recovery steps

Authorized SystemAdmin/deployment identity restores SQL to a new isolated target and verifies schema/integrity before controlled connection cutover. Recover required Blob versions/objects privately and reconcile database media/knowledge references. Preserve/reconcile audit and identity access. Reconcile outbox and broker state against restored execution IDs before resuming consumers, because a restore can reintroduce previously executed jobs.

The local scripts [backup verification](../../../scripts/disaster-recovery/verify-sql-backup.ps1), [isolated restore](../../../scripts/disaster-recovery/restore-test.ps1), and [smoke checks](../../../scripts/disaster-recovery/smoke-test.ps1) support local drills only; they do not establish Azure PITR/Blob recovery. Use the approved Azure restore/deployment process from the connected private network under designated identities. Any cutover/retention/destructive cleanup requires the approved recovery decision.

## 6. Verification after recovery

Check schema/migrations, runtime identity permissions, incident/status/escalation/situation/resource relationships, audit continuity, media access and knowledge references. Run authenticated and denied-scope cases, synthetic create/retrieve, resource concurrency, Copilot approval/replay and durable queue recovery. Reconnect browsers and refresh snapshots. Document recovered versus missing records and duplicate side effects; any unreconciled required state prevents resumption.

## 7. Escalation contacts

SystemAdmin/support → operations lead → pilot owner; data/organization owner approves data-loss decisions; security reviewer validates identity/audit/exposure consequences. Names, channel and backups in [pilot scope](../pilot-scope.md) must be assigned before a drill.

## 8. Audit and evidence requirements

Record outage start, detection, recovery start, verified service-restored time, recovery point, data-loss estimate, measured RTO/RPO, original/restored target identifiers, Blob versions, migration IDs, queue reconciliation, approvals and tests. Targets are RTO ≤60 minutes and RPO ≤15 minutes, not achieved claims. Store credentials and sensitive contents outside reports in approved secret/evidence systems.

## 9. Conditions for returning to normal operation

Restored state/integrity/private access verified, audit/media/jobs reconciled, actual loss understood and required tests pass. Technical/data/security reviewers sign relevant findings; only the pilot owner approves [resumption](../pilot-go-no-go.md#stop-and-resumption-authority). Resume consumers gradually and monitor backlog. Retain the original target until authorized cleanup.
