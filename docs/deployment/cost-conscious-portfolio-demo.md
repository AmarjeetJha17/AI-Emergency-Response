# Cost-conscious portfolio demo

**Current status (26 October 2026): the portfolio Web and API Container Apps are deployed in Central US.** The maintainer verified the Web homepage and login, HTTP 200 from both health endpoints, API-to-SQL connectivity, and `AI__Enabled=false`. This portfolio path is separate from `infra/main.bicep` and the staging/pilot templates and is intended for synthetic data only. The historical deployment record below documents earlier checkpoints; statements that applications were absent apply to the dates recorded in those sections. The API's native Container Apps secrets and the Web's reduced-security Data Protection mode are documented in the current hosted-demo summary below.

## Current hosted-demo verification

The deployed URLs are the [Web application](https://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io/) and [API readiness endpoint](https://ca-er-api-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io/health/ready). As reported by the maintainer on 2026-10-26, the Web homepage and login worked, both Web and API health endpoints returned HTTP 200, SQL connectivity was verified, and the API was confirmed with AI disabled. This is a bounded verification report, not a complete end-to-end or operational acceptance test; the documentation update did not independently query Azure.

The Web app uses the `PortfolioDemo` environment and persists Data Protection keys to Blob Storage without Key Vault wrapping. This reduced-security configuration is strictly for synthetic demonstration data. The API AI feature is disabled; no live Azure OpenAI, computer-vision, or predictive-AI behavior is claimed for the hosted demo. The API uses native Container Apps secrets for its JWT signing key and initial synthetic-admin password; the Web Syncfusion license is also injected as a Container Apps secret. No secret values are recorded here.

## Architecture and scope

The subscription-scope entry point is `infra/portfolio-demo.bicep`; its resource-group-scoped implementation is `infra/modules/portfolio-demo-stack.bicep`. The entry point creates only the explicitly named resource group and this stack. Both app deployment toggles default to `false`; AI role assignment and verified-model flag also default to `false`. The preview helper submits only `az deployment sub what-if`.

| Component | Demo choice | Ongoing cost / limitation |
|---|---|---|
| Container Apps | One Consumption environment in a dedicated custom VNet; no Bastion, VM, or migration runner. API and Web both default to zero minimum replicas. | Scale-to-zero lowers idle compute. The first request can be cold; process-local background work may be lost after scale-down/restart. Set API minimum to one only for a live demo that needs background processing, and include the idle compute in the estimate. VNet-integrated environment infrastructure also costs money. |
| Container Registry | Basic SKU with managed-identity pulls. | Public registry endpoint; access to image pulls is identity-controlled. Basic is intended for low-volume use. |
| SQL | Entra-only authentication, General Purpose serverless, one vCore maximum, 0.5 minimum, 60-minute auto-pause; access is limited to the ACA subnet using a SQL virtual-network rule and SQL service endpoint. | Database storage/backups and compute remain billable; auto-pause may add cold-start latency. SQL public network access is enabled for the service-endpoint design, but the server has no broad IP allow rule: only the selected subnet is allowed. |
| Blob Storage | Standard LRS, private containers, shared-key and public blob access disabled; default-deny network ACL permits the ACA subnet. | Capacity, operations, retention, and egress cost. Uses a public PaaS endpoint with subnet firewall restriction, not a private endpoint. |
| Key Vault | Standard/RBAC, purge protection, and restricted network ACL remain part of the foundation. The currently deployed API credentials and Web Syncfusion license use native Container Apps secrets; the Web Data Protection key ring is persisted in Blob Storage without Key Vault wrapping. | The vault is retained for the foundation/future protected configuration; it is not the active source of these app secrets or Web key wrapping in the current reduced-security demo. Vault/key retention can outlive resource deletion. Never put secret values in Bicep parameters or source control. |
| Logs | Log Analytics, 30-day retention. | Ingestion is a significant variable cost; cap verbosity and set a budget. |
| AI | References the existing AI account only. No account or model deployment is created. | Model inference is separately metered. Deployment availability and app identity permission must be confirmed before setting the verification flag. |
| Search and durable messaging | Omitted. The Staging app uses the existing in-memory message transport; no Service Bus or AI Search is provisioned. | Search-backed retrieval/citations are unavailable. Background work is not durable across process restarts. Copilot/model failures must remain visible as degraded/unavailable; do not present fabricated AI output. |

The storage and SQL endpoints are public Azure service endpoints whose network ACLs allow the dedicated ACA subnet. This avoids private-endpoint and private-DNS resources while retaining a subnet restriction. It is a deliberate reduction from the private-endpoint staging model: it depends on the custom VNet and service endpoints being supported with the delegated ACA subnet in the target subscription/region. Confirm that in the current Azure documentation/provider preview before provisioning. Do not replace the subnet rules with “Allow Azure services” or unrestricted IP access.

SQL Entra-only auth and the SQL server Entra administrator are defined in Bicep. The API managed identity still needs a contained SQL user and reader/writer grants. `scripts/deployment/bootstrap-portfolio-demo-database.sql` creates that identity user with only `db_datareader` and `db_datawriter`; it refuses to run outside a database named `EmergencyResponse`. Run it as an authorized Entra database administrator through an approved access path. The network ACLs allow only the ACA subnet, so an operator's workstation cannot connect directly to SQL or Key Vault. Before migrations/bootstrap and adding Key Vault secrets, use approved temporary single-operator-IP firewall exceptions for the required services and remove them immediately afterward, or provide an approved short-lived migration runner. The included SQL script does not open those firewall rules. Apply schema migrations through the project's approved migration process; do not grant runtime DDL rights.

The general foundation template keeps both app deployment toggles off by default. The separate portfolio API and Web templates were later deployed after immutable images, native Container Apps secrets, SQL identity setup, and app configuration were prepared. In the current deployment, AI is explicitly disabled and Search/model behavior is not claimed. The API readiness endpoint and SQL connectivity were verified by the maintainer on 2026-10-26. The foundation's AI availability flag remains an operator assertion, not a network probe; do not infer deployed AI functionality from it.

### Required and optional resources

For this hosted Web/API path, the required resources include the isolated resource group, Container Apps environment and dedicated VNet/subnet, Web and API Container Apps, Azure SQL, the East US 2 container registry, managed identities/RBAC, and Blob Storage for media, knowledge files, and the Web key ring. Log Analytics is retained for diagnostics with 30-day retention. The current apps use native Container Apps secrets; the Web key ring is not Key Vault-wrapped, so this deployment is synthetic-data-only. Key Vault remains part of the foundation but is not currently used to resolve these app secrets or wrap the Web key ring. Reproduction also needs the reviewed immutable images, SQL contained identity, required database schema, and secret values supplied securely at deployment time. The foundation's app toggles remain off by default; the separate isolated app templates are used for the deployed apps.

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

## East US 2 deployment attempt — 2026-10-19

The authenticated Azure CLI context was verified as the enabled `Azure subscription 1`. The preview helper ran with the requested East US 2 resource group and existing AI-account coordinates; it reported 27 resources to create and no deletes or modifications. Both Bicep files compiled with Bicep CLI 0.48.1.

Subscription deployment `portfolio-demo-foundation-20261019` was submitted with Web/API deployment, AI inference role assignment, and AI availability assertion disabled. Azure later reported the subscription deployment as `Failed`.

Observed resources in `rg-er-portfolio-demo-eastus2`: Log Analytics workspace `log-er-portfolio-bxwiyolb`, API and Web user-assigned identities, Basic ACR `acerportfoliobxwiyolb`, VNet `vnet-er-portfolio-demo`, Storage account `sterportfoliobxwiyolb`, Key Vault `kv-er-portfolio-bxwiyolb`, and Container Apps environment `cae-er-portfolio-demo` (reported `Waiting` during inventory). No SQL server/database was present in the observed inventory. The resource group contained no Container Apps, and a read-only check found zero model deployments on the referenced AI account.

Two resource operations had failed in the nested deployment:

- SQL server `sql-er-demo-bxwiyolb`: Azure returned `RegionDoesNotAllowProvisioning`, stating East US 2 was not accepting new Azure SQL Database servers at that time.
- Key Vault key `web-data-protection`: Azure returned `ForbiddenByFirewall` because the vault's network ACL has `defaultAction: Deny` and `bypass: None`, so the Key Vault management-plane caller was not authorized from its client address.

No East US 2 retry, resource-group deletion, security relaxation, or cleanup was performed. The East US 2 resources remain preserved.

Validation performed for this attempt: both Bicep templates compiled; the exact preview helper completed with 27 creates and no deletes/modifications; `git diff --check` passed before deployment. Application tests were not run as part of this infrastructure operation.

## Central US deployment — 2026-10-20

The active Azure CLI context was verified as enabled `Azure subscription 1`. The Central US resource group was absent before deployment. Both Bicep templates compiled with the installed Bicep CLI. The updated what-if for `rg-er-portfolio-demo-centralus` in `centralus` reported **25 creates and no modifications or deletes**. The environment quota error from the earlier preflight had cleared after the user removed the former East US 2 environment. The preview kept Web/API disabled, AI inference role assignment disabled, and model availability assertion disabled. The plan retained the Key Vault's `defaultAction: Deny` and `bypass: None`.

Subscription deployment `portfolio-demo-foundation-centralus-20261020` was submitted in `centralus`; nested resource-group deployment is `portfolio-demo-stack`. Both scopes now report `Succeeded` with no deployment error. The Container Apps environment reports `Succeeded` and has static IP `4.249.153.64`. No second deployment was submitted.

Verified Central US resources in `rg-er-portfolio-demo-centralus` at the latest inventory:

| Resource | Observed status/configuration |
|---|---|
| `cae-er-portfolio-demo` | Container Apps environment `Succeeded`; static IP `4.249.153.64`; default domain `blacksand-fa4db74f.centralus.azurecontainerapps.io`; subnet is `vnet-er-portfolio-demo/aca-infrastructure`. |
| `sql-er-demo-qqgqkekj` | SQL server `Ready` in Central US. |
| `EmergencyResponse` database | `Online`, General Purpose serverless `GP_S_Gen5`; virtual-network rule succeeded. |
| `vnet-er-portfolio-demo` and `aca-infrastructure` subnet | Present in Central US; subnet operation succeeded. |
| `acerportfolioqqgqkekj` | ACR Basic; provisioning `Succeeded`; admin user disabled. |
| `sterportfolioqqgqkekj` | Storage Standard LRS; provisioning `Succeeded`; public blob access and shared-key access disabled. `incident-media`, `knowledge-documents`, and `web-data-protection` containers and their role assignment operations succeeded. |
| `kv-er-portfolio-qqgqkekj` | Key Vault Standard/RBAC; provisioning `Succeeded`; `defaultAction: Deny`, `bypass: None`; subnet rule retained. The key was not created by the follow-up helper; existing key presence could not be verified because data-plane listing was denied by the firewall. |
| `id-er-portfolio-api`, `id-er-portfolio-web` | Both user-assigned identities present in Central US. |
| `log-er-portfolio-qqgqkekj` | Log Analytics workspace provisioning `Succeeded`, 30-day retention. |

The subscription and resource-group deployments both report `Succeeded`, and `cae-er-portfolio-demo` reports environment state `Succeeded`. The resource inventory contained no Web or API Container Apps. A read-only query of `modelgpt123/jhaji8569-9493-resource` returned no AI model deployments. Successful nested operations included the SQL server/database, SQL Entra-only authentication, SQL subnet rule, managed identities, VNet/subnet, registry, Storage, Key Vault, Log Analytics, and planned role assignments.

### Key Vault follow-up — 2026-10-20

The follow-up helper was inspected and minimally strengthened to validate the exact target resource group/region, verify the configured RSA key operations, and retry firewall cleanup when Azure CLI checks fail. PowerShell syntax validation passed. It was run against only `rg-er-portfolio-demo-centralus/kv-er-portfolio-qqgqkekj` after confirming the enabled `Azure subscription 1`, the Central US vault, and both managed identity principals.

The helper added only the workstation's discovered current IPv4 `/32` temporarily. During that window, Key Vault data-plane `key list` continued to fail with `ForbiddenByFirewall` / `Client address is not authorized` even though the exact address appeared in the vault ACL. After its 18 attempts over three minutes, the helper stopped before key creation, key verification, or key-scoped role assignment. Its `finally` cleanup removed the temporary IP rule and verified the vault ACL. A final management-plane read confirmed `ipRules: []`, `defaultAction: Deny`, and `bypass: None`.

The intended key therefore was **not created by this run**, and its presence cannot be independently confirmed because Key Vault data-plane reads are blocked. No `Key Vault Crypto User` key-scoped grant was made. The Web identity retains its existing `AcrPull`, vault-scoped `Key Vault Secrets User`, and Web Data Protection container-scoped `Storage Blob Data Contributor` assignments. The API identity retains its existing `AcrPull`, vault-scoped `Key Vault Secrets User`, and `Storage Blob Data Contributor` assignments scoped to the `incident-media` and `knowledge-documents` containers. No unexpected role assignment was created by the failed helper. The vault-wide Crypto User grant remains absent.

Do not retry through this workstation or relax the ACL. A safe next attempt needs a data-plane access path Azure actually recognizes, such as a verified approved operator egress IP that is allowed by the vault or an authorized private-network runner. Key setup blocks enabling the Web app because its Data Protection key remains unverified; it does **not** itself block the separately authorized SQL initialization stage. No SQL initialization, migrations, model deployment, AI grant, or Web/API deployment was performed in this task.

The earlier `MaxNumberOfGlobalEnvironmentsInSubExceeded` what-if failure no longer reproduced after the East US 2 Container Apps environment was removed; the Central US what-if passed and no quota increase request was submitted. The East US 2 resource group was not modified in this task.

Validation for this recovery: both Bicep templates compiled; the latest Central US what-if reported 25 creates and no modifications/deletes; the subscription and resource-group deployments succeeded; PowerShell parser validation passed for the helper; and `git diff --check` passed. The key operation failed before key creation as detailed above. Application tests were not run. No commit or push was made.

### SQL initialization and migrations — 2026-10-20

The existing Central US `EmergencyResponse` database was initialized in enabled subscription `Azure subscription 1` using Microsoft Entra authentication. Its initial Serverless state was `Paused`; a normal database connection resumed it for the SQL operations. After the session ended, Azure auto-paused it again. The API and Web Container Apps remain undeployed.

The API managed identity `id-er-portfolio-api` was verified in Azure and its principal ID was used to create the contained database user. Azure SQL rejected a name-only `FROM EXTERNAL PROVIDER` lookup because the identity display name is duplicated in the tenant. The bootstrap SQL now takes `ApiIdentityObjectId` as a sqlcmd variable and uses `WITH OBJECT_ID` to bind the intended identity. The user has `db_datareader` and `db_datawriter`; it is not a member of `db_owner` or `db_ddladmin`.

The repository EF Core migrations were applied once with `dotnet ef database update` against this database. The migration bundle command could not finish packaging on this Windows host because EF's temporary-file cleanup failed, so the direct EF CLI migration workflow was used. The design-time context factory now accepts a SQL-scoped Entra access token from the process environment and passes it to `Microsoft.Data.SqlClient`; no token or password is stored in the repository. The Azure SQL authentication extension needed for `Active Directory Default` was not present in the current package set, so this one-time flow used the short-lived CLI token.

Verification confirmed 47 user tables, 36 migration-history rows, and latest migration `20261016170848_AddPortfolioDemoOwnership`. The IDs returned by the EF migration listing exactly match all 36 database history IDs; there are no pending migrations. The API user's least-privilege role memberships were rechecked after migration.

For operator access, only the workstation's verified public IPv4 address was temporarily added as a SQL server `/32` firewall rule. The rule was removed in a `finally` cleanup path and independently verified absent. The final SQL firewall list is empty; `portfolio-demo-container-apps` remains `Ready` for the `vnet-er-portfolio-demo/aca-infrastructure` subnet. SQL remains Microsoft Entra-only. Its public network access setting remains enabled for the subnet service-endpoint design, with no broad IP rule.

The SQL initialization did not deploy apps, create AI model deployments, or change Key Vault. Final read-only checks found no Container Apps in the demo resource group and no model deployments on the referenced AI account. Key Vault `kv-er-portfolio-qqgqkekj` remains `defaultAction: Deny`, `bypass: None`, with no IP rules; its Web Data Protection key is still a separate outstanding setup item.

Validation: the API Release build succeeded with 27 package vulnerability warnings. The temporary SQL runner build succeeded with one transitive NuGet version-resolution warning. PowerShell syntax validation passed for the temporary scripts; `git diff --check` is recorded after this update. No application tests were run. Work remains uncommitted and unpushed.

### VNet-contained Key Vault provisioning attempt — 2026-10-22

The earlier workstation attempt failed with `ForbiddenByFirewall`; its diagnostic named client address `20.62.60.129` and caller `KeyVault/ManagementPlane`. Adding the workstation `/32` did not authorize that request path. The error confirms that the operation was evaluated against the Key Vault firewall as a management-plane caller, but does not reveal the internal Azure network hop. I did not repeat the workstation-IP procedure.

Before trying an ACA job, read-only checks confirmed the enabled `Azure subscription 1`, the Central US vault, the healthy `cae-er-portfolio-demo` environment, and the exact `aca-infrastructure` subnet. That subnet has a succeeded `Microsoft.KeyVault` service endpoint and is already listed in the vault's virtual-network rules. The vault still had `defaultAction: Deny`, `bypass: None`, and no IP rules.

A manual, non-ingress Container Apps Job was attempted in the existing environment using a dedicated temporary user-assigned identity and a vault-scoped `Key Vault Crypto Officer` role. ARM accepted the job create request, but the job resource reached `provisioningState: Failed` before an execution was started. The provider did not return a useful failure detail in the resource response or Activity Log. Consequently, the job did not inspect or create the key, perform an encryption round trip, or grant the Web identity `Key Vault Crypto User` at key scope. Key presence remains unverified because the workstation data-plane request is still blocked.

Cleanup checks confirmed the temporary identity is absent, there are no vault-scoped `Key Vault Crypto Officer` assignments, and the resource-group inventory lists zero Container Apps Jobs. The Container Apps CLI reported the job deleted and ARM accepted asynchronous delete requests; however, a direct GET by the deleted job ID continued returning a stale `Failed` record. Full control-plane tombstone removal could not be confirmed. No temporary role assignment or job identity remains. The Web and API identities have no `Key Vault Crypto User` assignment at the key scope. The final vault ACL remains restricted (`Deny` / `None`, zero IP rules), and the ACA subnet endpoint/rule was not changed.

This attempt did not deploy Web/API apps, initialize SQL, create AI deployments, or change the existing schema. Application tests were not run. The next safe step is to resolve the Container Apps Job provisioning failure and verify deletion of the failed job record before retrying the VNet-contained workflow; do not reopen workstation access or broaden the vault firewall. Web deployment remains disabled until the key and Web identity access are independently verified.

### Read-only diagnosis of the failed key-provisioning job — 2026-10-22

The active context was again verified as the enabled `Azure subscription 1`. The attempted resource was `Microsoft.App/jobs/kv-key-provisioner-20261022` in `rg-er-portfolio-demo-centralus`, using `cae-er-portfolio-demo`. The recorded attempt used the Azure CLI image `mcr.microsoft.com/azure-cli:2.91.0`, a manual job with no ingress, and a temporary user-assigned identity; the intended action was to inspect/create the Web Data Protection key. The job resource itself has since been deleted, so its submitted command, environment variables, CPU/memory, and complete identity configuration cannot be re-read from ARM.

The Activity Log shows repeated job reads from approximately 09:18 to 09:23 UTC alternating between `Running` and `Deleting`. At `2026-10-22T09:23:25.1295003Z`, the job write operation ended as `Canceled`, correlation ID `33f743a5-e506-474e-b7d3-c9e6c348191b`, with only the generic error `ResourceOperationFailure`: “The resource operation completed with terminal provisioning state 'Canceled'.” The deletion completed at `2026-10-22T09:23:32.3551537Z`; the Activity Log recorded `Microsoft.App/jobs/delete` as `Succeeded`. A current resource GET returns `ResourceNotFound`, and a current resource-group inventory lists no `Microsoft.App/jobs` resources. The earlier stale failed record is no longer visible.

This does **not** establish the original cause of the provisioning failure: the cancellation record reflects the cleanup operation, not the failure that preceded it. No nested deployment operation or original provider error is exposed by the retained Activity Log records. The environment's app log destination is Log Analytics, but the environment has no diagnostic settings, and the expected `ContainerAppSystemLogs_CL` table is not present in the workspace; therefore no container startup/system records are available for this job. The evidence supports that the failure happened before a usable execution, but it cannot distinguish a job configuration/provider problem from image retrieval or startup. It does not implicate Key Vault networking, RBAC, or key creation because the job never reached a recorded key operation.

Current control-plane checks still show the environment `Succeeded`, attached to `vnet-er-portfolio-demo/subnets/aca-infrastructure`. That subnet has a succeeded `Microsoft.KeyVault` service endpoint, and the vault rule references that same subnet ID. The Key Vault remains RBAC-enabled with `defaultAction: Deny`, `bypass: None`, and no IP rules. The vault has only the existing Web and API `Key Vault Secrets User` grants (plus inherited subscription roles); no temporary `Key Vault Crypto Officer` assignment or key-scoped `Key Vault Crypto User` assignment is present. A direct key data-plane read returned `ForbiddenByFirewall` for the workstation request, so the key is **unverified**, not confirmed absent.

Recommended next steps, in order:

1. Use correlation ID `33f743a5-e506-474e-b7d3-c9e6c348191b` and the failed job resource name with Azure support, and check saved CLI output for the original provider error. The canceled write record is not the original failure and is insufficient to prescribe a configuration change.
2. Once the error is known, review a static job definition that records the exact environment, image, command, sizing, and dedicated identity; submit a new run only after validating the identified defect and preserving the approved subnet boundary.
3. If Azure cannot recover the provider detail, assess another reviewed provisioning path that stays inside the approved network and grants only the required key-scoped access. Do not remove the Key Vault key dependency: no application/configuration analysis in this investigation established a safe substitute.

Checks performed: active account verification; Central US resource inventory; environment, vault, and subnet reads; Activity Log inspection for the job and its UTC window; job resource GET and current jobs inventory; Container Apps diagnostic-settings and Log Analytics workspace checks; key data-plane read; vault- and key-scope role-assignment reads; and `git diff --check`. No infrastructure, job, firewall, identity, role, app, or model deployment was changed during this investigation.

### Explicit Data Protection fallback design — 2026-10-22

The Web application now supports an explicit reduced-security option for the synthetic portfolio demo while retaining the existing Blob Storage key-ring persistence. The default secure hosted path requires both `DataProtection:BlobUri` and `DataProtection:KeyIdentifier` and uses `ProtectKeysWithAzureKeyVault`. Missing or partial Key Vault configuration does not select the fallback; outside `Development` and `Test`, missing secure configuration fails startup. The framework-default key ring remains available only to local development and test hosts without hosted settings.

The fallback opt-in is `DataProtection:UsePortfolioDemoUnwrappedKeys=true`. Startup accepts it only when `ASPNETCORE_ENVIRONMENT=PortfolioDemo`, requires the Web Data Protection Blob URI, and rejects a simultaneously configured Key Vault key identifier. Any other environment, including Production and Staging, rejects the flag. The portfolio Bicep path keeps `deployWeb=false` by default; when an explicitly enabled Web deployment also opts into the fallback, it sets the environment to `PortfolioDemo`, passes the flag, persists to the existing `web-data-protection` container using the Web managed identity, and leaves the key identifier empty. Otherwise, the Web deployment remains in `Staging` and requires the verified Key Vault key URI. The app name remains `EmergencyResponse.Web` across revisions.

In fallback mode, the Data Protection key ring persists across container restarts and revisions in Blob Storage, but the XML key material has no additional Azure Key Vault wrapping layer. Azure Storage platform encryption is not equivalent to application-level protection of these Data Protection keys. Keep access restricted to the existing Web managed identity and the `web-data-protection` container. Use synthetic data only; this mode is not approved for real incident records or production operations. The Key Vault-wrapped configuration remains the preferred design when key provisioning is available. The app emits a startup warning whenever the fallback is selected.

The Web project already uses Microsoft's current Azure Data Protection extension packages, `Azure.Extensions.AspNetCore.DataProtection.Blobs` 1.5.4 and `Azure.Extensions.AspNetCore.DataProtection.Keys` 1.6.4, which are the current versions shown on NuGet for the `net10.0`-targeted packages as reviewed on 2026-10-22. Microsoft guidance documents the same Blob persistence and Key Vault wrapping APIs and recommends managed identity credentials and a stable application name for shared deployments. References: [ASP.NET Core Data Protection configuration](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0), [Blob provider 1.5.4](https://www.nuget.org/packages/Azure.Extensions.AspNetCore.DataProtection.Blobs/1.5.4), and [Key Vault provider 1.6.4](https://www.nuget.org/packages/Azure.Extensions.AspNetCore.DataProtection.Keys/1.6.4).

Focused configuration tests passed: 10/10 for the secure default, missing Key Vault configuration, explicit PortfolioDemo selection, rejection in other environments, Production fail-closed behavior, ambiguous configuration, and local Development defaults. The Web Release build succeeded with 0 errors and 15 existing warnings. Both Bicep templates compiled successfully, and `git diff --check` passed. The Bicep app toggles remain off by default; AI inference grants and model-availability assertions also remain disabled. This change does not provision Azure resources or deploy applications. Web deployment is still blocked on the existing app prerequisites (reviewed immutable images, API URL/CORS values, required secrets, and the selected Data Protection mode); choosing the fallback removes only the Key Vault key and key-scoped Crypto User prerequisite. No Azure runtime verification is claimed.

### App image deployment attempt — 2026-10-22

The selected Azure context was verified as enabled `Azure subscription 1`. The Central US resource group and Container Apps environment reported `Succeeded`; the SQL database was `Paused` at inspection time, the Basic ACR reported `Succeeded`, and the Key Vault remained restricted with `defaultAction: Deny` and `bypass: None`. The current app inventory was empty. The old East US 2 resource group was not inspected or changed.

To keep the Key Vault data-plane blocker from preventing portfolio-only secret provisioning, the portfolio Bicep entry point now has an explicit `usePortfolioDemoContainerAppSecrets` opt-in and `@secure()` parameters for the API JWT key, synthetic admin password, and Web Syncfusion license. It uses native Container Apps secrets only when explicitly selected; Key Vault references remain the default. The option does not alter the general Staging/Production templates. No secret values were generated, entered, or submitted during this attempt.

Docker Engine 29.8.0 built both images from the current uncommitted working tree under unique run tag `portfolio-20261022-155736-311846`; the tag is not a commit SHA. Azure CLI ACR login succeeded. The API image push failed while committing layer `sha256:9b660e3f3a1a97d9bf64bb18f8e17b89cc78e7fb667a5b62264e13f565ad233f`, with `net/http: timeout awaiting response headers`. A subsequent registry repository inventory was empty. The Web image was not pushed, and no app deployment or Bicep deployment was submitted. Per the deployment gate, do not proceed to app deployment after this push failure; first resolve the registry upload timeout, then build/push and verify both tags before running app what-if.

No API/Web app, SQL migration, AI model deployment, or AI inference role assignment was created. Both portfolio Bicep files compiled after the secret-path change. Docker restore reported existing NuGet vulnerability warnings for `System.Security.Cryptography.Pkcs` and `System.Security.Cryptography.Xml`; these did not prevent the image builds. No deployment health or SQL connectivity checks were possible. Changes remain uncommitted and unpushed.

### API-only AI-disabled configuration — 2026-10-22

The isolated API deployment sets `AI__Enabled=false` and omits AI endpoint and model deployment settings. The API does not register Azure OpenAI chat or embedding clients in this mode. AI-only endpoints return HTTP 503 with a Problem Details response titled `AI feature is disabled.` Non-AI API routes remain available; situation clustering may still run from incident data without an AI-generated summary. This setting is specific to the portfolio API deployment; other environments retain the existing AI-enabled default and validate their HTTPS endpoint and chat/embedding deployment names at startup.

### API native-secret fallback preparation — 2026-10-22

The isolated `infra/portfolio-demo-api.bicep` entry point now accepts `apiJwtSigningKey` and `apiAdminSeedPassword` as required `@secure()` parameters and stores them as native Azure Container Apps secrets. The `Jwt__SecretKey` and `AdminSeed__Password` environment entries reference those secrets with `secretRef`. This entry point no longer asks Container Apps to resolve these two values from Key Vault. Key Vault itself, its ACLs, the general portfolio stack, and Web configuration are unchanged. No values are embedded in the template, outputs, or a checked-in parameter file.

The API configures HMAC-SHA256 JWT signing. The application has no separate configured minimum-length validator; the signing implementation uses UTF-8 bytes as the symmetric key, and the IdentityModel HMAC-SHA256 implementation requires at least 256 key bits. For a new value, generate at least 32 cryptographically random bytes and encode them as unpadded Base64URL ASCII (43 characters for 32 bytes). Do not reuse a human password as the signing key. JWTs issued under a different signing key will no longer validate. The failed Container App had no provisioned revision, so this app has no prior revision-issued tokens to preserve.

`AdminSeeder` looks up the configured portfolio-admin email first. If it already exists, the seeder returns without changing its password or profile. If it does not exist, startup creates the account using the supplied password and assigns the SystemAdmin role. A replacement value therefore does not rotate an existing account; it becomes the initial password only if that account is absent. The configured Identity password rules require at least 8 characters, with uppercase, lowercase, digit, and non-alphanumeric characters; the deployment parameter enforces a 12-character minimum. Before a deployment is approved, generate a fresh high-entropy password meeting those rules and deliver it to the intended operator through the approved out-of-band channel.

For a future, explicitly approved deployment, provide both values only through an interactive secure-string parameter prompt or an approved deployment runner's protected secret inputs, passing them directly to the secure Bicep parameters in memory. Do not put values in a command literal, shell history, transcript, ordinary parameter file, environment dump, log, or output. Azure Resource Manager secure parameters are not displayed in deployment history or command output; they do not replace protecting the operator-side input path. Discard preview-only values and generate the deployment values separately. References: [Bicep secure parameters](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/parameters), [ARM secure-parameter guidance](https://learn.microsoft.com/en-us/azure/azure-resource-manager/templates/best-practices).

This is template preparation only. For structural what-if validation, separate cryptographically random preview-only JWT and password strings were supplied through the secure parameters; CLI output masked the values, and they were not saved, selected for app use, or deployed. Do not reuse them. The existing failed app, Key Vault, SQL, identity, networking, storage, Web app configuration, and AI configuration were not changed.

The API-only resource-group what-if against the existing `ca-er-api-portfolio` succeeded. It predicts **one modification** to that Container App (native secret definitions and their existing environment-variable references), **11 ignored existing resources**, and **zero actionable changes** to unrelated infrastructure. The raw preview represented four resource-derived values as ARM reference expressions; read-only comparison confirmed the resulting registry host, image tag, API identity client ID, and SQL connection configuration match the existing app. The existing `Consumption` workload profile and 100 inactive-revision limit are also preserved. The preview kept `AI__Enabled=false`, ingress, probes, and resource sizing. No deployment was submitted.

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
- [Container Apps quotas](https://learn.microsoft.com/en-us/azure/container-apps/quotas)
- [Request an Azure Container Apps quota increase](https://learn.microsoft.com/en-us/azure/container-apps/quota-requests)
- [Key Vault RBAC guide](https://learn.microsoft.com/en-us/azure/key-vault/general/rbac-guide)
- [Key Vault network security](https://learn.microsoft.com/en-us/azure/key-vault/general/network-security)
