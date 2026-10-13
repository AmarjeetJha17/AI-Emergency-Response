# Failed Deployment Runbook

Owner: Technical support lead/authorized deployment administrator. Status: Draft; staged rollback exercise pending.

## 1. Symptoms

New revision fails startup/readiness, image pull or Key Vault references fail, API/Web authentication/CORS breaks, SQL permissions/schema mismatch, missing consumer registration or errors rise after release.

## 2. Impact and severity

Critical if persistence, authorization, private access or shared state is unsafe. Otherwise classify by affected capability and dependency. A successful Bicep deployment or liveness probe alone is not application acceptance.

## 3. How to confirm the issue

Record release SHA/image digest, deployed revision, migration history and deployment result. Inspect sanitized Container Apps startup/revision logs, identity-based ACR pulls, runtime settings, Key Vault permissions and readiness. Verify actual Web-to-API and browser hub routes. Separate quota/provisioning failure from application failure.

## 4. Immediate containment

Stop promotion and rehearsal writes if correctness is unknown. Preserve previous working image tags, settings, schema/migration state and failed revision diagnostics. Do not shift traffic to an unready revision or create public SQL/storage access as a workaround. First deployment with no known-good revision stays NO-GO.

## 5. Authorized recovery steps

Support diagnoses and applies the minimal reviewed identity/configuration/image correction. For an application-only regression, restore a known-good immutable image/configuration using authorized Container Apps deployment controls after verifying database compatibility. For schema incompatibility/destruction, follow reviewed forward recovery or [restore](restore-and-recovery.md); an EF Down() or image rollback is not automatically safe. Run migration artifacts under the separate deployment identity, never runtime db_owner/startup migration.

## 6. Verification after recovery

Confirm intended images/digests and healthy revision, private runtime identity SQL/Blob/queue access, auth/CORS/SignalR, incident create/retrieve, resource commands, audit and degraded AI behavior. Check queue redelivery once and Web Data Protection/session behavior. Record any unreconciled prior requests or jobs.

## 7. Escalation contacts

Support/deployment lead → operations lead → pilot owner; security reviewer for access/secret concerns; data owner for schema loss. Names/backups in [pilot scope](../pilot-scope.md). See [staging deployment](../staging-deployment.md) for the approved sequence.

## 8. Audit and evidence requirements

Retain deployment/run IDs, immutable images/digests, revision traffic state, schema/migration IDs, sanitized startup failures, approvals, timestamps and rollback/forward-recovery result. Never copy resolved Key Vault secrets or connection strings into reports.

## 9. Conditions for returning to normal operation

Compatible revision/schema verified, affected critical scenarios retested and state/audit reconciled. The [staging report](../staging-verification.md) contains observed results; support signs verification and the owner approves resumption. No known-good recovery evidence means NO-GO.
