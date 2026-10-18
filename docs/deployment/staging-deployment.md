# Staging Deployment Runbook

This runbook deploys only the Web and API apps with synthetic records. It does not deploy the Worker or make a production/pilot release. The API is limited to one replica because situation, knowledge, predictive and some other job queues still use in-process channels.

## Gates

1. Confirm the active Azure subscription/resource group and request a Container Apps environment quota increase if provider validation reports the environment limit. The current requested portfolio preflight is East US 2; see [portfolio Azure preflight](portfolio-azure-preflight.md).
2. Review East US 2 service/SKU availability, VNet overlap (`10.40.0.0/16`), SQL/Search/Bastion pricing, model deployment availability/quota, and expected AI request cost.
3. Compile all tracked Bicep templates, then run subscription `what-if` with explicit `location`, `aiResourceGroupName`, and `aiResourceName`; review create/modify/replace/delete and unsupported operations. The preview helper is `scripts/preview-staging-network.ps1`.
4. Provision the foundation only after the quota, budget, naming and network reviews pass. Confirm SQL, Blob and Search private endpoints are approved and their private DNS resolves from the Container Apps VNet.
5. Populate Key Vault secrets `jwt-signing-key`, `admin-seed-password`, and `syncfusion-license-key` through an approved secret-entry path. Never put the values in Bicep parameters or shell history.

## Build immutable images and migration tools

Commit the exact source to deploy, then use its 12-character SHA as `imageTag`. Run `scripts/deployment/prepare-staging-artifacts.ps1` to build the solution, generate a self-contained Linux EF bundle, and publish the Search index provisioner. The script writes only under ignored `artifacts/staging/`.

Build and push just Web and API. Do not build/deploy a Worker image for this release.

```powershell
$registry = az acr show --name <staging-acr> --query loginServer --output tsv
$tag = (git rev-parse --short=12 HEAD).Trim()
az acr login --name <staging-acr>
docker build -f docker/Dockerfile.api -t "$registry/emergencyresponse-api:$tag" .
docker build -f docker/Dockerfile.web -t "$registry/emergencyresponse-web:$tag" .
docker push "$registry/emergencyresponse-api:$tag"
docker push "$registry/emergencyresponse-web:$tag"
```

## Private database migration and Search index

The foundation creates the API user-assigned identity and assigns its Azure data-plane roles before the API container exists. Deploy the temporary runner from `infra/staging-migration-runner.bicep` with an SSH public key. It has no public VM IP; connect through the Standard Azure Bastion and use a local Bastion tunnel for SSH/SCP. Copy `artifacts/staging/efbundle`, `artifacts/staging/search-index-provisioner/`, and `scripts/deployment/bootstrap-staging-database.sql` to the VM. Install the approved SQL command-line tooling on the VM if it is not already available.

As the signed-in Microsoft Entra SQL administrator, execute the database bootstrap script against the private Azure SQL FQDN and `EmergencyResponse` database. It creates the pre-existing API runtime identity user (`id-er-staging-api`, `db_datareader`/`db_datawriter`) and temporary migration user (`id-er-staging-migration`, `db_datareader`/`db_datawriter`/`db_ddladmin`). Verify `USER_NAME()` and role membership. The separate user-assigned identities exist from the foundation and runner deployments; no app container needs to start before migrations.

As the temporary runner managed identity (set `AZURE_CLIENT_ID` to the runner identity client ID), execute the migration bundle with the Entra-authenticated connection string. Run the Search index provisioner with the private Search endpoint, `emergency-knowledge-staging`, and the configured embedding dimension (`1536` for the currently selected `text-embedding-3-small` deployment). Verify the migrations table and index fields before deploying the API revision. Remove the temporary Search role assignment and SQL migration user after setup; remove the runner deployment and Bastion resources when no longer needed.

## Deploy API, then Web, then update CORS

After migrations, the Search index and SQL contained users have been verified, deploy `infra/apps.bicep` with the immutable `imageTag`, `deployWeb=false`, and explicit `aiResourceGroupName`/`aiResourceName`. Both apps use the same existing Container Apps environment. Confirm the API revision starts, the managed identity reaches SQL, Service Bus, Blob, Search, Key Vault and Foundry, and `/health/ready` is healthy.

Read the API ingress FQDN. Deploy `infra/apps.bicep` with `deployWeb=true`, `apiBaseUrl=https://<api-fqdn>/`, the same `imageTag`, and the same explicit AI account coordinates. Read the Web ingress FQDN. Run the Bicep deployment again with `deployWeb=true`, the actual API URL, the same AI coordinates, and `webOrigin=https://<web-fqdn>` so the API CORS policy allows the deployed Web origin. Keep external API ingress enabled because the browser currently connects directly to `/hubs/incidents`.

Confirm revisions, identity-based image pulls, Key Vault secret references, startup/readiness probes, console logs, Data Protection persistence and the absence of secret values in logs. Web/API URLs and CORS must all use HTTPS.

## Functional verification and recovery

Use synthetic test accounts and records only. Verify Citizen incident creation, Operator authorization/read, database persistence, AI success and unavailable behavior, duplicate-candidate behavior, private media authorization, resource transitions/concurrency, escalation, situation command center, notifications, grounded Copilot, explicit/audited Copilot approval, and SignalR reconnect followed by an authoritative snapshot refresh. Verify a prior app image can be redeployed and record the database migration state; do not assume an image rollback reverses schema changes.

Record actual image tags, deployment and test links, health/network evidence, failures and limitations in `staging-verification.md`. Do not mark unexecuted tests as successful. Worker deployment, broad-scale load tests, and pilot entry remain out of scope for this staging release.
