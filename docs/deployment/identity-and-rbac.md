# Identity and RBAC Plan

## Identity boundaries

| Principal | Planned access | Status |
|---|---|---|
| Current signed-in Entra user | SQL logical-server Entra administrator for the preview and initial staging setup | Supplied to the preview command at runtime; not stored in tracked files |
| ACR pull user-assigned identity | `AcrPull` scoped to the staging registry | Defined in Bicep; attached to app registry configuration after app resources are added |
| Web Container App system identity | Web API calls; optional Data Protection/Key Vault access only if configured | Deferred until Web image and app definition exist |
| API Container App system identity | SQL runtime data access, approved Blob access, Service Bus sending, and only required AI/Search access | Deferred until API image and app definition exist |
| Worker Container App system identity | SQL job access, Service Bus receive/complete, approved Blob retrieval, and required processing services | Deferred until a Worker host and complete durable transport exist |
| Deployment/migration identity | Resource deployment and reviewed schema changes, separate from runtime identities | Must be selected and authorized before deployment |

Do not reuse the SQL administrator identity as an application runtime identity. Do not grant runtime identities `db_owner` to make migrations work.

## Planned least privilege

After the Container Apps and system-assigned identities exist, create contained Azure SQL users for the API and Worker and grant only the required database roles. The initial application plan is `db_datareader` and `db_datawriter`; confirm whether narrower custom permissions are practical for the actual application paths. Apply schema migrations through the separate deployment identity.

Grant Azure Storage data roles only to workloads that use the Azure Blob implementation. The current media and knowledge implementations are local-file based, so Blob permissions should wait until those adapters are implemented and verified. Grant `Azure Service Bus Data Sender` to API identities only for queues they publish to, and `Azure Service Bus Data Receiver` to Worker identities only for queues they consume. No Web SQL, Blob, or queue data permissions are planned by default.

`AcrPull` is a registry-scope role for the dedicated user-assigned image-pull identity. ACR admin authentication remains disabled. Store no credentials in Bicep parameters; use managed identity or Key Vault for runtime secrets that cannot use identity authentication.

For direct resource-group deployments of `main.bicep`, `infra/main.bicepparam` reads `ER_SQL_ENTRA_ADMIN_LOGIN`, `ER_SQL_ENTRA_ADMIN_OBJECT_ID`, and `ER_SQL_SERVER_NAME` from the local process environment. The staging preview script obtains the admin values from the active CLI user and generates a server name without writing them to a tracked parameter file.

## Preview behavior

`scripts/preview-staging-network.ps1` reads the active Azure CLI subscription and signed-in Entra user's display name/object ID, then passes those values directly to subscription-scope `what-if`. It does not write the values to files and does not submit a deployment. SQL contained users and runtime role assignments are not created by this preview.
