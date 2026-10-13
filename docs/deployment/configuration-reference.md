# Configuration Reference

This reference records configuration names found in the current application and calls out target settings that are not yet implemented. Configure cloud settings through environment variables, Key Vault references, or managed identity-backed providers. Never commit real credentials or place them in `appsettings.Production.json`.

See the [environment matrix](environment-matrix.md) for workload ownership and the [resource inventory](azure-resource-inventory.md) for illustrative names.

ASP.NET Core environment variables use double underscores for nested keys, for example `AI__Endpoint` for `AI:Endpoint` and `ApiSettings__BaseUrl` for `ApiSettings:BaseUrl`.

The Web and API container images use .NET 10 and listen on HTTP port `8080` (`ASPNETCORE_HTTP_PORTS=8080`). This is a container runtime setting, not a secret. The Docker build excludes every `appsettings*.json` file, including base settings that may contain developer-machine paths or local connection values. Container Apps must inject application configuration and secret references at runtime; no settings file is included in the image.

The Azure SQL target uses `ConnectionStrings__DefaultConnection` with the normal `tcp:<server>.database.windows.net,1433` hostname and `Authentication=Active Directory Default`. Private DNS inside the VNet resolves that hostname to the private endpoint. Do not configure the application with the endpoint's private IP address.

## Database and identity

| Setting | Current use and deployment guidance |
|---|---|
| `ConnectionStrings:DefaultConnection` | Required by API outside the `Test` environment. For Azure SQL use `Server=tcp:<sql-server>.database.windows.net,1433;Database=EmergencyResponse;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`. Private DNS provides the private route. `infra/apps.bicep` builds this connection for the API; a DBA must create and verify the API contained user before app startup. Do not use a SQL username/password. |
| `ConnectionStrings:TestDatabase` | Selected only in `Test`; local/CI test configuration, not a deployed runtime setting. |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:SecretKey`, `Jwt:ExpirationMinutes` | API JWT validation and issuance. Keep issuer/audience as environment configuration; supply `SecretKey` through Key Vault or a managed secret source and never expose it to browser code. |
| `AdminSeed:Email`, `AdminSeed:Password`, `AdminSeed:FirstName`, `AdminSeed:LastName` | Admin seed options. The API invokes admin seeding at startup; review and constrain this bootstrap path before a pilot, and do not commit the password. Prefer a separately controlled initial-admin procedure. |

## AI, search, and maps

| Setting | Current use and deployment guidance |
|---|---|
| `AI:Provider`, `AI:Endpoint`, `AI:ApiKey`, `AI:Deployment`, `AI:Model`, `AI:EmbeddingDeployment` | `AI:Endpoint` and `AI:Deployment` are required by current AI service registration. `AI:ApiKey` is optional; when absent, Azure OpenAI uses `DefaultAzureCredential`, which supports the Container App managed identity. The staging template reuses the existing Foundry deployments with synthetic data only. |
| `KnowledgeSearch:Endpoint`, `KnowledgeSearch:IndexName`, `KnowledgeSearch:TopK`, `KnowledgeSearch:MinimumRetrievalScore`, `KnowledgeSearch:MaximumContextChunks`, `KnowledgeSearch:CreateIndexIfMissing` | Azure AI Search endpoint/index and retrieval limits. Staging sets index auto-creation to false so the runtime identity only needs Search Index Data Contributor. `tools/EmergencyResponse.SearchIndexProvisioner` creates the isolated staging index during controlled private-network setup. |
| `AzureMaps:Endpoint`, `AzureMaps:ClientId`, `AzureMaps:Enabled`, `AzureMaps:WeatherCacheMinutes`, `AzureMaps:SevereAlertCacheMinutes`, `AzureMaps:TrafficCacheMinutes` | Azure Maps/external-context configuration. Keep client identity and enablement environment-specific; test provider degradation separately from core incident readiness. |

## Storage, messaging, and Web/API connectivity

| Setting | Current use and deployment guidance |
|---|---|
| `IncidentMediaStorage:RootPath` | Local media implementation root only. When `Storage:BlobServiceUri` is set, the API uses `AzureBlobIncidentMediaStorage` and does not use a local path. |
| `Storage:BlobServiceUri`, `Storage:IncidentMediaContainer`, `Storage:KnowledgeContainer` | Select the managed-identity Blob implementations for incident media and knowledge source documents. Blob containers are private and provisioned in Bicep. `DefaultAzureCredential` accesses them through the Container App system identity. |
| `Knowledge:StorageRootPath`, `Knowledge:MaxFileSizeBytes`, `Knowledge:MaximumChunkCharacters`, `Knowledge:ChunkOverlapCharacters` | Knowledge parsing/chunking options; cloud document storage uses the Blob adapter when `Storage:BlobServiceUri` is configured. |
| `Messaging:Provider`, `Messaging:ServiceBus:FullyQualifiedNamespace` | Set `Messaging:Provider=AzureServiceBus` in Staging to select the managed-identity Service Bus publisher and consumer. Production continues to use Service Bus. Only incident AI, visual analysis, duplicate detection, and external-context job types currently use durable Service Bus; other queues remain in-process. |
| `Messaging:Retry:*` | Retry settings for reliable messaging. Confirm the exact option fields in the selected deployment's configuration before setting values. |
| `ApiSettings:BaseUrl` | Required by Web server-side API clients and rendered into pages for browser SignalR/API interactions. For the initial pilot, it must resolve to an HTTPS API endpoint reachable from both the Web server and client browser. Keep API ingress externally reachable until the addressing model is separated and tested. |
| `Cors:AllowedOrigins` | API CORS origins are environment-driven. Set an indexed environment variable such as `Cors__AllowedOrigins__0` to the exact deployed Web HTTPS origin. Localhost origins are fallback only in Development/Test. An empty Staging list allows no browser origins until the Web FQDN is known. |
| SignalR `/hubs/incidents` | Hub is hosted by API and browser connects directly using a Web-issued SignalR token. Preserve this model for initial staging/pilot; verify authentication, group membership, CORS, reconnect, and snapshot refresh. |

## Secrets, Web runtime, and data protection

| Setting | Current use and deployment guidance |
|---|---|
| `KeyVault:VaultUri` | Optional Key Vault configuration hook in API and Web. Grant each managed identity only the required secret/key permissions. |
| `DataProtection:BlobUri`, `DataProtection:KeyIdentifier` | Optional Web Data Protection configuration; both must be set together. Uses Azure Blob persistence and Key Vault key protection with `DefaultAzureCredential`. Use a private shared key store across Web replicas. |
| `Syncfusion:LicenseKey` | Required by the Web startup path. Store as a secret and scope access to the Web workload. |
| `Logging:*` | ASP.NET Core logging levels. Cloud collection/export is not fully wired: current OpenTelemetry setup exports to console. Configure and verify Azure Monitor/Application Insights collection and alerting before pilot. |
| `AllowedHosts` | ASP.NET Core host filtering. Set to the deployed hostnames appropriate to each environment. |

## Container health endpoints

| Endpoint | Workload | Meaning |
|---|---|---|
| `/health/live` | Web and API | Process liveness; used to determine whether the container should be restarted. |
| `/health/ready` | Web | Web process can accept requests. It does not check API availability. |
| `/health/ready` | API | Critical SQL Server health check passes. AI provider failure is not a reason to mark core incident APIs unready. |

## Other environment-specific application options

The API also binds `Map:*`, `VideoFrameExtraction:*`, `ResourceRecommendation:*`, `SituationDetection:*`, `Predictive:*`, `Copilot:Actions:*`, `Copilot:Memory:*`, `Security:*`, `Retention:*`, and `Reliability:*`. Review their option classes before setting cloud values. File paths such as model and local storage paths must be replaced with container-safe paths or Blob-backed providers where applicable. Do not copy developer-machine paths into Container Apps.

For the current no-deployment preview, the SQL Entra administrator is obtained from the active Azure CLI user by `scripts/preview-staging-network.ps1`; the display name and object ID are passed to ARM at runtime and are not stored in Bicep parameter files or source control. Runtime identity, storage, and queue configuration remains pending app-resource deployment.

## Configuration and secret checks

- Keep production secrets out of tracked settings files, container images, logs, and client-visible Web configuration.
- Use managed identity for Azure SQL, Service Bus, Storage, Key Vault, and supported AI/Search APIs; use Key Vault only for credentials that still require secret material.
- Keep runtime identities separate from migration/deployment identities.
- Check configuration keys against the actual options and service registrations when adding a setting; a documented target key is not active until code reads it.
