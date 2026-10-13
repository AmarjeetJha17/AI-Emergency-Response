# Configuration Reference

This reference records configuration names found in the current application and calls out target settings that are not yet implemented. Configure cloud settings through environment variables, Key Vault references, or managed identity-backed providers. Never commit real credentials or place them in `appsettings.Production.json`.

See the [environment matrix](environment-matrix.md) for workload ownership and the [resource inventory](azure-resource-inventory.md) for illustrative names.

ASP.NET Core environment variables use double underscores for nested keys, for example `AI__Endpoint` for `AI:Endpoint` and `ApiSettings__BaseUrl` for `ApiSettings:BaseUrl`.

The Web and API container images use .NET 10 and listen on HTTP port `8080` (`ASPNETCORE_HTTP_PORTS=8080`). This is a container runtime setting, not a secret. The Docker build excludes every `appsettings*.json` file, including base settings that may contain developer-machine paths or local connection values. Container Apps must inject application configuration and secret references at runtime; no settings file is included in the image.

## Database and identity

| Setting | Current use and deployment guidance |
|---|---|
| `ConnectionStrings:DefaultConnection` | Required by API outside the `Test` environment. Target Azure SQL with Microsoft Entra authentication and a contained database identity; verify the connection mode and permissions with the deployed identity. Do not use a production SQL username/password. |
| `ConnectionStrings:TestDatabase` | Selected only in `Test`; local/CI test configuration, not a deployed runtime setting. |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:SecretKey`, `Jwt:ExpirationMinutes` | API JWT validation and issuance. Keep issuer/audience as environment configuration; supply `SecretKey` through Key Vault or a managed secret source and never expose it to browser code. |
| `AdminSeed:Email`, `AdminSeed:Password`, `AdminSeed:FirstName`, `AdminSeed:LastName` | Admin seed options. The API invokes admin seeding at startup; review and constrain this bootstrap path before a pilot, and do not commit the password. Prefer a separately controlled initial-admin procedure. |

## AI, search, and maps

| Setting | Current use and deployment guidance |
|---|---|
| `AI:Provider`, `AI:Endpoint`, `AI:ApiKey`, `AI:Deployment`, `AI:Model`, `AI:EmbeddingDeployment` | `AI:Endpoint` and `AI:Deployment` are required by current AI service registration. `AI:ApiKey` is optional in the options type; use managed identity where the selected SDK/provider supports it. Keep model/deployment names environment-specific and estimate usage before real-model load testing. |
| `KnowledgeSearch:Endpoint`, `KnowledgeSearch:IndexName`, `KnowledgeSearch:TopK`, `KnowledgeSearch:MinimumRetrievalScore`, `KnowledgeSearch:MaximumContextChunks` | Azure AI Search endpoint/index and retrieval limits. Use an isolated index and service identity per cloud environment; this options type does not define an API-key setting. |
| `AzureMaps:Endpoint`, `AzureMaps:ClientId`, `AzureMaps:Enabled`, `AzureMaps:WeatherCacheMinutes`, `AzureMaps:SevereAlertCacheMinutes`, `AzureMaps:TrafficCacheMinutes` | Azure Maps/external-context configuration. Keep client identity and enablement environment-specific; test provider degradation separately from core incident readiness. |

## Storage, messaging, and Web/API connectivity

| Setting | Current use and deployment guidance |
|---|---|
| `IncidentMediaStorage:RootPath` | Local media implementation root only. Production still needs an Azure Blob `IIncidentMediaStorage` implementation with private containers and authorized API access. |
| `Knowledge:StorageRootPath`, `Knowledge:MaxFileSizeBytes`, `Knowledge:MaximumChunkCharacters`, `Knowledge:ChunkOverlapCharacters` | Current knowledge-document options; storage is local-file based. Add a private Blob implementation before deploying knowledge documents to Azure. |
| `Messaging:ServiceBus:FullyQualifiedNamespace` | Required by current Service Bus registration when the API environment is `Production`; use managed identity and distinct sender/consumer permissions. The current code chooses the in-memory bus in non-Production environments, including a conventional `Staging` environment. Add explicit provider selection before staging can validate the production topology. |
| `Messaging:Retry:*` | Retry settings for reliable messaging. Confirm the exact option fields in the selected deployment's configuration before setting values. |
| `ApiSettings:BaseUrl` | Required by Web server-side API clients and rendered into pages for browser SignalR/API interactions. For the initial pilot, it must resolve to an HTTPS API endpoint reachable from both the Web server and client browser. Keep API ingress externally reachable until the addressing model is separated and tested. |
| API CORS policy `WebClient` | Currently hard-coded to local origins in API `Program.cs`. Externalize the allowed Web origins and configure only actual HTTPS Web origins before cloud staging. No environment-driven CORS key is currently wired. |
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

## Configuration and secret checks

- Keep production secrets out of tracked settings files, container images, logs, and client-visible Web configuration.
- Use managed identity for Azure SQL, Service Bus, Storage, Key Vault, and supported AI/Search APIs; use Key Vault only for credentials that still require secret material.
- Keep runtime identities separate from migration/deployment identities.
- Check configuration keys against the actual options and service registrations when adding a setting; a documented target key is not active until code reads it.
