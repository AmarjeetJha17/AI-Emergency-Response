# Azure portfolio deployment preflight

**Status: infrastructure preparation only.** No Azure resources or model deployments were created by this work. A successful Bicep compile is not an Azure what-if, quota confirmation, or deployment.

## Requested preflight inputs

| Setting | Current value | Status |
|---|---|---|
| Target region | `eastus2` | User-selected; provider, SKU, quota, and subscription checks remain |
| Existing AI resource group | `modelgpt123` | User-provided; target subscription resource not queried here |
| Existing AI resource | `jhaji8569-9493-resource` | User-provided; kind `AIServices` reported by owner, not independently queried |
| Chat model | `gpt-5-mini`, version `2025-08-07` | Catalog lists it in East US 2; account and quota availability still need an authenticated check |
| Embedding model | `text-embedding-3-small`, version `1` | Catalog lists it in East US 2; account and quota availability still need an authenticated check |

These are command-line/runtime inputs, not defaults in the reusable Bicep templates. Do not add subscription IDs, tenant IDs, credentials, keys, or passwords to source control.

## Template adaptation

The subscription wrapper now requires an explicit region and existing AI resource group/name, then passes those values into `main.bicep`. The direct foundation and app templates also require explicit AI resource coordinates, preventing the former `AI-Emergency-Response-RG` and `ai-emergency-response` assumptions. `main.bicepparam` reads local `ER_AI_RESOURCE_GROUP` and `ER_AI_RESOURCE_NAME` environment variables.

The Web and API definitions already use the same existing Container Apps environment. The foundation declares one VNet-integrated Container Apps environment and `apps.bicep` attaches both apps to it. Current apps are configured separately; no second environment is needed for Web/API sharing.

Regional resources in `main.bicep` use the explicit `location` parameter; private DNS zones/links correctly use `global`. Apps inherit the resource group's deployment region. The temporary migration runner inherits the resource-group region unless explicitly overridden.

### Validation status in this workspace

- `git diff --check`, PowerShell syntax parsing, generated ARM JSON parsing, and a scan for the old AI account/resource-group/region defaults passed.
- Bicep compilation could not be completed. Azure CLI had no installed Bicep compiler; the standalone compiler present on disk hung on version invocation, and `az bicep install` could not resolve `aka.ms`. The generated `infra/staging-preview.json` was synchronized manually and parsed as JSON, but it must be regenerated and reviewed with a working Bicep compiler before relying on it.
- Azure `what-if` was not run because this session has no authenticated subscription. Use the command below after logging into the intended subscription. No deployment or model deployment command was executed.

## What-if command to run in the intended subscription

This environment has Azure CLI 2.91.0 but has no authenticated subscription context (`az account show` reports that `az login` is required). Therefore no Azure `what-if` was run here and no account state is claimed. After signing in locally and selecting the intended subscription, run:

```powershell
az login
az account set --subscription "<target-subscription-name-or-id>"
./scripts/preview-staging-network.ps1 `
  -ResourceGroupName 'rg-er-portfolio-preview' `
  -Location 'eastus2' `
  -AiResourceGroupName 'modelgpt123' `
  -AiResourceName 'jhaji8569-9493-resource' `
  -ValidationLevel 'Provider'
```

The script calls only subscription-scope `az deployment sub what-if`; it does not submit `create` or `update`. Provider validation is intended to surface current provider/quota checks where supported. Review every create, modify, replace, delete, and unsupported result before considering any deployment. The historical preview mentioned by the owner listed 53 creations and one unsupported change; the tracked ARM JSON currently contains 56 resource declarations recursively, including nested deployment wrappers and child resources. These counts are different measures and must not be treated as the new account's what-if result.

If a local CLI preview is still blocked, retain its exact error output and resolve it before provisioning. The account-specific result cannot be reproduced in this workspace because it is not signed into the user's new subscription.

## Dependency and cost review

This is a qualitative cost review, not a quote. The owner reports having about $200 in Azure credit; the balance/expiry has not been queried. Use the [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/) with East US 2, the actual selected plan/SKU, active hours, storage/log volumes, and the number of days the demo will run, then add an Azure budget/forecast alert before creation. The Bicep currently requests:

| Service in current foundation | Why the application uses it | Cost/decision to assess |
|---|---|---|
| Azure Container Apps + Log Analytics | Web/API hosting and platform logs | Consumption can scale to zero, but `apps.bicep` sets `minReplicas: 1` for both API and Web, so both apps remain active and accrue compute while idle. Review whether a controlled demo can reduce replicas without breaking the API's in-process workers; also price the VNet environment, log ingestion/30-day retention, and request/egress charges. Environment and log workspace are distinct resources. |
| Azure SQL logical server + `GP_S_Gen5_1` database | Operational records, identities, audit | Confirm the SKU maps to the intended serverless behavior, min/max vCores, auto-pause delay, storage, backup retention, and resume latency. The current Bicep does not explicitly set auto-pause policy. |
| Azure AI Search `basic`, one replica/partition | Knowledge retrieval and grounded Copilot | Provisioned Search capacity is an ongoing cost while the service exists, including idle periods; evaluate whether this feature is in the demo scope before creating it. |
| Service Bus `Standard` | The API currently publishes and consumes four durable job queues | Standard namespace has an ongoing namespace/SKU cost even with low message volume; Basic would not preserve the current managed-identity/RBAC plan. Do not omit without disabling or replacing these code paths. |
| Storage `Standard_LRS` + private endpoint | Incident evidence, knowledge documents, Web Data Protection | Data/operations and private endpoint costs; Web Data Protection persistence is required for multi-instance/revision continuity. The deployment-artifact container is temporary setup support. |
| VNet, private endpoints, private DNS | Private SQL, Blob and Search networking | Private endpoints have ongoing hourly costs; assess all three and DNS/network topology against the demo's security requirements. Public endpoints would require an explicit, reviewed alternative firewall/identity design, not a silent toggle. |
| Bastion + migration runner (separate template) | Temporary private DB migration/index provisioning path | Significant avoidable temporary spend while idle. Deploy only for setup, then remove Bastion/VM and temporary role/user assignments per the runbook. They are not part of `staging-preview.bicep` foundation itself. |
| ACR `Standard` | Holds immutable API/Web images | Ongoing tier cost plus stored-image storage; prune old images under a reviewed retention policy. |
| Key Vault `Standard` and RSA key | Runtime secret references and Web Data Protection key encryption | Transactions and key operations; purge protection/soft delete can retain the vault after deletion. Secret entry is an operator task. |
| Existing Azure AI account, two model deployments | Chat/analysis and embeddings | Pay-per-token `Standard` deployments are preferred over reserved PTUs for a low-volume demo; actual token usage and quotas need monitoring. |

An Application Insights resource is not defined by this foundation; OpenTelemetry instrumentation exists in the application, but Azure exporter/collector and dashboards must be configured separately if required. Private endpoints, Bastion, SQL, Search, logging, and ACR are the main fixed/ongoing items to quantify. No exact dollar estimate is asserted.

### Portfolio demo scope recommendation

The separate, feature-limited design is now documented in [cost-conscious-portfolio-demo.md](cost-conscious-portfolio-demo.md), with independent Bicep and a what-if-only preview helper. It leaves `main.bicep` and staging/pilot behavior unchanged. That design retains VNet subnet restrictions for SQL and Storage while removing private endpoints/DNS, Search, Service Bus, Bastion, and the migration VM. Review its explicit limitations, network service-endpoint compatibility, cost assumptions, and validation status before considering any provisioning. The full-feature staging cost table above still describes the original staging path; it is not an estimate for the portfolio demo.

## Model deployment plan (not executed)

Microsoft's current model catalog lists the requested `gpt-5-mini` and `text-embedding-3-small` versions for East US 2 under Standard availability. This is regional catalog availability only; it does not prove the specific account kind, subscription offer, quota, or deploy capacity is eligible. Recheck the live Foundry catalog and quota immediately before deploying.

For this low-volume synthetic portfolio demo, use pay-per-token `Standard` deployments, not reserved provisioned throughput. `Standard` keeps request processing in the Azure region of the resource; with the existing account reported in East US 2, use synthetic demo data only and do not send personal/real incident data. If quota/availability forces `GlobalStandard`, stop and explicitly accept its global processing geography before proceeding; do not silently switch deployment type.

After confirmation, the planned minimum-capacity requests are:

```powershell
az cognitiveservices account deployment create `
  --resource-group 'modelgpt123' `
  --name 'jhaji8569-9493-resource' `
  --deployment-name 'gpt-5-mini' `
  --model-format 'OpenAI' `
  --model-name 'gpt-5-mini' `
  --model-version '2025-08-07' `
  --sku-name 'Standard' `
  --sku-capacity 1

az cognitiveservices account deployment create `
  --resource-group 'modelgpt123' `
  --name 'jhaji8569-9493-resource' `
  --deployment-name 'text-embedding-3-small' `
  --model-format 'OpenAI' `
  --model-name 'text-embedding-3-small' `
  --model-version '1' `
  --sku-name 'Standard' `
  --sku-capacity 1
```

`1` is a deliberately small capacity request, not a guarantee that the provider accepts it or that it satisfies app throughput. Check the account's current minimum, per-region/model Standard quota, and capacity-to-TPM mapping in the portal/CLI first. If capacity 1 is rejected or not offered, stop and recalculate rather than automatically increasing it. Do not execute these commands as part of preflight. After a separately approved deployment, verify both deployment records report `Succeeded`, record model/version/SKU/capacity without secrets, test one synthetic inference and embedding, and verify the app's deployment names match the API configuration.

## Verification checklist before any provisioning

- [ ] Confirm selected subscription and ownership/budget, and create a cost budget/alert.
- [ ] Confirm resource-group name availability and resource providers are registered (`Microsoft.App`, `Microsoft.Sql`, `Microsoft.Storage`, `Microsoft.Search`, `Microsoft.ServiceBus`, `Microsoft.CognitiveServices`, `Microsoft.Network`, `Microsoft.KeyVault`, `Microsoft.ContainerRegistry`, `Microsoft.OperationalInsights`, `Microsoft.ManagedIdentity`).
- [ ] Check East US 2 availability/SKU and quotas for Container Apps environments, SQL serverless/database, Search Basic, Service Bus Standard, ACR Standard, private endpoints, public IP/Bastion only if temporary setup requires it, and model TPM.
- [ ] Recheck the previously observed Container Apps environment quota blocker in the new subscription.
- [ ] Check global-name availability where the provider requires it (SQL server, Storage account, ACR, Search service, Key Vault, Service Bus namespace) and validate all other names against provider/region/resource-group rules; Bicep-generated hashes reduce but do not eliminate collisions.
- [ ] Confirm deployment principal can create resource groups/resources, role assignments at each service scope, SQL Entra admin, and the AI account role assignment; verify Entra tenant and AI account are in the intended tenant/subscription relationship.
- [ ] Confirm `10.40.0.0/16` does not overlap existing VNets, private endpoint/DNS behavior, outbound routes, Web/API access, and browser SignalR reachability.
- [ ] Populate `jwt-signing-key`, `admin-seed-password`, and `syncfusion-license-key` through an approved secret path; verify SQL Entra-only app identity users, migrations, Search index (`1536` dimensions), images, CORS, HTTPS, health, and synthetic-only demo data.
- [ ] If using a reduced portfolio-only design, review its separate architecture, excluded features, exposure, teardown plan, and cost calculation before authoring/deploying it.

## Sources

- [Foundry model/region availability](https://learn.microsoft.com/en-us/azure/foundry/foundry-models/concepts/models-sold-directly-by-azure-region-availability)
- [Foundry deployment types and data-processing regions](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/deployment-types)
- [Foundry quota and capacity](https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/quota)
- [Azure Container Apps billing](https://learn.microsoft.com/en-us/azure/container-apps/billing) and [plan types](https://learn.microsoft.com/en-us/azure/container-apps/plans)
- [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/)
