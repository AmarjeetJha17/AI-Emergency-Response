# Azure Resource Inventory

This is a planning inventory, not a provisioning record. None of these resources is claimed to exist. Azure names have different length, character, and global-uniqueness rules; verify every final name in the selected subscription before creating anything.

Environment boundaries and target roles are defined in the [environment matrix](environment-matrix.md).

## Bicep foundation resources

`infra/main.bicep` generates names from the resource-group ID and the `staging`/`pilot` environment parameter. The table below describes the resources actually declared in that template. `centralindia` examples elsewhere are not a verified or selected region. The current Azure CLI session has no active login, so service availability, name availability, and pricing have not been checked.

| Bicep resource | Purpose | Intended identity access | Environment |
|---|---|---|---|
| Log Analytics workspace | Container Apps platform log destination; 30-day retention in the template | Container Apps environment writes platform logs; operations users read under Azure RBAC | Staging or pilot, separate instance per resource group |
| Container Apps environment | Managed runtime boundary for later Web/API/Worker apps | Platform-managed identity integration; app identities assigned separately when apps are added | Staging or pilot |
| Azure Container Registry | Stores versioned application images; admin account is disabled | Later app user-assigned or system-assigned identity gets `AcrPull`; CI identity gets narrowly scoped push permission | Staging or pilot |
| Storage account | Hosts private incident-media and knowledge-document containers; anonymous Blob and shared-key access are disabled | API/Worker runtime identities receive only required Blob data roles after implementation; no app role assignment exists yet | Staging or pilot |
| `incident-media` Blob container | Private incident uploads | API identity for authorized media operations; Worker only if processing requires retrieval | Staging or pilot |
| `knowledge-documents` Blob container | Private source knowledge documents | API/Worker identity only after Blob storage adapters and authorization are implemented | Staging or pilot |
| Key Vault | Stores environment secrets/keys with RBAC authorization and purge protection | Each workload identity receives only the specific secret/key access it needs; assignments are not in this foundation | Staging or pilot |
| Service Bus namespace | Durable message transport; local authentication is disabled | API identity sends; future Worker identity receives; role assignments are not in this foundation | Staging or pilot |
| `incident-ai`, `visual-analysis`, `duplicate-detection`, `external-context` queues | Durable queues matching current API publisher/consumer route names | API sender and future Worker receiver, scoped to required queues | Staging or pilot |

All four queues are included because the current Azure Service Bus publisher and consumer map to those names. This does not mean every application job is durable today; see the Worker deferral below.

## Target resources not declared in this foundation

| Resource | Purpose and access plan | Status |
|---|---|---|
| Resource group, e.g. `rg-er-staging-centralindia` | Isolate staging policy, ownership, and cost; apply approved tags and budget | Create only after subscription/region/cost review |
| Web Container App, e.g. `ca-er-web-staging` | Public HTTPS MVC/Web entry point; configure API URL and Web secret settings | Not deployed; container image only is being prepared |
| API Container App, e.g. `ca-er-api-staging` | REST and SignalR endpoint; public HTTPS for the current browser SignalR connection model | Not deployed; configure SQL, externalized CORS origins, health probes, and identity first |
| Worker Container App, e.g. `ca-er-worker-staging` | Dedicated persistent queue consumer | Deferred until all job producers/consumers use a verified durable transport and a Worker host exists |
| Azure SQL server/database, e.g. `sql-er-staging-001` / `sqldb-er-staging` | Operational records, identity, and audit using Entra authentication | Not declared; design private network and migration identity before provisioning |
| Application Insights | Application telemetry and alerts | Not declared; current app exporters and Azure Monitor integration need configuration |
| Azure AI Search and Foundry | Retrieval and model workloads with environment-specific quotas and cost limits | Not declared |
| Azure SignalR Service | Optional SignalR scale-out | Not declared; evaluate after staging topology/load testing |

## Pilot examples

Use an independent pilot resource group and independent stateful services, for example `rg-er-pilot-centralindia`, `cae-er-pilot`, `ca-er-web-pilot`, `ca-er-api-pilot`, `ca-er-worker-pilot`, `sqldb-er-pilot`, and `sb-er-pilot-001`. Final names must follow each resource provider's rules and must be checked for availability. Do not reuse staging databases, storage containers, Service Bus namespaces, AI evaluation indexes, secrets, or identities.

## Shared controls and tags

The Bicep foundation applies `project`, `environment`, and `managedBy` tags. Add approved owner and cost-center tags before deployment. Avoid putting personal data, subscription secrets, or incident identifiers in tags. Configure an environment budget and cost alerts before deploying billable services. Record approved SKU, replica limits, retention, backup, network, and data residency decisions with the inventory before provisioning.

## Provisioning status

No Azure resources were created as part of Phase 17 Parts 1 or 2. The Bicep template is a local definition only. Subscription, region, SKU, name availability, role assignments, and cost validation remain pending until an intended Azure subscription is available and authenticated.
