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

Inject configuration at runtime through Container Apps environment variables or managed identity/Key Vault integration. At minimum, Web needs `ApiSettings__BaseUrl` and `Syncfusion__LicenseKey`. API needs `ConnectionStrings__DefaultConnection`, valid JWT settings, and the service configuration required by its registrations. Production additionally requires `Messaging__ServiceBus__FullyQualifiedNamespace`; runtime identities and data-plane permissions must be configured before use. See [configuration-reference.md](configuration-reference.md) for the current configuration contract and known deployment gaps.

Do not bake credentials into Docker build arguments, image layers, appsettings files, or container labels. The Bicep foundation disables ACR admin auth, Storage account-key auth, and Service Bus local auth; workload role assignments are intentionally a later deployment step.

## Local container connectivity

Use disposable local dependencies and a dedicated test database. When a container connects to a service running on Windows, use `host.docker.internal` as the host name; `localhost` inside the container refers to the container itself. Linux containers cannot use Windows Integrated Security to connect to SQL Server. Configure SQL authentication only for a disposable local/test database, and never pass production credentials or data to smoke tests.

The API expects an initialized schema and successful database access before it serves health endpoints. Its automatic migration path is limited to the `Test` environment; production/staging schema changes must be applied separately using the reviewed migration process. Do not report a successful API smoke test if SQL is unavailable.

## Background processing

The API currently hosts both the outbox publisher and multiple process-local background workers. The durable Service Bus consumer handles incident AI, visual analysis, duplicate detection, and external-context messages, but not every current job type is yet routed through that shared transport. Keep the existing single-host worker behavior until all jobs have a verified durable cross-process path; then introduce a dedicated Worker host and image, removing duplicate consumers from API replicas.

## Health and local smoke checks

Build from the repository root:

```powershell
docker build -f docker/Dockerfile.web -t emergencyresponse-web:local .
docker build -f docker/Dockerfile.api -t emergencyresponse-api:local .
```

Run each image with its required local configuration, map host ports to container port `8080`, and request `/health/live` and `/health/ready`. Web also requires a configured Syncfusion license value at startup. API requires SQL Server and its required application settings; test only against a disposable database. Verify the runtime process is non-root with `docker image inspect`/`docker exec`, and verify no secrets or local data were included in the image.

## Azure foundation

`infra/main.bicep` defines the initial staging/pilot resource foundation: Log Analytics, a Container Apps environment, ACR, private Blob containers, Key Vault, and Service Bus with the four durable queue names currently mapped in the API publisher/consumer. It does not create SQL, application Container Apps, private networking, or identity role assignments. Public endpoint reachability is enabled in this foundation, while anonymous Blob access and local key-based authentication are disabled; apply network restrictions and least-privilege roles before an operational pilot.

The template is compiled locally as validation only. It does not prove resource availability, pricing, policy compatibility, or create Azure resources.
