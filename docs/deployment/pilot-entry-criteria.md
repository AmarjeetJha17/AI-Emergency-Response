# Pilot Entry Criteria

This checklist is for an authorized, limited pilot. A checked architecture item means the design is documented; operational items require evidence from the isolated staging/pilot deployment. A green CI run is not, by itself, pilot approval.

Use this with the [release checklist](release-checklist.md), [recovery objectives](recovery-objectives.md), [disaster-recovery runbook](disaster-recovery-runbook.md), and [failure matrix](failure-matrix.md).

The proposed bounded synthetic rehearsal is defined in [pilot scope](pilot-scope.md), with its [operator guide](operator-guide.md), [runbooks](runbooks/incident-triage.md), [tabletop exercise](tabletop-exercise.md), [success metrics](pilot-success-metrics.md), [monitoring requirements](monitoring-and-alerts.md), and [go/no-go evidence and stop authority](pilot-go-no-go.md). Current decision: **NO-GO**; actual staging/recovery/exercise evidence and named approvals are pending. A documented rehearsal exclusion does not satisfy or waive the full deployment requirements below. Record any approved bounded rehearsal exception explicitly in the decision record without waiving authorization, persistence, audit or recovery of required state.

## Architecture and environments

- [ ] Web, API, and dedicated Worker responsibilities and scale boundaries are documented and implemented without duplicate hosted workers in API replicas.
- [ ] Local, staging, and pilot environments have separate databases, storage, queues, identities, secrets, and AI/Search data.
- [ ] Worker processing uses durable cross-process messaging; queue age, retries, poison messages, and recovery are observable.
- [ ] Incident media and knowledge documents use private Azure Blob storage through authorized storage implementations.
- [ ] Azure region, service availability, data residency, network requirements, resource names, SKUs, and costs are reviewed for the selected subscription.
- [ ] Azure budgets and cost alerts are active before billable pilot resources are created.

## Identity, connectivity, and security

- [ ] Web, API, and Worker managed identities are assigned least-privilege service-specific access.
- [ ] Azure SQL uses the approved Microsoft Entra runtime identity; schema changes use a separate deployment identity.
- [ ] Blob containers are private; media and knowledge authorization is verified through application routes.
- [ ] Key Vault, JWT signing material, Syncfusion licensing, and any required provider credentials are supplied without committed secrets.
- [ ] Admin bootstrap/seeding behavior is reviewed and controlled for the pilot environment.
- [ ] HTTPS is enforced; API authorization and role restrictions are verified.
- [ ] API CORS allows only the deployed Web origins.
- [ ] Browser SignalR authentication, hub group membership, reconnect behavior, and API snapshot refresh are verified.
- [ ] The API's public ingress is reviewed and every externally reachable route is authorized; any future private-ingress change has separate Web/API and browser SignalR routing tests.

## Health, reliability, and observability

- [ ] Container Apps startup, liveness, and readiness probes are configured and tested independently.
- [ ] Readiness reflects required dependencies only; an AI provider outage does not block core incident intake when SQL and required application configuration are healthy.
- [ ] Worker health is exposed independently from API health and is tested during restart, scaling, and queue recovery.
- [ ] Staging uses the production-equivalent Service Bus topology; it does not silently fall back to in-memory messaging.
- [ ] SQL backup and restore and Blob recovery have been tested in an isolated environment; measured RTO/RPO are recorded.
- [ ] AI/Search/weather/storage dependency degradation and worker restart/queue redelivery are verified.
- [ ] Logs, traces, metrics, dashboards, and actionable alerts are available through the selected Azure Monitor/Application Insights setup.
- [ ] Data retention, classification, audit, and operational record access are approved.

## Pilot controls and approval

- [ ] Pilot user cohort, roles, support contacts, and authorized pilot owner are named.
- [ ] Synthetic staging data and approved pilot data are clearly separated.
- [ ] Operator incident escalation, manual fallback, and AI-unavailable procedures are documented and rehearsed.
- [ ] Known limitations, performance results, rollback/forward-recovery approach, and data retention commitments are documented.
- [ ] Release artifacts, database migration procedure, health checks, and staging smoke evidence are identified for the exact release.
- [ ] Authorized pilot owner approves entry after reviewing the evidence above.
