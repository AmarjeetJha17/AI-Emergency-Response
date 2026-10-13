# Environment Matrix and Azure Hosting Strategy

## Decision status

This document records the target architecture for a controlled pilot. It does not assert that Azure resources exist or that a deployment has been verified. Azure CLI is installed locally, but there is no active Azure login, so no subscription or region was verified for this decision.

Related plans: [resource inventory](azure-resource-inventory.md), [configuration reference](configuration-reference.md), and [pilot entry criteria](pilot-entry-criteria.md).

Use Azure Container Apps for the first cloud deployment. Keep the MVC Web app, REST/SignalR API, and background Worker as separate container apps. Use Azure SQL Database, private Blob Storage, Azure Service Bus, managed AI/Search services, and Azure Key Vault. Use Azure Monitor and Log Analytics for cloud observability. Evaluate Azure SignalR Service only when staging measurements or multi-replica operation require SignalR scale-out.

The following resource groups are naming examples only:

- `rg-er-staging-centralindia`
- `rg-er-pilot-centralindia`

`centralindia` is a candidate example, not a selected deployment region. Choose a region only after reviewing the intended subscription, service availability, network and residency requirements, and cost. Apply the same isolation and naming rules to another region if that review selects one.

## Workload mapping

| Existing project or concern | Local | Staging and pilot target |
|---|---|---|
| `EmergencyResponse.Web` | Run the MVC app locally | Dedicated Web Container App |
| `EmergencyResponse.Api` | Run REST API and SignalR locally | Dedicated API Container App |
| Application, Domain, Infrastructure | Shared libraries in API/Web assemblies | Shared libraries included in the relevant images |
| Background processing | API currently hosts multiple workers | Dedicated Worker Container App; do not run the same hosted workers in every API replica |
| `ApplicationDbContext` | Local SQL Server | Separate Azure SQL database per cloud environment |
| Incident media and knowledge files | Local file system | Private Blob containers with authorized application access |
| Durable processing | In-memory queues locally; Service Bus integration is Production-only | Azure Service Bus with distinct publisher and consumer identities |
| Real-time updates | SignalR hosted by API | Keep the API hub reachable over HTTPS for the initial pilot; assess Azure SignalR Service for scale-out |
| AI, embeddings, and knowledge retrieval | Configured providers and local substitutes as supported | Managed Azure AI Foundry/Azure OpenAI and Azure AI Search services, isolated from local evaluation data |

The repository currently has five application projects (API, Web, Application, Domain, Infrastructure), but no Worker project. The API currently registers hosted workers, including outbox, data-retention, AI, visual-analysis, predictive-analysis, and external-context processing. Several work queues and both incident-media and knowledge-document storage use in-memory or local-file implementations. The Worker host, production-grade Blob providers, and complete cross-process durable queue path are prerequisites to deployment; they are not provided by this architecture document.

Service Bus is currently selected only when `ASPNETCORE_ENVIRONMENT` is `Production`; other environments register the in-memory message bus. Do not set Staging to `Production` just to activate Service Bus. Add an explicit environment/provider configuration path before staging reliability tests so staging can exercise the production queue topology without production-only startup behavior.

## Environment separation

| Environment | Purpose | Data and service boundary |
|---|---|---|
| Local | Daily development on Windows | Local SQL Server and local private storage; no cloud deployment required |
| Staging | Deployment, integration, and release-candidate validation | Dedicated resource group, database, private storage, queue namespace, and AI/Search resources or isolated test indexes; synthetic/test data only |
| Pilot/Production | Controlled use by authorized pilot participants | Separate resource group and data services; governed operational data, access, retention, and recovery controls |

Never reuse operational databases, uploads, credentials, or AI evaluation data across environments. Staging and pilot must have separate identity assignments and configuration. Local development must continue to work without Azure access.

## Network and SignalR decision

The Web app's `ApiSettings:BaseUrl` configures its server-side API clients. The same configured API base URL is rendered into the browser and used by `incident-realtime.js` to connect directly to `/hubs/incidents`; the Web app obtains the SignalR access token from its own `/Account/SignalRToken` route. The API currently has a CORS policy with localhost origins hard-coded in `Program.cs`.

For the initial pilot, keep the API HTTPS-reachable from both the Web app and the browser, preserve the direct browser-to-API SignalR model, and allow only the actual deployed Web origin in CORS. Externalize the CORS origin list before staging. Do not make the API private until server-to-server API addressing and browser SignalR addressing are separated and verified. SignalR remains a notification channel; clients refresh authoritative state from the API after reconnect.

## Identity and service access

Use managed identities for deployed workloads. Assign access by workload and service, not by broad environment membership.

| Identity | Planned minimum access |
|---|---|
| Web | Call required API routes; access the shared Data Protection Blob/Key Vault key only when configured; no incident-media access by default |
| API | Azure SQL runtime access, approved media/document Blob operations, Service Bus publish rights, and only required AI/Search access |
| Worker | Azure SQL access needed by jobs, Service Bus consume/complete rights, approved media/document Blob access, and only required AI/Search access |
| CI build | Build and publish images/artifacts; no data-plane access |
| Deployment/migration job | Time-limited environment-specific deployment and schema permissions, separate from runtime identities |
| Developer | Local development access; no shared production runtime credential |

For Azure SQL, target Microsoft Entra authentication with a contained database principal and least privilege. For Storage and Service Bus, use service-specific data-plane RBAC. Validate Azure Identity and SqlClient authentication against the selected hosting identity before directing pilot traffic.

## Health and operations

The API exposes `/health/live`, `/health/ready`, and `/health`. In current code, liveness excludes dependency checks and readiness selects checks tagged `ready`; only SQL Server is tagged `ready`. AI and worker health are reported by other checks but do not currently make `/health/ready` fail. This supports the requirement that an AI outage alone should not remove core incident intake from readiness. Map liveness and readiness separately in Container Apps, and define an independent Worker readiness/health signal when the Worker host exists. Choose startup probe delays based on measured startup, migrations, and initialization.

Start with small CPU/memory allocations and explicit min/max replica limits. Configure subscription budgets and cost alerts before provisioning. Estimate Container Apps, SQL, Storage, Service Bus, AI/Search, Key Vault, and monitoring costs using the selected region and usage assumptions. Avoid premium networking, high availability tiers, and large real-model load tests until justified and costed. Do not treat local TestServer measurements as cloud capacity measurements.
