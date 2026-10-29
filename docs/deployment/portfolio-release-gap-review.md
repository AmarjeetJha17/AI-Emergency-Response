# Portfolio release gap review — 29 October 2026

Status: local changes prepared; no Azure resource changes, SQL writes, inference, deployment, commit or push performed. Existing seeding-tool changes are preserved and are outside this release-gap patch.

## Findings and fixes

### SignalR CORS

The deployed API had no `Cors__...` environment settings. A trusted-origin OPTIONS request reached Kestrel and returned 204 without CORS headers. Unauthenticated negotiate POST returned 401 Bearer. This establishes an origin configuration failure rather than authentication intercepting OPTIONS.

The isolated API template now requires a nonempty `webOrigin` and supplies `Cors__AllowedOrigins__0`. Its default is the explicitly approved Web origin, without a trailing slash:

```text
https://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io
```

`WebClientCors` retains exact-origin matching, credential support for SignalR and the existing HTTP methods/headers. Wildcard and malformed origin settings are rejected. Outside Development/Test, absent configuration still permits no cross-origin access. Middleware ordering remains CORS, rate limiting, authentication, authorization, then the authenticated hub mapping. The hub retains `[Authorize]`; CORS never substitutes for authorization.

The SQL-free HTTP pipeline tests use the real hub, shared production CORS registration and JWT validation: no token/invalid token yields 401; a valid test token negotiates successfully; trusted preflight succeeds without credentials; untrusted preflight gets no CORS grant. The existing unit tests additionally cover GET/POST/DELETE transports, required headers, missing configuration and wildcard rejection.

### Active incident counts

`DashboardService` excludes Closed and Rejected. `EmergencyMapService` previously excluded only Closed. Four rejected synthetic reports therefore inflated the map's active count from 16 to 20.

The map now excludes both terminal statuses. Resolved remains active according to the existing dashboard/domain convention; this change does not redefine it as Closed. The map still requires a location, so its wording now says **active incidents with mapped locations**. A regression test compares actual dashboard and map results for every status, then verifies the legitimate difference when an active incident has no location. No records or statuses are edited.

### Evidence upload wording

The backend already skips the visual-analysis queue when `AI:Enabled=false` and returns `analysisStatus: Disabled`. The view nevertheless promised automatic analysis and its success handler always claimed analysis was queued.

The authorized incident-detail API response now carries nullable `VisualAnalysisEnabled`, derived from the API's actual AI options rather than stored incident data or a duplicate Web configuration flag. The Web displays enabled, disabled or unconfirmed wording. Missing capability from an older API defaults to unconfirmed. Accepted evidence-only storage does not imply that file-security checks will permit every upload.

After an upload, only an explicit `Queued` response claims queueing; `Disabled` reports evidence-only storage; missing/unexpected status stays unconfirmed. The response is not echoed into HTML. Existing authorization, rate limits, upload validation, malware-scanning fail-closed behavior and the backend queue guard are unchanged. No live upload was performed.

### Forecast failure and remaining blocker

The browser calls `/Dashboard/IncidentVolume`; the Web makes a server-side authenticated request to `/api/predictive/incident-volume?horizon=24`. This is independent of browser CORS.

Read-only aggregate queries over the existing API console logs found:

| Marker | Count in the inspected 24-hour window |
|---|---:|
| `DllNotFoundException` with `MklImports` | 2 |
| `FftUtils` type initialization | 2 |
| Missing `libiomp5.so` shared library | 2 |
| Forecast worker failure | 2 |

The inspected logs establish a Linux native-runtime dependency failure. `Microsoft.ML.TimeSeries` 4.0.3 resolves `Microsoft.ML.Mkl.Redist` 4.0.3. Its cached Linux assets contain `libMklImports.so` but no `libiomp5.so`; the API Dockerfile uses the ASP.NET runtime image without installing the missing OpenMP dependency. Microsoft documents the TimeSeries/MKL native dependency chain in [Install extra ML.NET dependencies](https://learn.microsoft.com/en-us/dotnet/machine-learning/how-to-guides/install-extra-dependencies).

The native exception can become an API 500, which the Web previously collapsed into a generic 503. This evidence does not establish that every historical 503 has the same cause. It does not justify changing SQL permissions. The current source's initial SQL query translated in a local connection-blocking diagnostic; that was not a successful database or native-library test.

The Web now emits only fixed operation names, diagnostic codes and optional upstream HTTP status, with no exception object, response body, token or connection string:

| Code | Meaning |
|---|---|
| FC001 | Upstream HTTP failure; its status is logged separately |
| FC002 | HTTP connectivity failure with no upstream status |
| FC003 | Invalid or absent JSON payload |
| FC004 | Timeout/cancellation not initiated by the caller |

401/403 remain 401/403. Caller cancellation propagates. Other failures remain unavailable rather than becoming fabricated/empty successful forecasts. Tests verify response and log redaction.

**Unresolved:** no Docker/runtime dependency change, Linux native-load validation or forecast execution was performed. Forecasting is not declared healthy. A separate Linux-image dependency fix must select a trusted, compatible OpenMP runtime and verify loader resolution inside the final non-root image. Do not blindly copy an old Intel installation recipe, add arbitrary library symlinks, upgrade unrelated ML/EF packages or infer success from a Windows build. `AI__Enabled=false` does not disable local ML.NET forecasting.

## Forecast side effects and safe validation

| Path | Behavior | Allowed in a strictly read-only check? |
|---|---|---|
| Web/API health/readiness | Health and dependency checks; does not exercise ML forecasting | Yes; not evidence of forecast health |
| Dashboard summary and map snapshot | Database reads | Yes |
| `GET /api/predictive/hotspots` | Read-only incident activity calculation, not a model forecast | Yes; may still fail if controller/service initialization fails |
| `GET /api/predictive/incident-volume` and Web proxy | Runs local forecasting and saves an `OperationalForecast` when available | **No** |
| `POST /api/predictive/regenerate` | Forecasting and persistence | **No** |
| `GET /api/predictive/resource-demand` | Runs local model forecasting, even though it does not save results | **No model execution in this task** |
| `ForecastGenerationBackgroundWorker` | Runs on startup and hourly; can save forecasts and create notifications | Not a read-only validation mechanism |

No forecast endpoint was invoked during this investigation. Aggregated historical logs and local fake/intercepted tests are the available evidence. A full dashboard reload automatically calls its forecast proxy; block those requests only in the temporary test browser or avoid the page when enforcing strictly read-only checks.

An authorized operator can inspect history and **their own** permissions with read-only SQL, without generating a forecast:

```sql
SELECT COUNT_BIG(*) AS IncidentCount,
       MIN(ReportedAtUtc) AS EarliestReportUtc,
       MAX(ReportedAtUtc) AS LatestReportUtc
FROM dbo.Incidents;

SELECT HAS_PERMS_BY_NAME(N'dbo.Incidents', N'OBJECT', N'SELECT') AS CanReadIncidents,
       HAS_PERMS_BY_NAME(N'dbo.OperationalForecasts', N'OBJECT', N'INSERT') AS CanInsertForecasts;
```

These checks were not executed in Azure as part of this task. Permission results apply only to the connected principal; an administrator's result does not verify the API managed identity. Do not grant permissions, impersonate users or modify data to complete this investigation. Current training requirements are 336 observations and 20 non-zero buckets; a report count alone does not prove those conditions hold.

## Exact release-gap file inventory

Included existing local CORS preparation:

- `infra/portfolio-demo-api.bicep`
- `src/EmergencyResponse.Api/Program.cs`
- `src/EmergencyResponse.Api/Configuration/WebClientCors.cs`
- `tests/EmergencyResponse.UnitTests/Configuration/WebClientCorsTests.cs`
- `docs/deployment/signalr-cors-verification.md` (updated with follow-up evidence)

New or changed for this task:

- `src/EmergencyResponse.Api/Controllers/IncidentsController.cs`
- `src/EmergencyResponse.Application/DTOs/Incidents/IncidentResponse.cs`
- `src/EmergencyResponse.Infrastructure/Map/EmergencyMapService.cs`
- `src/EmergencyResponse.Web/Controllers/DashboardController.cs`
- `src/EmergencyResponse.Web/Models/Incidents/EvidenceAnalysisNotice.cs`
- `src/EmergencyResponse.Web/Views/Incidents/Details.cshtml`
- `src/EmergencyResponse.Web/Views/Map/Index.cshtml`
- `src/EmergencyResponse.Web/wwwroot/js/emergency-map.js`
- `src/EmergencyResponse.Web/wwwroot/js/evidence-analysis-status.js`
- `tests/EmergencyResponse.UnitTests/Configuration/IncidentVisualCapabilityTests.cs`
- `tests/EmergencyResponse.UnitTests/Map/EmergencyMapServiceTests.cs`
- `tests/EmergencyResponse.UnitTests/Web/EvidenceAnalysisNoticeTests.cs`
- `tests/EmergencyResponse.UnitTests/Web/DashboardForecastTests.cs`
- `tests/EmergencyResponse.IntegrationTests/Health/SignalRCorsPipelineTests.cs`
- `scripts/tests/evidence-analysis-status.test.cjs`
- `docs/deployment/portfolio-release-gap-review.md`

Local build/test output is under ignored `TestResults/`. The pre-existing hosted seeder, principal scripts and related tests/documents were not edited.

## Validation results

| Check | Result |
|---|---|
| Full Release unit suite | 676 passed, 0 failed, 0 skipped |
| SQL-free SignalR HTTP pipeline tests | 5 passed, 0 failed |
| JavaScript UI checks (dashboard, resources and evidence status) | 31 passed, 0 failed |
| API Release build, non-incremental | Passed; 0 errors, 5 warnings |
| Web Release build, non-incremental | Passed; 0 errors, 5 warnings |
| API-only Bicep compilation | Passed |
| `git diff --check` | Passed |

The complete SQL Server integration suite, hosted forecast endpoints, live uploads and Linux native-loader test were not run. Initial concurrent test builds encountered a local shared-output lock; the reported successful results are from sequential builds/runs after correcting a test namespace import. These are local preparation results, not post-deployment verification.

Relevant commands from the repository root:

```powershell
rtk proxy dotnet test tests/EmergencyResponse.UnitTests/EmergencyResponse.UnitTests.csproj -c Release --no-restore
rtk proxy dotnet test tests/EmergencyResponse.IntegrationTests/EmergencyResponse.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~SignalRCorsPipelineTests
rtk proxy node --test scripts/tests/evidence-analysis-status.test.cjs scripts/tests/dashboard-ui.test.cjs scripts/tests/resource-command-center.test.cjs
```

### Warnings, reported separately

No package upgrades were made. The observed warning set is pre-existing in unchanged dependencies/source:

| Warning | Location/package | Detail |
|---|---|---|
| CS8601 | `EmergencyCopilotService.cs:127` and `:202` | Possible null reference assignment |
| CS8602 | `IncidentAIJobProcessor.cs:200` | Possible null reference dereference |
| NU1902 | `System.Security.Cryptography.Pkcs` 9.0.18 and 10.0.10 | Moderate: GHSA-2fmv-rv2c-59x7 |
| NU1903 | `System.Security.Cryptography.Pkcs` 9.0.18 and 10.0.10 | High: GHSA-846f-f764-76mx |
| NU1903 | `System.Security.Cryptography.Xml` 8.0.4 and 10.0.10 | High: GHSA-7hqp-7c5m-4vq6 |
| NU1903 | `System.Security.Cryptography.Xml` 8.0.4 and 10.0.10 | High: GHSA-c399-9853-6jmj |
| NU1903 | `System.Security.Cryptography.Xml` 8.0.4 and 10.0.10 | High: GHSA-r955-8q5j-3h83 |
| NU1903 | `System.Security.Cryptography.Xml` 8.0.4 | High: GHSA-vrv5-2jg9-4mmr |

Warning repetition varies by project and restore/build cache; the clean API and Web build totals above each include two PKCS audit warnings and three compiler warnings. Test-build logs also reported the XML and test dependency versions listed here. Dependency vulnerability remediation remains a separate release risk, not a reason to weaken tests or permissions.

## Proposed deployment plan — separate review and approval required

1. Resolve and review the Linux native-library blocker separately, or explicitly approve leaving forecasting unavailable with honest UI. Account for the existing forecast worker's startup/hourly persistence and notification behavior before any runtime change that makes forecasting work. No inference or hosted forecast test is authorized by this document.
2. Build new immutable API and Web artifacts from the reviewed tree. Do not overwrite the old tags or assume the currently pinned images contain these changes. Verify native loader dependencies in the final Linux image without running a model, publishing secrets or contacting Azure SQL.
3. Verify artifact digests before updating any image allowlists or deployment scripts. Retain Central US placement, the East US 2 registry, API/Web pull identities, AI-disabled settings, native secret references and current Data Protection behavior.
4. Prepare a narrowly scoped API update for the new image, revision and exact approved CORS origin, then a Web update for the reviewed image/revision. Obtain property-level previews with zero unrelated actionable changes. Inspect the added CORS environment entry by **name**, not merely an array index.
5. Preserve existing secret values. The native-secrets API helper requests fresh signing/admin inputs; do not run it unchanged as if it were a secret-preserving rollout. Any required JWT rotation needs separate review because it invalidates sessions. Never retrieve or print existing secret values as a workaround.
6. After approval, roll out API first, then Web. The new Web treats a missing capability from an older API as unconfirmed, but API-first provides the authoritative flag immediately. Verify readiness, exact image/revision and unchanged AI/network/identity settings.
7. Perform trusted/untrusted OPTIONS checks and unauthenticated/authorized hub negotiation as described in the linked CORS runbook. Read dashboard/map snapshots and confirm the shared active-state rule; map counts may legitimately be lower for incidents without locations. Inspect disabled-AI wording without uploading files or invoking analysis.
8. Read existing logs for fixed forecast diagnostic codes. Do not call forecast-generating endpoints, start a worker manually, change permissions, seed data or perform migrations as a verification shortcut. Stop if preview, readiness or authorization checks fail.

This is a proposal, not authorization to deploy.
