# Cost-conscious portfolio demo

**Status: isolated deployment path prepared; no Azure resource, model deployment, or account setting was changed.** This path is intentionally separate from `infra/main.bicep` and the staging/pilot templates. It is intended for synthetic portfolio data only.

## Architecture and scope

The subscription-scope entry point is `infra/portfolio-demo.bicep`; its resource-group-scoped implementation is `infra/modules/portfolio-demo-stack.bicep`. The entry point creates only the explicitly named resource group and this stack. Both app deployment toggles default to `false`; AI role assignment and verified-model flag also default to `false`. The preview helper submits only `az deployment sub what-if`.

| Component | Demo choice | Ongoing cost / limitation |
|---|---|---|
| Container Apps | One Consumption environment in a dedicated custom VNet; no Bastion, VM, or migration runner. API and Web both default to zero minimum replicas. | Scale-to-zero lowers idle compute. The first request can be cold; process-local background work may be lost after scale-down/restart. Set API minimum to one only for a live demo that needs background processing, and include the idle compute in the estimate. VNet-integrated environment infrastructure also costs money. |
| Container Registry | Basic SKU with managed-identity pulls. | Public registry endpoint; access to image pulls is identity-controlled. Basic is intended for low-volume use. |
| SQL | Entra-only authentication, General Purpose serverless, one vCore maximum, 0.5 minimum, 60-minute auto-pause; access is limited to the ACA subnet using a SQL virtual-network rule and SQL service endpoint. | Database storage/backups and compute remain billable; auto-pause may add cold-start latency. SQL public network access is enabled for the service-endpoint design, but the server has no broad IP allow rule: only the selected subnet is allowed. |
| Blob Storage | Standard LRS, private containers, shared-key and public blob access disabled; default-deny network ACL permits the ACA subnet. | Capacity, operations, retention, and egress cost. Uses a public PaaS endpoint with subnet firewall restriction, not a private endpoint. |
| Key Vault | Standard/RBAC, purge protection, short list of runtime secrets and a Web Data Protection key; public service endpoint with default-deny network ACL for the ACA subnet. | Vault/key retention can outlive resource deletion; secret and key operations are billable. Do not put secret values in Bicep parameters or source control. |
| Logs | Log Analytics, 30-day retention. | Ingestion is a significant variable cost; cap verbosity and set a budget. |
| AI | References the existing AI account only. No account or model deployment is created. | Model inference is separately metered. Deployment availability and app identity permission must be confirmed before setting the verification flag. |
| Search and durable messaging | Omitted. The Staging app uses the existing in-memory message transport; no Service Bus or AI Search is provisioned. | Search-backed retrieval/citations are unavailable. Background work is not durable across process restarts. Copilot/model failures must remain visible as degraded/unavailable; do not present fabricated AI output. |

The storage and SQL endpoints are public Azure service endpoints whose network ACLs allow the dedicated ACA subnet. This avoids private-endpoint and private-DNS resources while retaining a subnet restriction. It is a deliberate reduction from the private-endpoint staging model: it depends on the custom VNet and service endpoints being supported with the delegated ACA subnet in the target subscription/region. Confirm that in the current Azure documentation/provider preview before provisioning. Do not replace the subnet rules with “Allow Azure services” or unrestricted IP access.

SQL Entra-only auth and the SQL server Entra administrator are defined in Bicep. The API managed identity still needs a contained SQL user and reader/writer grants. `scripts/deployment/bootstrap-portfolio-demo-database.sql` creates that identity user with only `db_datareader` and `db_datawriter`; it refuses to run outside a database named `EmergencyResponse`. Run it as an authorized Entra database administrator through an approved access path. The network ACLs allow only the ACA subnet, so an operator's workstation cannot connect directly to SQL or Key Vault. Before migrations/bootstrap and adding Key Vault secrets, use approved temporary single-operator-IP firewall exceptions for the required services and remove them immediately afterward, or provide an approved short-lived migration runner. The included SQL script does not open those firewall rules. Apply schema migrations through the project's approved migration process; do not grant runtime DDL rights.

Apps are off by default because deploying them needs immutable image tags, Key Vault secrets, an exact Web origin/API URL, SQL identity setup, and a review of the AI permission. The API readiness check verifies SQL, while the AI provider check reports `Degraded` unless the operator explicitly marks deployment availability/permissions verified. That flag is an assertion, not a network probe. Search is intentionally unset. Do not set the flag based only on catalog availability or the existence of an AI account.

### Required and optional resources

For this hosted Web/API path, the eventual required resources are the isolated resource group, Container Apps environment and dedicated VNet/subnet, Web and API Container Apps, Azure SQL, a container registry, managed identities/RBAC, Key Vault for runtime secrets and protected Web Data Protection, and Storage for media/key persistence. Log Analytics is retained for diagnosing the demo; keep its 30-day retention and monitor ingestion. Build/push images, add secrets securely, create the SQL contained user, and apply migrations before enabling the apps. The preview defaults leave app resources disabled until those prerequisites are ready.

Live AI is optional: without deployments/permissions, AI features degrade and must say so. Azure AI Search is omitted, which removes indexed knowledge retrieval and citations; Copilot cannot answer from source documents. Service Bus is omitted, so queued work uses process-local memory and is not durable; avoid relying on it after scale-down. Storage is kept because the template wires media and Web Data Protection to it; removing it would make media non-durable and could invalidate authentication cookies after restarts. App Insights is not provisioned; platform logs go to Log Analytics. Bastion and the migration VM are omitted; use the narrowly controlled, temporary SQL operator access path described above. Azure Maps remains disabled. ACR Basic is the lowest-cost built-in registry tier in this template; GHCR could avoid ACR but requires a separate pull-secret workflow and adds an external registry dependency.

Removing the custom VNet would mean reaching SQL/Storage through public service endpoints. A fixed operator-IP firewall can control workstation access but does not provide a stable, narrow ACA-origin rule, while an “allow Azure services” exception is too broad for this demo. Keep the VNet and service-endpoint rules. Private endpoints and private DNS are removed to reduce cost/complexity, with the documented trade-off that data-plane traffic uses public PaaS endpoints protected by exact subnet ACLs rather than private IP connectivity.

## Cost estimate workflow

No exact monthly total is claimed: pricing depends on the chosen region/SKU, active hours, database size, model tokens, storage/operations, and Log Analytics ingestion. The calculator was not run in this session because those consumption inputs and credit expiry are not known. Before provisioning, enter these quantities into the [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/) for East US 2 and save the estimate with the deployment record:

1. Azure Container Apps Consumption: API at 1 vCPU/2 GiB and Web at 0.5 vCPU/1 GiB, both minimum 0 in the template; include expected active replica hours, request volume, and VNet-integrated environment network infrastructure charges. Model a second scenario with API minimum 1 if background work must run while idle.
2. Azure SQL Database serverless General Purpose Gen5: 0.5 minimum / 1 maximum vCore, 60-minute auto-pause, provisioned data size, backup retention, and expected active hours/resume cycles.
3. Basic ACR, Standard LRS Blob capacity/transactions/egress, Key Vault transactions/key operations, and Log Analytics GB ingested with 30-day retention.
4. Existing Azure AI account: estimate chat and embedding token volume at the selected Standard model deployment type. The template does not create those deployments.
5. Include outbound data, public IP/load-balancing/network charges as applicable. Do not include Azure free grants or the reported credit balance until eligibility and expiry have been checked in the target subscription.

Compare the calculator total with the actual remaining credit and intended demo duration; add an Azure budget and alert before any creation. A calculator estimate is not a spending cap. Delete the isolated resource group after the demo and verify retained/purge-protected resources and billing separately. Never delete the existing AI resource group as part of demo teardown.

## Existing Azure AI account and model plan

The brief supplies `eastus2`, AI resource group `modelgpt123`, and account `jhaji8569-9493-resource`; these are user-provided coordinates and were not queried in this session. The account is only referenced as an existing resource. The template has no model deployment resource. Before use, check account kind, tenant/subscription, account endpoint, current regional catalog, Standard quota and capacity, model version support, account deployment list, and the app managed identity's inference permission in Azure. The owner previously reported no deployments; that was not independently verified.

Candidate model names are `gpt-5-mini` for chat and `text-embedding-3-small` for embeddings. The current catalog lists `gpt-5-mini` version `2025-08-07` under Global Standard and `text-embedding-3-small` version `1` under Standard in East US 2. Global Standard is pay-per-token but may process in any Azure region; if that geography is not acceptable, do not deploy `gpt-5-mini` as Global Standard. A regional Standard option such as `gpt-4o-mini` version `2024-07-18` is listed for East US 2, but switching models is a deliberate app/configuration choice, not an automatic fallback. The template lets the deployment names be configured and defaults to the two candidate names. Catalog facts do not establish account eligibility or capacity. Check account kind, tenant/subscription, endpoint, current catalog, deployments, model version, quota/capacity, price, processing geography, and the API identity's inference permission in Azure before deployment. The owner previously reported no deployments; that was not independently verified. No model deployment or role grant is part of this preflight. If an authorized deployment and permission are later verified, set `aiDeploymentAvailabilityVerified=true` only as an operator assertion and recheck the health behavior.

## Preview and validation

`scripts/preview-portfolio-demo.ps1` requires an authenticated Azure CLI context and reads the signed-in Entra user for the prospective SQL administrator. It defaults the AI role grant to false and hard-codes apps and the verified-model flag off. It invokes subscription-scope `what-if` only; it does not submit a deployment.

Example after authenticating and selecting the intended subscription:

```powershell
az login
az account set --subscription '<target subscription>'
./scripts/preview-portfolio-demo.ps1 `
  -ResourceGroupName 'rg-er-portfolio-demo-eastus2' `
  -Location 'eastus2' `
  -AiResourceGroupName 'modelgpt123' `
  -AiResourceName 'jhaji8569-9493-resource'
```

Review resource creates, modifications, replacements, deletes, unsupported operations, policy failures, role-assignment scope, and quotas. A what-if is not a deployment or cost estimate. Do not enable AI role assignment without confirming the exact principal, account scope, and permission; do not deploy apps until image/secret/network/database prerequisites are reviewed.

## Checks before any future deployment

- [ ] Compile both Bicep files with a working current Bicep CLI; resolve all diagnostics.
- [ ] Confirm custom ACA VNet, delegated subnet size, and `Microsoft.Sql` / `Microsoft.Storage` service endpoint support in the target region and subscription.
- [ ] Run and review authenticated `what-if`; confirm quota, provider registration, names, RBAC permissions, and policy.
- [ ] Complete and save the Pricing Calculator estimate, verify actual credit/expiry, and create a budget alert.
- [ ] Confirm AI account coordinates, model deployment availability/quota/data geography, and API identity inference role before enabling model-dependent features.
- [ ] Build/push reviewed immutable images, create the three Key Vault secrets by approved secure process, bootstrap SQL contained user, apply migrations, configure exact CORS/API URL, then deploy apps.
- [ ] Exercise SQL/API/Web health, login, SignalR over HTTPS, upload/evidence, and degraded AI/Search behavior using synthetic data only.
- [ ] Tear down the isolated resource group after the demo and inspect retained resources/costs.

## References

- [Container Apps billing](https://learn.microsoft.com/en-us/azure/container-apps/billing)
- [Container Apps custom VNet](https://learn.microsoft.com/en-us/azure/container-apps/vnet-custom)
- [Azure SQL VNet service endpoint rules](https://learn.microsoft.com/en-us/azure/azure-sql/database/vnet-service-endpoint-rule-overview)
- [Azure SQL serverless tier](https://learn.microsoft.com/en-us/azure/azure-sql/database/serverless-tier-overview?tabs=general-purpose&view=azuresql)
- [Azure Container Registry SKUs](https://learn.microsoft.com/en-us/azure/container-registry/container-registry-skus)
- [Foundry model availability by region and deployment type](https://learn.microsoft.com/en-us/azure/foundry/foundry-models/concepts/models-sold-directly-by-azure-region-availability)
- [Foundry deployment types and data processing](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/deployment-types)
- [Azure OpenAI quota and capacity](https://learn.microsoft.com/en-us/azure/foundry/openai/quotas-limits)
- [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/)
