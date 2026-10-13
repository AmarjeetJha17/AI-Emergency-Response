# Containerization

## Images

- `EmergencyResponse.Web` is built from `docker/Dockerfile.web`.
- `EmergencyResponse.Api` is built from `docker/Dockerfile.api`.
- Both Dockerfiles use the repository root as their build context so shared projects and configuration used by restore are available.
- Worker deployment is deferred. Several AI, knowledge, predictive, and external-context jobs still use process-local queues. Splitting those consumers into another process before all producers and consumers use a shared durable transport could lose work.

## Runtime

- .NET 10 multi-stage builds compile and publish with the SDK image, then run on the ASP.NET runtime image.
- Both images listen on HTTP port `8080` and run as the non-root `$APP_UID` user.
- `/health/live` is the liveness endpoint. `/health/ready` is the readiness endpoint. The API readiness endpoint checks its SQL dependency; Web health reports whether the Web process can serve requests and does not validate API availability.
- The API performs seed operations during startup and does not automatically apply production migrations. Apply reviewed migrations before starting a new API deployment.

## Configuration and secrets

The Docker build excludes all `appsettings*.json` files, secret files, environment files, local media and knowledge data, and build/test output. This also prevents developer-machine paths and local connection settings from entering Linux images. Supply required application configuration at runtime; no settings file is baked into either image.

Inject configuration at runtime through Container Apps environment variables, managed identity, and Key Vault references. Web needs `ApiSettings__BaseUrl`, `Syncfusion__LicenseKey`, and persistent Data Protection Blob/Key Vault settings. API needs `ConnectionStrings__DefaultConnection`, JWT configuration, `Messaging__Provider=AzureServiceBus`, its Service Bus namespace, private Blob settings, AI/Search endpoints, and the deployed Web origin in `Cors__AllowedOrigins__0`. The `infra/apps.bicep` template defines these settings and app-level Key Vault references; create the referenced secrets before activating revisions. See [configuration-reference.md](configuration-reference.md).

Do not bake credentials into Docker build arguments, image layers, appsettings files, or container labels. The Bicep foundation disables ACR admin auth, Storage account-key auth, and Service Bus local auth; workload role assignments are intentionally a later deployment step.

## Local container connectivity

Use disposable local dependencies and a dedicated test database. When a container connects to a service running on Windows, use `host.docker.internal` as the host name; `localhost` inside the container refers to the container itself. Linux containers cannot use Windows Integrated Security to connect to SQL Server. Configure SQL authentication only for a disposable local/test database, and never pass production credentials or data to smoke tests.

The API expects an initialized schema and successful database access before it serves health endpoints. Its automatic migration path is limited to the `Test` environment; production/staging schema changes must be applied separately using the reviewed migration process. Do not report a successful API smoke test if SQL is unavailable.

## Background processing

The API currently hosts both the outbox publisher and multiple process-local background workers. The durable Service Bus consumer handles incident AI, visual analysis, duplicate detection, and external-context messages, but not every current job type is yet routed through that shared transport. Staging API replicas are therefore capped at one, and the Worker remains undeployed. A dedicated Worker host and image must wait until all job paths use a verified durable cross-process transport.

## Health and local smoke checks

Build from the repository root:

```powershell
docker build -f docker/Dockerfile.web -t emergencyresponse-web:local .
docker build -f docker/Dockerfile.api -t emergencyresponse-api:local .
```

Run each image with its required local configuration, map host ports to container port `8080`, and request `/health/live` and `/health/ready`. Web also requires a configured Syncfusion license value at startup. API requires SQL Server and its required application settings; test only against a disposable database. Verify the runtime process is non-root with `docker image inspect`/`docker exec`, and verify no secrets or local data were included in the image.

## Azure foundation

`infra/main.bicep` defines Log Analytics, a VNet-integrated Container Apps environment, ACR, private Blob containers and Blob private endpoint, Key Vault, Service Bus with four queue definitions, a separate private Azure AI Search service, an ACR pull identity, and Azure SQL with Entra-only authentication and a private endpoint/DNS link. `infra/apps.bicep` defines external HTTPS Web/API Container Apps and workload RBAC. API ingress remains external for the current browser SignalR route; the Worker is intentionally absent. SQL public network access is disabled. `infra/staging-preview.bicep` wraps the foundation for subscription-scope preview. The VNet ranges, region, and SQL/Search SKUs require overlap, availability, and cost review.

The API requires Service Bus sender/receiver access because its current topology both publishes and consumes four durable queue types. Other job paths still use local channels. Web/API images, identities, and runtime authorization have not been deployed or tested. The current Bicep disables public access to Blob and Search; SQL public access, Blob shared keys, ACR admin auth, and Service Bus local auth are disabled. Service Bus remains network reachable but requires managed identity. Review role assignments, quotas, and costs before provisioning.

The preview script `scripts/preview-staging-network.ps1` resolves the active Azure CLI user and runs only `az deployment sub what-if`. It does not create resources. Compilation and `what-if` do not prove resource availability, pricing, SQL private DNS resolution, runtime authorization, container startup with SQL, or SignalR behavior.
