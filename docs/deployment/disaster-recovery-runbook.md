# Disaster Recovery Runbook

## 1. Detect

Confirm application health, SQL health, storage health, AI dependency status, queue status, and regional Azure status. Capture correlation IDs and timestamps.

## 2. Classify

Determine whether the event is an application failure, database failure, storage failure, dependency failure, regional outage, data corruption, or security incident.

## 3. Stabilize

- Stop or hold the affected deployment.
- Prevent repeated destructive operations.
- Preserve audit records and telemetry.
- Communicate the incident state through the organization's incident process.

## 4. Recover

Choose the smallest recovery action that addresses the failure: restart the application, replay queued work, restore SQL to a point in time, geo-restore, fail over a failover group, or recover storage versions/soft-deleted objects. Restrict recovery operations to designated administrators and deployment identities.

For Azure SQL, evaluate three recovery levels: point-in-time restore for accidental changes/corruption, geo-restore for regional recovery, and failover groups or geo-replication for a faster regional path with asynchronous replication considerations. Observe replication lag and test the selected failover path.

## 5. Verify

Confirm database connectivity and schema version, authentication, incident creation and retrieval, resource workflows, dashboards, SignalR reconnect, media access, and AI degraded mode. Check media references after database restoration.

## 6. Resume

Gradually restore AI, search, external context, and predictive workloads. Monitor queue age, retries, dead letters, and worker health.

## 7. Post-incident

Record root cause, data-loss estimate, measured RTO/RPO, failures, and corrective actions. Treat untested backups as unproven recovery mechanisms.

## Database deployment and rollback

Production application instances must not run EF migrations during startup. Generate a reviewed migration artifact in the deployment stage (for example, `dotnet ef migrations bundle`) or use a reviewed SQL script with the deployment identity. Keep runtime database access least-privileged and separate from schema-changing deployment access. Back up before forward migrations and smoke-test afterward. For destructive changes, use expand, deploy compatible code, migrate data, switch usage, then contract; a migration `Down()` is not assumed safe.

## Local DR drill

Use `scripts/disaster-recovery/verify-sql-backup.ps1`, `restore-test.ps1`, and `smoke-test.ps1` with explicitly named local databases. The restore script refuses to overwrite an existing database. Run the drill with realistic test data, paused and resumed workers, duplicate delivery, backup, restore to an isolated database, EF integrity checks, application smoke tests, and media-reference checks. Record StartedAt, DetectedAt, RecoveryStartedAt, ServiceRestoredAt, DataLossEstimate, RTO, RPO, failures, and corrective actions.
