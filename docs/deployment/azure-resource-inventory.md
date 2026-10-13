# Azure Resource Inventory

This is an infrastructure and status inventory, not a provisioning record. The Bicep resources below are definitions and preview targets; they are not claims that those resources exist. Azure names have different length, character, and global-uniqueness rules; verify availability before deploying.

Environment boundaries and target roles are defined in the [environment matrix](environment-matrix.md).

## Bicep foundation resources

`infra/main.bicep` generates names from the resource-group ID and the `staging`/`pilot` environment parameter. `infra/staging-preview.bicep` wraps the foundation in a subscription-scope preview for `rg-er-staging-eastus`, with East US as the preview region. The preview script uses the active CLI subscription and signed-in Entra user without writing their identifiers into tracked files. Region availability, network overlap, global name availability, and pricing still require review before deployment.

| Bicep resource | Purpose | Intended identity access | Environment |
|---|---|---|---|
| Log Analytics workspace | Container Apps platform log destination; 30-day retention in the template | Container Apps environment writes platform logs; operations users read under Azure RBAC | Staging or pilot, separate instance per resource group |
| Virtual network and subnets | Isolate Container Apps infrastructure and private endpoints; `10.40.0.0/16`, ACA `/23`, private-endpoint `/24` | VNet control-plane access is administrative; subnet access is consumed by ACA and Private Link | Staging or pilot; CIDRs require overlap review |
| Container Apps environment | Replacement VNet-integrated environment `cae-er-staging-vnet`, externally reachable at environment level | Platform-managed identity integration; app identities are configured with app resources later | Staging preview target |
| Azure Container Registry | Stores versioned application images; admin account is disabled | Later app user-assigned or system-assigned identity gets `AcrPull`; CI identity gets narrowly scoped push permission | Staging or pilot |
| ACR pull identity and role assignment | Dedicated user-assigned identity with `AcrPull` scoped to this registry | Attached to app registry configuration when versioned images and app resources are added | Staging or pilot |
| Storage account | Hosts private incident-media and knowledge-document containers; anonymous Blob and shared-key access are disabled | API/Worker runtime identities receive only required Blob data roles after implementation; no app role assignment exists yet | Staging or pilot |
| `incident-media` Blob container | Private incident uploads | API identity for authorized media operations; Worker only if processing requires retrieval | Staging or pilot |
| `knowledge-documents` Blob container | Private source knowledge documents | API/Worker identity only after Blob storage adapters and authorization are implemented | Staging or pilot |
| Key Vault | Stores environment secrets/keys with RBAC authorization and purge protection | Each workload identity receives only the specific secret/key access it needs; assignments are not in this foundation | Staging or pilot |
| Service Bus namespace | Durable message transport; local authentication is disabled | API identity sends; future Worker identity receives; role assignments are not in this foundation | Staging or pilot |
| `incident-ai`, `visual-analysis`, `duplicate-detection`, `external-context` queues | Durable queues matching current API publisher/consumer route names | API sender and future Worker receiver, scoped to required queues | Staging or pilot |
| Azure SQL logical server and database | Operational records, identity, and audit; Entra-only auth and public network access disabled | Entra administrator for setup; future API/Worker contained users get minimum data roles, not `db_owner` | Staging or pilot |
| SQL private DNS zone/link, private endpoint, and zone group | Resolve the normal SQL FQDN to its private endpoint for VNet workloads | API and future Worker use normal SQL FQDN through private DNS | Staging or pilot |

All four queues are included because the current Azure Service Bus publisher and consumer map to those names. This does not mean every application job is durable today; see the Worker deferral below.

## Target resources not declared in this foundation

| Resource | Purpose and access plan | Status |
|---|---|---|
| Resource group `rg-er-staging-eastus` | Isolate staging policy, ownership, and cost; wrapper includes it in subscription-scope `what-if` | Preview only; not created by the what-if |
| Web Container App, e.g. `ca-er-web-staging` | Public HTTPS MVC/Web entry point; configure API URL and Web secret settings | Not deployed; container image only is being prepared |
| API Container App, e.g. `ca-er-api-staging` | REST and SignalR endpoint; public HTTPS for the current browser SignalR connection model | Not deployed; configure SQL, externalized CORS origins, health probes, and identity first |
| Worker Container App, e.g. `ca-er-worker-staging` | Dedicated persistent queue consumer | Deferred until all job producers/consumers use a verified durable transport and a Worker host exists |
| Web/API runtime identities and scoped data roles | SQL, Blob, and Service Bus operations required by each workload | Deferred until Container App definitions and image deployment are authorized; SQL contained users require an approved database connection |
| Application Insights | Application telemetry and alerts | Not declared; current app exporters and Azure Monitor integration need configuration |
| Azure AI Search and Foundry | Retrieval and model workloads with environment-specific quotas and cost limits | Not declared |
| Azure SignalR Service | Optional SignalR scale-out | Not declared; evaluate after staging topology/load testing |

## Pilot examples

Use an independent pilot resource group and independent stateful services, for example `rg-er-pilot-centralindia`, `cae-er-pilot`, `ca-er-web-pilot`, `ca-er-api-pilot`, `ca-er-worker-pilot`, `sqldb-er-pilot`, and `sb-er-pilot-001`. Final names must follow each resource provider's rules and must be checked for availability. Do not reuse staging databases, storage containers, Service Bus namespaces, AI evaluation indexes, secrets, or identities.

## Shared controls and tags

The Bicep foundation applies `project`, `environment`, and `managedBy` tags. Add approved owner and cost-center tags before deployment. Avoid putting personal data, subscription secrets, or incident identifiers in tags. Configure an environment budget and cost alerts before deploying billable services. Record approved SKU, replica limits, retention, backup, network, and data residency decisions with the inventory before provisioning.

## Provisioning status

Phase 17.3 changes infrastructure definitions and runs a subscription-scope template-level `what-if` only. The current preview reports 26 resource changes, including the planned ACR pull role assignment; no changes are applied. Provider-level preflight is blocked because the subscription's one-Container-Apps-environment quota is already used. Request ACA environment quota before provisioning. The `10.40.0.0/16` range, East US region, and `GP_S_Gen5_1` database SKU are preview inputs; check network overlap, service availability, naming, data residency, and cost before provisioning. See [staging verification](staging-verification.md) for pending post-deployment checks.
