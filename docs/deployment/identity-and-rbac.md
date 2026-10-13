# Identity and RBAC Plan

## Identity boundaries

| Principal | Planned access | Status |
|---|---|---|
| Current signed-in Entra user | SQL logical-server Entra administrator for the preview and initial staging setup | Supplied to the preview command at runtime; not stored in tracked files |
| ACR pull user-assigned identity | `AcrPull` scoped to the staging registry | Defined in `main.bicep` and attached to Web/API app definitions |
| Web Container App system identity | Key Vault secret and key access; Data Protection Blob container only | Assignments defined in `apps.bicep`; not exercised until deployment |
| API runtime user-assigned identity `id-er-staging-api` | Azure OpenAI inference, private media/knowledge Blob access, Search index data, Key Vault secret references, Service Bus queues, and Azure SQL | Created with foundation and granted resource-scoped roles before the API app exists, so its SQL user can be bootstrapped and migrations can run before app activation |
| Worker Container App system identity | SQL job access, Service Bus receive/complete, approved Blob retrieval, and processing services | Deferred until a Worker host and complete durable transport exist |
| Temporary migration-runner user-assigned identity | SQL migration after a DBA grants the contained user, plus temporary Search index provisioning | Defined in `staging-migration-runner.bicep`; remove its Search role, SQL user, identity, VM, and Bastion after setup |
| Deployment identity | Resource deployment and secret setup; separate from runtime identities | Azure role assignment remains an operator responsibility |

Do not reuse the SQL administrator identity as an application runtime identity. Do not grant runtime identities `db_owner` to make migrations work.

## Planned least privilege

`main.bicep` scopes API Blob roles to the media and knowledge containers, API Search access to Search Index Data Contributor, API model inference to the existing Foundry account, and Service Bus Sender/Receiver to the four queues currently consumed in the API. Web receives only its Data Protection Blob and Key Vault permissions. Create the API and temporary migration contained SQL users using `scripts/deployment/bootstrap-staging-database.sql`; the API receives `db_datareader`/`db_datawriter`, while the short-lived migration identity additionally receives `db_ddladmin`. Do not grant `db_owner`. Apply and verify migrations, then remove the temporary migration account and identity.

Azure Blob adapters use `DefaultAzureCredential` and are selected when `Storage:BlobServiceUri` is configured. Service Bus is selected explicitly in Staging with `Messaging:Provider=AzureServiceBus`; the current API both publishes and consumes the four Service Bus-backed job types. Other background queues remain process-local, so API max replicas are set to one and no Worker is deployed.

`AcrPull` is a registry-scope role for the dedicated user-assigned image-pull identity. ACR admin authentication remains disabled. Store no credentials in Bicep parameters; use managed identity or Key Vault for runtime secrets that cannot use identity authentication.

For direct resource-group deployments of `main.bicep`, `infra/main.bicepparam` reads `ER_SQL_ENTRA_ADMIN_LOGIN`, `ER_SQL_ENTRA_ADMIN_OBJECT_ID`, and `ER_SQL_SERVER_NAME` from the local process environment. The staging preview script obtains the admin values from the active CLI user and generates a server name without writing them to a tracked parameter file.

## Preview behavior

`scripts/preview-staging-network.ps1` reads the active Azure CLI subscription and signed-in Entra user's display name/object ID, then passes those values directly to subscription-scope `what-if`. It does not write the values to files and does not submit a deployment. SQL contained users and runtime role assignments are not created by this preview.
