# Local Setup and Synthetic Portfolio Demo

This is a local engineering demonstration, not an operational pilot. Use only fabricated incidents/media and an isolated database. Missing dependencies or failed steps are recorded as pending/failed in the [evidence register](feature-verification.md), not bypassed.

## 1. Prerequisites and configuration

- .NET 10 SDK and the repository-local EF tool; SQL Server **2025** for the native-vector schema.
- A SQL login permitted to create/migrate a disposable local database. Do not use Azure production credentials.
- A valid Syncfusion license for Web startup, held in Web User Secrets.
- Configured Azure AI endpoint, chat and embedding deployments, and permitted credentials. The normal Development host does not supply a fake AI provider. Missing Search configuration prevents knowledge-based Copilot evidence; do not invent citations.
- Optional Maps/context configuration; video processing needs `ffmpeg`/`ffprobe`, which are not included in the current API image.

Run from your clone's repository root, not from a hard-coded filesystem path:

```powershell
$ErrorActionPreference = 'Stop'
dotnet tool restore
dotnet restore AI-Emergency-Response.slnx
dotnet build AI-Emergency-Response.slnx --configuration Release
dotnet dev-certs https --trust
```

Every native command must exit successfully before continuing. User Secrets IDs already exist for API and Web. Configure your own values without copying credentials into tracked JSON:

```powershell
$sqlPassword = Read-Host 'Password for disposable local SQL login' -AsSecureString
$sqlPasswordText = [System.Net.NetworkCredential]::new('', $sqlPassword).Password
$demoConnection = "Server=127.0.0.1,1433;Database=EmergencyResponse_PortfolioDemo;User Id=sa;Password=$sqlPasswordText;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
dotnet user-secrets set 'ConnectionStrings:DefaultConnection' $demoConnection --project src/EmergencyResponse.Api
dotnet user-secrets set 'IncidentMediaStorage:RootPath' (Join-Path (Get-Location) 'private-media/portfolio') --project src/EmergencyResponse.Api
dotnet user-secrets set 'Knowledge:StorageRootPath' (Join-Path (Get-Location) 'private-knowledge/portfolio') --project src/EmergencyResponse.Api
```

`sa` is a disposable-local example, not a deployed runtime identity. Supply a valid local administrator password and a random JWT signing key of at least 32 characters using variables populated from secure prompts:

```powershell
$adminPassword = [System.Net.NetworkCredential]::new('', (Read-Host 'New synthetic admin password' -AsSecureString)).Password
$jwtKey = [System.Net.NetworkCredential]::new('', (Read-Host 'Local JWT signing key, at least 32 characters' -AsSecureString)).Password
$syncfusionKey = [System.Net.NetworkCredential]::new('', (Read-Host 'Your Syncfusion license' -AsSecureString)).Password
dotnet user-secrets set 'AdminSeed:Email' 'portfolio-admin@test.local' --project src/EmergencyResponse.Api
dotnet user-secrets set 'AdminSeed:Password' $adminPassword --project src/EmergencyResponse.Api
dotnet user-secrets set 'Jwt:SecretKey' $jwtKey --project src/EmergencyResponse.Api
dotnet user-secrets set 'Syncfusion:LicenseKey' $syncfusionKey --project src/EmergencyResponse.Web
```

Configure non-secret provider endpoints/deployment names with your own permitted values:

```powershell
dotnet user-secrets set 'AI:Endpoint' '<your-Azure-AI-HTTPS-endpoint>' --project src/EmergencyResponse.Api
dotnet user-secrets set 'AI:Deployment' '<chat-deployment>' --project src/EmergencyResponse.Api
dotnet user-secrets set 'AI:Model' '<model-name>' --project src/EmergencyResponse.Api
dotnet user-secrets set 'AI:EmbeddingDeployment' '<embedding-deployment>' --project src/EmergencyResponse.Api
dotnet user-secrets set 'KnowledgeSearch:Endpoint' '<your-Azure-Search-HTTPS-endpoint>' --project src/EmergencyResponse.Api
```

Replace placeholders before running. The AI client uses an externally configured `AI:ApiKey` or Azure Identity credentials. Search configuration and access must match [configuration reference](../deployment/configuration-reference.md). Do not provision cloud resources merely to finish screenshots. User Secrets are local plaintext configuration, not a production secret store. Environment variables override User Secrets.

## 2. Explicit migrations and startup

The design-time factory does not load API User Secrets. Pass the intended connection explicitly. Check the database name before applying migrations:

```powershell
if ($demoConnection -notmatch 'Database=EmergencyResponse_PortfolioDemo;') { throw 'Unexpected demo database.' }
dotnet ef database update --project src/EmergencyResponse.Infrastructure --startup-project src/EmergencyResponse.Api --context ApplicationDbContext --connection $demoConnection
```

Development API startup seeds reference data, roles, and the configured SystemAdmin, but does **not** apply migrations. Existing development users are not reset by seeding. Previously published passwords must be changed on retained databases.

Start API and Web in separate terminals from the repository root:

```powershell
dotnet run --project src/EmergencyResponse.Api --configuration Release --launch-profile https
```

```powershell
dotnet run --project src/EmergencyResponse.Web --configuration Release --launch-profile https
```

API HTTPS: `https://localhost:7191`; Web HTTPS: `https://localhost:7201`. Web's `ApiSettings:BaseUrl` already matches the API HTTPS profile. Check API `/health/live` and `/health/ready`, then sign in through Web. A successful liveness response alone does not establish SQL/provider readiness.

## 3. Separate automated-test databases

Tracked test connection strings are intentionally empty. Integration/readiness configuration reads environment variables last. Load options likewise prefer `ConnectionStrings__LoadTestDatabase`.

Use a fresh disposable test dataset. The integration safety guard requires the exact database name `EmergencyResponse_IntegrationTests`. Reusing retained incidents, resource assignments, and situations across complete runs can fail two dataset-dependent assertions. Stop all test hosts before any authorized reset of your own disposable database; never reset a shared database or remove the name guard to run against it.

```powershell
$env:ConnectionStrings__TestDatabase = "Server=127.0.0.1,1433;Database=EmergencyResponse_IntegrationTests;User Id=sa;Password=$sqlPasswordText;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
$env:ConnectionStrings__LoadTestDatabase = "Server=127.0.0.1,1433;Database=EmergencyResponse_LoadTests;User Id=sa;Password=$sqlPasswordText;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
dotnet test tests/EmergencyResponse.UnitTests --configuration Release --logger 'trx;LogFileName=unit-tests.trx' --results-directory TestResults/Unit --collect 'XPlat Code Coverage'
dotnet test tests/EmergencyResponse.IntegrationTests --configuration Release --logger 'trx;LogFileName=integration-tests.trx' --results-directory TestResults/Integration --collect 'XPlat Code Coverage'
dotnet run --project tests/EmergencyResponse.CopilotEvaluation --configuration Release
dotnet run --project tests/EmergencyResponse.LoadTests --configuration Release -- --profile Smoke --scenario mixed
```

These hosts use deterministic test substitutes. Their results are not live AI evidence. SQL test setup can reset data; database names are guarded and must remain dedicated. Existing users of this clone may load the preserved **ignored** `.env.local.ps1` file with `. ./.env.local.ps1`; new clones must configure their own variables. No dotenv file is loaded automatically by the application.

For packaging validation:

```powershell
docker build -f docker/Dockerfile.web -t emergencyresponse-web:portfolio-check .
docker build -f docker/Dockerfile.api -t emergencyresponse-api:portfolio-check .
az bicep install
./scripts/validate-infrastructure.ps1
```

No Azure deployment occurs. Containers exclude all appsettings JSON and require runtime environment configuration; User Secrets from the host are not baked into images.

## 4. Walkthrough and screenshot protocol

1. Record the source SHA, date, local environment, database, actor roles, and provider mode. Use one browser session per actor; never capture passwords/tokens.
2. Register a synthetic Citizen through the existing application, then follow the [DEMO18 import and cleanup commands](synthetic-demo-data.md). The repeatable dataset has flood, fire, collision, collapse and overlapping-report inputs. It schedules no AI jobs. Use no real people, phone numbers, or emergency requests.
3. As synthetic SystemAdmin, open incident details and verify the saved identifier, location, history, and actual analysis state. Do not describe pending/failed AI as successful.
4. Capture the dashboard and map. Record missing tiles/layers explicitly. Respect map attribution; no automated tile scraping.
5. Review synthetic resources and, only where the UI supports it, assign a simulated resource. Capture the command center; do not demonstrate real dispatch.
6. Open the DEMO18 manually grouped situation. Its summary explains uncertain casualties and overlapping counts; it is not an AI-generated summary or automatic clustering result. Any demonstration of automatic situation processing remains pending until executed.
7. Use an approved synthetic knowledge document. Ask one knowledge question only if ingestion/Search is functioning; show actual citations and verify their source. No document/Copilot result is fabricated.
8. Optionally demonstrate a supported note/tag proposal with explicit SystemAdmin approval, then check persisted results and audit. Do not claim crash-safe exactly-once execution.

Budget: at most **10 user-triggered incident/Copilot requests** total, one attempt per request. Background work can make additional provider calls; inspect available usage/cost telemetry and stop on throttling or unavailability. No model load test, retraining, automatic retries, or public deployment. Screens unavailable because of providers remain pending.

Capture the six requested views using actual browser screenshots. Store reviewed images under [screenshots](screenshots/README.md), add descriptive captions, and record observations. Local UI render, live provider output, automated test success, and staging verification are separate evidence types.

## 5. Cleanup and publication

Stop only the API/Web processes started for the demo. Retain or remove the disposable demo database/storage through an explicitly reviewed cleanup; do not delete any shared database or arbitrary directory. Remove session environment variables containing passwords and dispose of temporary credentials. Do not commit local secrets, raw logs containing identifiers, or test data outside the reviewed synthetic artifacts.

For DEMO18 fixture removal use the explicit [ownership-scoped cleanup](synthetic-demo-data.md#safe-cleanup), preview first. Do not delete by prefix or force cascading removal of operator-created records. The tool preserves accounts and shared reference data.

Publish changes through a feature branch and pull request. Required checks and protected master apply; direct pushes and automatic dependency merges are not the portfolio workflow. Link actual run results in the evidence register before marking a check complete.

## Phase 18 Part 2.1 results

2026-10-14 local verification: Release build, 536 unit tests, 87 SQL integration tests, EF model check and 79.59% Domain/Application coverage gate passed. The CLI seed/repeat/cleanup/reseed lifecycle passed. Nineteen real-cookie MVC/snapshot/access checks passed; browser rendering, JavaScript, SignalR, live provider workflows and screenshots remain pending. See the [full commands, actual evidence and blockers](synthetic-demo-data.md#actual-verification--2026-10-14).
