# Staging Deployment Verification

## Release Identity

- Git commit: Not assigned; the staging source changes are uncommitted.
- Web image tag: Local smoke image `emergencyresponse-web:phase17-staging-check`; not pushed to ACR. This is a local check tag, not a release tag.
- API image tag: Local build image `emergencyresponse-api:phase17-staging-check`; not pushed to ACR. This is a local check tag, not a release tag.
- Worker image tag: Not applicable; Worker deployment is deferred.
- Migration version: Linux self-contained EF bundle generated locally at `artifacts/staging/efbundle`; not applied to Azure SQL.
- Search index provisioner: Linux self-contained tool generated locally at `artifacts/staging/search-index-provisioner/`; not run against Azure AI Search.
- Source commit: No immutable release SHA assigned because deployment changes are still uncommitted.

## Infrastructure

- Resource group: `rg-er-staging-eastus` is the target; provisioning is pending.
- Container Apps environment: `cae-er-staging-vnet` is defined in Bicep; provisioning is pending.
- SQL private endpoint: Defined in Bicep; approval and private DNS tests are pending.
- Blob Storage: Private containers and Blob private endpoint are defined; provisioning is pending.
- Service Bus: Namespace and four durable queues are defined; provisioning is pending.
- Azure AI Search: Separate Basic staging service and private endpoint are defined; provisioning and index setup are pending.

## Health and Networking

- Web reachable: Local container `/health/live` smoke check returned HTTP 200 on port 18080; Azure staging URL not tested.
- API liveness/readiness: Not tested; no staging deployment.
- Worker healthy: Not applicable; no dedicated Worker host exists.
- SQL private connectivity and managed identity: Not tested.
- SignalR connect/reconnect and snapshot refresh: Not tested.
- Key Vault references and secret-free logs: Not tested.

## Functional Verification

All functional staging checks remain pending: synthetic Citizen incident submission, authorized Operator retrieval, AI processing/degraded mode, duplicate detection, private media, resource assignment and concurrency, escalation, situation command center, notifications, grounded Copilot retrieval, approved/audited action, and SignalR reconnect.

## Security and Release Result

- Data: Synthetic staging data only; no staging application has been deployed.
- Managed identity/RBAC: Bicep definitions added; runtime identity authorization has not been exercised.
- Migrations: Separate Linux EF bundle and private runner workflow are defined; the bundle was generated locally but has not been applied.
- Rollback: Previous image/database state is not applicable until a first staging release exists.
- Result: **Blocked before provisioning.** On 2026-10-14, provider-level `what-if` again returned `MaxNumberOfGlobalEnvironmentsInSubExceeded`: the subscription allows one Container Apps environment. The apps in `rg-reachinbox` have been deleted, but `reachinbox-env` remains in `ScheduledForDelete` state. Azure activity logs show the environment deletion was accepted at 2026-10-13T20:54:58Z; completion has not yet been observed. No staging deployment was submitted. Wait for the environment deletion and quota release, then rerun the provider-level preview. Request a quota increase or Azure support if deletion remains pending. Template compilation is not a deployment or runtime verification.
- Local validation: Release solution build, Bicep compilation for the foundation/apps/preview/migration-runner templates, Web/API Docker image builds, non-root image configuration inspection, and the Web container liveness smoke check completed. The generated Linux migration bundle and Search index provisioner are ignored build artifacts, not release evidence.
- Known limitation: Several application job paths remain process-local. The API is capped at one staging replica and the Worker remains deferred; queue durability and restart recovery still need explicit verification.

Update this report with a commit SHA, image tags, actual Azure resource IDs, deployment/test run links, observed results, corrective actions and reviewer approval only after those actions occur. Do not mark pending checks as passed based on template compilation or container startup alone.
