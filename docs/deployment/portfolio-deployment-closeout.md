# Azure portfolio deployment closeout

Status recorded: 13 November 2026
Scope: the synthetic-data portfolio demonstration in Azure Container Apps. This is not approval for emergency operations or production use.

## Evidence labels

- **Verified in the deployment checks:** read-only Azure resource/revision inspection and HTTP checks recorded during the stabilization rollout.
- **Maintainer-reported:** checks or database observations reported by the maintainer; they were not independently repeated for this closeout.
- **Pending application acceptance:** a user-facing workflow that still needs a controlled, authenticated check. These items do not indicate that the Azure infrastructure is unavailable.

## Deployed architecture and resources

The browser uses the ASP.NET Core Web application and its API. The API uses EF Core with Azure SQL and managed identity. Both Container Apps pull images from Azure Container Registry using their user-assigned identities. Blob Storage backs application file workflows and the Web Data Protection key ring. Log Analytics receives platform diagnostics. The API and Web are in Central US; the registry is in East US 2.

| Component | Deployed resource | Region / purpose |
|---|---|---|
| Resource group | `rg-er-portfolio-demo-centralus` | Central US application resources |
| Container Apps environment | `cae-er-portfolio-demo` | Shared Web/API environment |
| Web Container App | `ca-er-web-portfolio` | Public ASP.NET Core Web application |
| API Container App | `ca-er-api-portfolio` | REST API and SignalR hub |
| Registry | `acerportfoliobxwiyolb.azurecr.io` | East US 2 image registry |
| Web pull identity | `id-er-portfolio-web` | Registry pull and Web storage access |
| API pull/runtime identity | `id-er-portfolio-api` | Registry pull and API data access |
| SQL server / database | `sql-er-demo-qqgqkekj.database.windows.net` / `EmergencyResponse` | Relational application data |
| Storage account | `sterportfolioqqgqkekj` | Private Blob storage |
| Key Vault | `kv-er-portfolio-qqgqkekj` | Restricted vault; Web Data Protection wrapping is not used in this demo mode |
| Log Analytics workspace | `log-er-portfolio-qqgqkekj` | Container Apps platform logs |
| Virtual network | `vnet-er-portfolio-demo` | Existing portfolio network resource |

The apps use external HTTPS ingress on port 8080. Both are in **Single** revision mode and the latest ready revision receives 100% traffic. The API CORS allowlist contains only the deployed Web origin. Registry authentication uses managed identities; credentials and secret values are intentionally omitted here.

## Endpoints

- [Portfolio Web application](https://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io/)
- [API readiness](https://ca-er-api-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io/health/ready)
- [API liveness](https://ca-er-api-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io/health/live)
- [Repository](https://github.com/AmarjeetJha17/AI-Emergency-Response)

## Verification register

### Verified in deployment checks

- Both apps report successful provisioning in Central US; their stabilization revisions were ready/healthy and assigned 100% traffic when inspected.
- The Web homepage and Web/API liveness and readiness endpoints returned HTTP 200.
- The approved Web-origin SignalR negotiate **OPTIONS preflight** returned HTTP 204 with the exact allowed origin, credential support, POST, and the requested SignalR headers.
- An unauthenticated SignalR negotiate POST returned HTTP 401, confirming that the hub still requires authentication. This does not verify an authenticated connection.
- API configuration was inspected with `AI__Enabled=false`. Forecast and retention background workers and Azure Maps external context are explicitly disabled in the portfolio configuration. This establishes configuration, not successful behavior of every unrelated feature.
- API image was pinned by OCI digest `sha256:65209ab91ac166980b9ca5d76a3aa95087c70ff6cdb522d360088c5a55669c4b`; Web image digest recorded for the stabilization build was `sha256:309499981746ab8136d77a25838c2e508c47530af078b60d00b606ef21b29f54`.

### Maintainer-reported

- Administrator login succeeded.
- Azure SQL connectivity was verified.
- SSMS showed 24 incident records labeled as synthetic.

These are recorded as maintainer-reported evidence; they were not repeated as part of this documentation task.

### Pending application acceptance

- Sign in with the authorized demo administrator and verify that the dashboard displays the expected incident data. Confirm that dashboard and Live Map counts are understandable and consistent with their documented status filters.
- In the same authorized browser session, complete SignalR negotiation and verify the connection/reconnect status. The preflight and expected unauthenticated 401 do not prove authenticated hub negotiation.
- Exercise a safe read-only dashboard refresh and inspect an incident's details, location, and history. Do not submit or edit an incident solely for acceptance testing.

The dashboard-data visibility check is an **application-acceptance item**, not an infrastructure failure: the apps, ingress, health endpoints, and database connectivity have separate evidence above. Until these checks pass, do not claim the complete dashboard workflow or authenticated real-time updates are verified.

## Feature and data limits

- **AI inference is disabled** in the deployed API (`AI__Enabled=false`). Do not claim live Azure OpenAI/Foundry inference, computer-vision analysis, AI-generated summaries, or predictive AI results. Do not enable AI or call paid models as part of routine demo verification.
- Forecast endpoints are distinct from the forecast background worker. The portfolio setting disables background generation; it does not establish that explicit forecast endpoints are disabled. Avoid invoking forecast endpoints during closeout checks because forecast workflows may persist results.
- The 24 imported incidents are synthetic examples. They are not live emergencies, verified casualty reports, dispatch instructions, or operational data. Use synthetic accounts and data only; never enter real personal, health, or incident information.
- The Web app's `PortfolioDemo` Data Protection mode persists keys in Blob Storage **without Key Vault wrapping**. This is a reduced-security configuration for synthetic demonstration data only. It is unsuitable for real users, sensitive data, or production. Treat demo cookies and data as disposable; do not reuse the configuration as a production baseline.
- The authenticated SignalR path and complete dashboard workflow remain pending above. This deployment is a portfolio demonstration, not an emergency-services system.

## Intermittent-demo operations and cost care

The apps were configured with a maximum of one replica and a minimum of zero in the reviewed stabilization baseline. On an intermittent demo, scaling to zero when idle can reduce app replica consumption in a Consumption workload, but produces a cold start and makes the apps unavailable until traffic starts them again. Confirm the current environment/workload plan and scale settings before relying on this behavior. Single revision mode means an update shifts the app to a new revision rather than keeping a second revision available for traffic rollback; retain the previous image digest and use the reviewed revision-update procedure if rollback is needed.

Starting the API can run its normal startup path, including database initialization/reference/admin seeding checks. Those paths are intended to be idempotent, but a restart is not a guaranteed zero-SQL-write event. Forecast background generation, retention cleanup, Azure Maps context, and AI-driven queue work have portfolio opt-outs; explicitly requested forecast operations remain a separate risk. Check the current worker settings before every release and avoid treating a restart as a read-only action.

For cost monitoring, review Azure Cost Management by resource group and service, configure a budget with actual/forecast alerts, and check usage after a demo session. Cost data and budget evaluation are delayed, so alerts are not an immediate hard spending cap. Scaling apps to zero does not imply every shared or data resource stops accruing cost; include the Container Apps environment/profile, SQL, storage, registry, logs, and network resources in review. See Microsoft's guidance on [Container Apps billing](https://learn.microsoft.com/en-us/azure/container-apps/billing) and [creating Cost Management budgets](https://learn.microsoft.com/en-us/azure/cost-management-billing/costs/tutorial-acm-create-budgets).

Before a planned demo, start/enable the apps only through the approved operator procedure, allow for cold starts and serverless SQL resume time, and confirm readiness before inviting users. After the demo, scale to zero or stop the apps only if the configured plan and operational procedure support it; verify the public endpoints are no longer expected to serve traffic. Do not delete shared SQL, Storage, Key Vault, registry, or network resources as a routine shutdown step.

## Related deployment documentation

- [Stabilization rollout and configuration safeguards](portfolio-stabilization-rollout.md)
- [SignalR CORS verification procedure](signalr-cors-verification.md)
- [Azure resource inventory](azure-resource-inventory.md)
- [Cost-conscious portfolio deployment notes](cost-conscious-portfolio-demo.md)
