# Phase 18 Part 2.1 — Synthetic Demo Data

Local exercise data only. This is not an emergency dispatch demonstration or pilot approval. Screenshots remain pending; the evidence below covers automated tests and real server responses, not browser rendering.

## Dataset and truthful states

| Incident identifier | Synthetic report | Uncertainty preserved |
|---|---|---|
| DEMO18-FLOOD-01 | Flood with two trapped vehicles and two reported occupants | Rescue outcome, injuries and casualties unknown |
| DEMO18-FIRE-01 | Apartment smoke/fire | Occupant, trapped-person and casualty counts unknown |
| DEMO18-ROAD-01 | Two-vehicle collision blocking a lane | Injuries and casualties unconfirmed |
| DEMO18-COLLAPSE-01 | Partial building collapse | Occupancy, trapped people and casualties unknown |
| DEMO18-FLOOD-02 | Nearby second underpass report | May overlap the first report; not confirmed |
| DEMO18-DUPLICATE-01 | Possible repeated underpass report | Input for a duplicate exercise, not an assessed match |

Each report has one primary location, initial Submitted history, and a DEMO18 explanatory note. Coordinates are synthetic locations in the Pune area; addresses and geographic labels are marked DEMO18. No personal contact details or media are imported.

Related records use existing domain types and reference data:

- Six available, unassigned resources: boat, fire truck, police vehicle, rescue team, ambulance and excavator, linked to their existing departments.
- Two open **manual** Elevated escalations for the fire and collapse, explicitly requesting human review. Scores are null; these are authored exercise records, not rule-engine/AI findings.
- One manually grouped flood situation with three linked reports and one primary incident. Its six-person total is the sum of report counts and may double-count the same two occupants. Its casualty total of zero is the existing non-nullable aggregate default for unknown counts, **not confirmed zero casualties**. The summary explicitly explains both limits.
- Three manual System reminders addressed only to the supplied review account, linked to the appropriate escalation/incident or situation. Inbox persistence does not imply SignalR delivery.

**39 fixture records plus one ownership audit record.** Incident Low/Normal values are intake defaults, not triage conclusions. All incident casualty fields remain null. Duplicate processing remains NotStarted. No AI/visual analyses, embeddings, similarity scores, predicted risks, knowledge documents, resource dispatches or background jobs are fabricated or scheduled.

The duplicate-review screen requires real computed match records. This importer supplies overlapping reports only; it does not populate that screen with invented scores. Live duplicate detection remains unverified.

## Implementation and isolation

The explicit console tool calls new methods on the **existing `DatabaseSeeder`** and reuses its incident-type/department seeding. Normal startup is unchanged. There is no seeding API, migration, second reference-data system, or demo identity bootstrap. Existing authorized application services/controllers read the data.

- CLI environment: `DOTNET_ENVIRONMENT=Development` only.
- CLI target: exact `EmergencyResponse_PortfolioDemo` database on `localhost`, `127.0.0.1` or `.` SQL Server. Ports/local instance suffixes are supported; remote/Azure targets are refused.
- Internal tests additionally allow the exact local `EmergencyResponse_IntegrationTests` database in Test, or in-memory SQLite in Test.
- `EmergencyResponse_LoadTests`, normal development databases, Staging and Production are refused. No LOADTEST or AI evaluation files are changed.
- API User Secrets are reused by the tool; environment variables override them. Do not put connection credentials in arguments, tracked JSON or logs.

Stable IDs and an exact, versioned ownership manifest in `AuditLogs` establish ownership. Repeated import retains IDs and existing edits/read states; it never adopts rows merely because their number starts with DEMO18. Different actors, identifier collisions, incomplete fixtures, invalid manifests or pending unrelated DbContext changes fail explicitly. Imports and applied cleanup use transactions.

## Prerequisites

Follow [local configuration and migrations](demo-script.md) first:

1. .NET 10 and repository EF tooling; migrated disposable **SQL Server 2025** database for the native-vector schema.
2. `ConnectionStrings:DefaultConnection` configured in API User Secrets or `ConnectionStrings__DefaultConnection` set to that exact local demo database. Explicitly apply existing migrations; the tool does not create/migrate databases.
3. Normal Development API startup with its required JWT/provider configuration, and Web startup with a valid Syncfusion license and API base URL. Starting the host alone is not live provider verification.
4. Register an active `DEMO18-citizen@test.local` Citizen through the existing `/Account/Register` workflow. Use no real personal information. The importer does not register, elevate or remove accounts.
5. Supply an existing active Operator/admin with incident and situation read permissions. For the full walkthrough, use the configured synthetic SystemAdmin. Confirm real account credentials privately; existing passwords are not reset by startup.

## Import and repeat

From the repository root, with the prerequisites above satisfied:

```powershell
$ErrorActionPreference = 'Stop'
$env:DOTNET_ENVIRONMENT = 'Development'
$reviewerEmail = 'portfolio-admin@test.local' # your existing synthetic admin
$null = New-Item -ItemType Directory -Force artifacts/portfolio

dotnet run --project tools/EmergencyResponse.DemoData --configuration Release -- seed --reporter-email DEMO18-citizen@test.local --operator-email $reviewerEmail > artifacts/portfolio/demo18-seed.json
if ($LASTEXITCODE -ne 0) { throw 'Demo import failed.' }

# Repeat: Created=false and the same record IDs; no operator edits overwritten.
dotnet run --project tools/EmergencyResponse.DemoData --configuration Release --no-build -- seed --reporter-email DEMO18-citizen@test.local --operator-email $reviewerEmail
if ($LASTEXITCODE -ne 0) { throw 'Repeat import failed.' }
```

The result JSON contains IDs/count groups, not credentials. Keep local results under ignored `artifacts/`.

## Authorized views and reproducible response checks

Start the normal API/Web as documented in [demo-script.md](demo-script.md). Through separate real login sessions:

- Citizen: `/Incidents?search=DEMO18-&pageSize=50`, `/Incidents/MyReports`, and all six incident details.
- Reviewer: incident list, `/Resources`, `/Situations` and its details, `/Notifications`, `/Dashboard`, `/Map` and `/Map/Snapshot`.
- Fire/collapse details carry manual escalation records; the escalation controller has commands, not a standalone index screen.
- Citizen notifications must not contain reviewer reminders; resource access must be denied. Anonymous incident access must require login.

Repeat the HTTP response checks without capturing a browser:

```powershell
$reporterPassword = Read-Host 'Synthetic Citizen password' -AsSecureString
$reviewerPassword = Read-Host 'Synthetic reviewer password' -AsSecureString
./scripts/verify-demo-pages.ps1 -ReporterPassword $reporterPassword -ReviewerEmail $reviewerEmail -ReviewerPassword $reviewerPassword -SeedResultPath artifacts/portfolio/demo18-seed.json
```

The script is restricted to local HTTPS and uses the actual antiforgery/cookie login plus existing Web-to-API JWT bridge. It checks content, not just HTTP 200, and writes sanitized route/role/check results. It deliberately uses Citizen incident details: the **operator detail view invokes on-demand prediction**, which can create application-owned snapshots. That prediction behavior is outside this fixture check and may correctly block subsequent cleanup.

No headless browser or generated screenshots substitute for a connected browser session. Chart/grid initialization, map tiles, interactive commands, accessibility/layout and SignalR recovery remain pending.

## Safe cleanup

Preview is the default; review its exact per-entity counts before applying:

```powershell
dotnet run --project tools/EmergencyResponse.DemoData --configuration Release --no-build -- cleanup
if ($LASTEXITCODE -ne 0) { throw 'Cleanup preview refused; review dependent records.' }

dotnet run --project tools/EmergencyResponse.DemoData --configuration Release --no-build -- cleanup --apply
if ($LASTEXITCODE -ne 0) { throw 'Cleanup refused; no partial removal was committed.' }
```

Cleanup removes only IDs in the validated ownership manifest, including its own audit marker. Shared reference data, borrowed accounts and unowned DEMO18-like records remain. Repeating applied cleanup is a no-op; reseeding uses the same stable IDs.

**Cleanup refuses if additional application-owned records reference fixtures**, including FK dependents, unmapped GUID references such as notes/job work items, audit evidence or outbox payloads. It does not cascade away operator work, erase audit evidence, clear unrelated notification FKs or delete records by prefix. Review and recover such data through the authorized workflow; do not weaken the guard or manually delete the marker to force removal. While editing the fixture lifecycle, pause other demo activity and resolve errors before reseeding.

Stop only the local hosts you started. Cleanup does not drop the database or delete media directories. After verification at this checkpoint the fixtures were restored for review; the verification hosts were stopped.

## Actual verification — 2026-10-14

Source: working changes on `docs/phase18-portfolio-release`, based on `962f97ebad67fa3dce634e224b6e92a6ffc200bb`; [PR #17](https://github.com/AmarjeetJha17/AI-Emergency-Response/pull/17) remains open. These results are local, not a GitHub run for uncommitted changes.

| Check | Actual result | Local evidence |
|---|---|---|
| Restore and Release solution build | Passed; final build 0 errors, 14 existing warnings | `artifacts/portfolio/demo18-build.log` |
| Full unit suite | **536 passed**, 0 failed/skipped; includes 17 demo guard/state cases and 10 situation-client regression cases | `TestResults/Demo18/FullUnit/unit-tests.trx` |
| Full SQL 2025 integration suite | **87 passed**, 0 failed/skipped; new case validates authenticated API visibility, Citizen scope, valid related records, repeat import and scoped cleanup | `TestResults/Demo18/FullIntegration/integration-tests.trx` |
| Domain/Application merged coverage | **79.59%** (2,344 / 2,945 lines), 70% gate passed; only final successful reports merged | `TestResults/Demo18/CoverageMerged/Cobertura.xml` |
| EF model check | No pending model changes | `artifacts/portfolio/demo18-ef-model.log` |
| Real local CLI lifecycle | Seed, repeat, cleanup preview, apply, repeat no-op and reseed passed; same IDs restored | `artifacts/portfolio/demo18-seed-*.json`, `demo18-cleanup-*.json` |
| Health | API liveness/readiness and Web liveness returned 200 with local configuration; not live AI-call evidence | Local HTTPS checks |
| Authorized Web responses | **19 checks passed**, actual cookies and JWT bridge; six detail responses and operational pages/snapshots contained expected fixtures | `artifacts/portfolio/demo18-pages.json` |
| Restored local data | Six synthetic incidents; zero AI analyses, duplicate matches and outbox messages | SQL read-only inspection of isolated demo database |
| Git diff hygiene | `git diff --check` passed | Local working tree |

Verification exposed and fixed an existing `SituationApiClient` bug: wrapper-first deserialization of the API's array closed the content stream before the fallback read. Both command-center/map readers now use the actual array contract once. Tests preserve parse/HTTP/authentication errors rather than silently returning empty snapshots. Earlier cleanup-order/unmapped-note guard failures and strict-mock disposal failures were corrected; the final suites passed without weakening their assertions.

The complete integration suite was run against a fresh disposable integration database because retained data can fail existing resource/situation count assertions. Only this checkpoint's disposable container/test database was reset. No Azure resources were deployed; no dependency update, screenshot, live AI request, dispatch, Copilot action or staging test is claimed.

## Remaining blockers

- Connected-browser/visual verification and all six screenshots, live AI/media/Copilot/citation/duplicate workflows, SignalR and operator state-transition demonstrations.
- Unknown casualty aggregation and possible double-counting in manual situations; intake Low/Normal defaults are not evaluated severity.
- Existing package vulnerability and nullable warnings remain; passing this test suite does not remediate them.
- Existing authorization, durable worker/recovery and Azure/pilot blockers in the [feature register](feature-verification.md) remain.

## Part 2.2 follow-up

The Phase 2.1 changes were committed as `bf63d0e` before [Part 2.2 verification](local-demo-verification.md). The final local build, 544 unit tests, 89 SQL integration tests, EF check, 52 real-JWT API checks, 19 MVC checks and 29 operational checks passed. This follow-up does not replace the historical coverage result above with a new measurement.

The six reports/resources, two authored escalations, manual linked situation and three owned reminders were reimported, previewed, cleaned and restored before operational edits. An unrelated forecast reminder, accounts and reference data were retained. LOADTEST row counts/checksums were unchanged around the final guarded demo operations. Demo cleanup now correctly refuses the additional application-owned history/escalation/audit evidence created during the walkthrough; reimport keeps those state changes and stable IDs.

The duplicate page can now display an explicit unavailable/pending/failed assessment without inventing a match; the no-embedding API returns a clear 409 rather than 500. The resource dispatch policy bypass and completed-assignment detail projection were fixed with regression coverage. There are still no fabricated AI analyses or duplicate scores in this dataset.

Normal startup and the fixture seeder remain unchanged. The missing synthetic Operator was provisioned by invoking the existing identity test seeder outside startup, in an ignored local diagnostic harness; no public account/fixture bootstrap was added. Later Part 2.2B manual screenshots provide the bounded evidence in the verification report; SignalR reconnect and live provider workflows remain unverified. On 2026-10-15 the project owner accepted Phase 2.2 as complete with the remaining checks explicitly deferred. See [phase closure and deferred backlog](local-demo-verification.md#phase-closure--2026-10-15). No Azure deployment occurred.
