# Azure Resource Inventory

This is a planning inventory, not a provisioning record. None of these resources is claimed to exist. Azure names have different length, character, and global-uniqueness rules; verify every final name in the selected subscription before creating anything.

Environment boundaries and target roles are defined in the [environment matrix](environment-matrix.md).

## Staging examples

`centralindia` below is an example suffix, not a verified or selected region. The current Azure CLI session has no active login, so service availability, name availability, and pricing have not been checked.

| Resource | Example name | Purpose and notes |
|---|---|---|
| Resource group | `rg-er-staging-centralindia` | Isolate staging ownership, policy, tags, and costs |
| Container Apps environment | `cae-er-staging` | Shared managed environment for staging apps |
| Web Container App | `ca-er-web-staging` | Public HTTPS ingress for MVC/Web clients |
| API Container App | `ca-er-api-staging` | HTTPS REST and `/hubs/incidents`; public for the initial browser SignalR model |
| Worker Container App | `ca-er-worker-staging` | Persistent Service Bus consumer and background jobs; create after Worker host and durable queues exist |
| Container Registry | `acerstaging001` | Private application image storage; confirm global name availability |
| SQL server | `sql-er-staging-001` | Entra-authenticated Azure SQL logical server; restrict network access |
| SQL database | `sqldb-er-staging` | Synthetic staging data only |
| Storage account | `sterstaging001` | Private media, knowledge, and (if selected) Data Protection containers; confirm globally unique valid name |
| Service Bus namespace | `sb-er-staging-001` | Durable staging queues/topics isolated from pilot |
| Key Vault | `kv-er-staging-001` | Environment-specific secrets and key references; use managed identity access |
| Log Analytics workspace | `log-er-staging-001` | Central staging logs and Container Apps diagnostics |
| Application Insights | `appi-er-staging` | Application telemetry connected to the staging workspace, after exporter/integration is configured |
| Azure AI Search | `srch-er-staging-001` | Staging-only index and documents |
| AI Foundry/Azure OpenAI | `ai-er-staging-001` | Approved deployments, quotas, and cost controls for staging |
| Azure SignalR Service | `signalr-er-staging-001` | Optional; add only if staging scale-out tests establish a need |

## Pilot examples

Use an independent pilot resource group and independent stateful services, for example `rg-er-pilot-centralindia`, `cae-er-pilot`, `ca-er-web-pilot`, `ca-er-api-pilot`, `ca-er-worker-pilot`, `sqldb-er-pilot`, and `sb-er-pilot-001`. Final names must follow each resource provider's rules and must be checked for availability. Do not reuse staging databases, storage containers, Service Bus namespaces, AI evaluation indexes, secrets, or identities.

## Shared controls and tags

Apply resource tags consistently, such as `application=emergency-response`, `environment=staging|pilot`, `owner=<approved-team>`, and `cost-center=<approved-value>`. Avoid putting personal data, subscription secrets, or incident identifiers in tags. Configure an environment budget and cost alerts before deploying billable services. Record approved SKU, replica limits, retention, backup, network, and data residency decisions with the inventory before provisioning.

## Provisioning status

No Azure resources were created as part of Phase 17 Part 1. Subscription, region, SKU, name availability, and cost validation remain pending until an intended Azure subscription is available and authenticated.
