# Azure Resource Inventory

This is an infrastructure and status inventory, not a provisioning record. The Bicep resources below are definitions and preview targets; they are not claims that those resources exist. Azure names have different length, character, and global-uniqueness rules; verify availability before deploying.

Environment boundaries and target roles are defined in the [environment matrix](environment-matrix.md).

## Bicep foundation resources

`infra/main.bicep` generates names from the resource-group ID and the `staging`/`pilot` environment parameter. `infra/staging-preview.bicep` wraps the foundation in a subscription-scope preview for `rg-er-staging-eastus`, with East US as the preview region. The preview script uses the active CLI subscription and signed-in Entra user without writing their identifiers into tracked files. Region availability, network overlap, global name availability, and pricing still require review before deployment.

| Bicep resource | Purpose | Intended identity access | Environment |
|---|---|---|---|
| Log Analytics workspace | Container Apps platform log destination; 30-day retention in the template | Container Apps environment writes platform logs; operations users read under Azure RBAC | Staging or pilot, separate instance per resource group |
| Virtual network and subnets | Isolate Container Apps infrastructure, private endpoints, and temporary private migration runner; `10.40.0.0/16`, ACA `/23`, private-endpoint `/24`, runner `/24`, Bastion `/26` | VNet control-plane access is administrative; subnet access is consumed by ACA, Private Link, and temporary migration services | Staging or pilot; CIDRs require overlap review |
| Container Apps environment | Replacement VNet-integrated environment `cae-er-staging-vnet`, externally reachable at environment level | Platform-managed identity integration; app identities are configured with app resources later | Staging preview target |
| Azure Container Registry | Stores versioned application images; admin account is disabled | Later app user-assigned or system-assigned identity gets `AcrPull`; CI identity gets narrowly scoped push permission | Staging or pilot |
| ACR pull identity and role assignment | Dedicated user-assigned identity with `AcrPull` scoped to this registry | Attached to Web/API registry configuration | Staging or pilot |
| API runtime identity `id-er-staging-api` | Pre-created API identity so SQL user bootstrap and database migration can occur before API activation | Container App selects it with `AZURE_CLIENT_ID`; Blob, Search, Foundry, Key Vault, and Service Bus permissions are scoped in the foundation | Staging only |
| Storage account | Hosts private incident-media, knowledge-document, Data Protection, and temporary deployment-artifact containers; anonymous Blob, shared-key, and public network access are disabled | API and Web identities receive container-scoped roles in `infra/apps.bicep`; no assignment is active until deployment | Staging or pilot |
| `incident-media` Blob container | Private incident uploads | API identity for authorized media operations; Worker only if processing requires retrieval | Staging or pilot |
| `knowledge-documents` Blob container | Private source knowledge documents | API/Worker identity only after Blob storage adapters and authorization are implemented | Staging or pilot |
| `web-data-protection` Blob container | Shared ASP.NET Core cookie-encryption keys | Web identity with Blob data access; encryption uses a Key Vault key | Staging or pilot |
| `deployment-artifacts` Blob container | Temporary EF bundle and Search-index tool transfer through the private runner | Access only through approved deployment workflow; no public access | Staging or pilot; delete artifacts after the staging setup |
| Key Vault | Stores environment secrets/keys with RBAC authorization and purge protection | Each workload identity receives only the specific secret/key access it needs; assignments are not in this foundation | Staging or pilot |
| Service Bus namespace | Durable message transport; local authentication is disabled | API identity sends; future Worker identity receives; role assignments are not in this foundation | Staging or pilot |
| `incident-ai`, `visual-analysis`, `duplicate-detection`, `external-context` queues | Durable queues matching current API publisher/consumer route names | API sender and future Worker receiver, scoped to required queues | Staging or pilot |
| Azure AI Search service and private endpoint | Isolated `emergency-knowledge-staging` index and private RAG retrieval | API Search Index Data Contributor; temporary runner can provision the index and its role is removed afterward | Staging only |
| Azure SQL logical server and database | Operational records, identity, and audit; Entra-only auth and public network access disabled | Entra administrator for setup; future API/Worker contained users get minimum data roles, not `db_owner` | Staging or pilot |
| SQL private DNS zone/link, private endpoint, and zone group | Resolve the normal SQL FQDN to its private endpoint for VNet workloads | API and future Worker use normal SQL FQDN through private DNS | Staging or pilot |

All four queues are included because the current Azure Service Bus publisher and consumer map to those names. This does not mean every application job is durable today; see the Worker deferral below.

## Target resources not declared in this foundation

| Resource | Purpose and access plan | Status |
|---|---|---|
| Resource group `rg-er-staging-eastus` | Isolate staging policy, ownership, and cost; wrapper includes it in subscription-scope `what-if` | Preview only; not created by the what-if |
| Web Container App `ca-er-web-staging` | Public HTTPS MVC/Web entry point; API URL, Data Protection, and Web license secret settings | Defined in `infra/apps.bicep`; not deployed |
| API Container App `ca-er-api-staging` | REST and SignalR endpoint; external HTTPS for the current browser SignalR connection model; max 1 replica while local job queues remain | Defined in `infra/apps.bicep`; not deployed |
| Worker Container App, e.g. `ca-er-worker-staging` | Dedicated persistent queue consumer | Deferred until all job producers/consumers use a verified durable transport and a Worker host exists |
| Web/API runtime identities and scoped data roles | Web gets Data Protection Blob and Key Vault rights; the pre-created API identity gets Blob, Search, Foundry, Key Vault, and four queue sender/receiver roles | Defined in `infra/main.bicep`; SQL contained users remain a separate DBA bootstrap step |
| Application Insights | Application telemetry and alerts | Not declared; current app exporters and Azure Monitor integration need configuration |
| Existing Foundry account | Reused low-volume for synthetic staging requests through managed identity; must confirm model endpoint role assignment | API OpenAI User assignment is defined in `infra/apps.bicep`; account remains in its existing resource group |
| Azure SignalR Service | Optional SignalR scale-out | Not declared; evaluate after staging topology/load testing |

## Pilot examples

Use an independent pilot resource group and independent stateful services, for example `rg-er-pilot-centralindia`, `cae-er-pilot`, `ca-er-web-pilot`, `ca-er-api-pilot`, `ca-er-worker-pilot`, `sqldb-er-pilot`, and `sb-er-pilot-001`. Final names must follow each resource provider's rules and must be checked for availability. Do not reuse staging databases, storage containers, Service Bus namespaces, AI evaluation indexes, secrets, or identities.

## Shared controls and tags

The Bicep foundation applies `project`, `environment`, and `managedBy` tags. Add approved owner and cost-center tags before deployment. Avoid putting personal data, subscription secrets, or incident identifiers in tags. Configure an environment budget and cost alerts before deploying billable services. Record approved SKU, replica limits, retention, backup, network, and data residency decisions with the inventory before provisioning.

## Provisioning status

Phase 17.4 implementation now includes Web/API and temporary-runner Bicep definitions, but none have been provisioned. Provider-level preflight previously reported the subscription's one-Container-Apps-environment quota was already used; recheck the quota before provisioning and request an increase if still blocked. The `10.40.0.0/16` range, East US region, SQL SKU, Search SKU, and Bastion SKU need current availability, overlap, and cost review. See [staging verification](staging-verification.md) for actual status and pending checks.
