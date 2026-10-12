# Load Testing

## Environment

The NBomber console project is `tests/EmergencyResponse.LoadTests`. Its default `LocalTestHost` mode starts the API through `WebApplicationFactory`, uses deterministic fake AI/search/weather/storage providers, and connects to a dedicated SQL Server database. The connection string must be supplied as `ConnectionStrings__LoadTestDatabase`, and the database name must include `LoadTest`. It must be disposable. The runner rejects the integration-test and application database names. Never point this project at production.

This embedded mode is a single-machine development baseline: API and load generator share a process, HTTP uses TestServer, and SQL is whatever server appears in that connection string. It is not production capacity. Use `External` mode against a dedicated, non-production staging host for network and deployed-infrastructure measurements. External mode requires a short-lived test identity token; incidents/resources/notifications used by scenarios are supplied through `LOAD_TEST_INCIDENT_ID`, `LOAD_TEST_RESOURCE_ID`, and `LOAD_TEST_NOTIFICATION_ID`. The runner rejects hostnames it cannot recognize as development/staging unless `LOAD_TEST_NONPROD_TARGET_CONFIRMED=true` is explicitly set. Its defaults cap incident creates, notification writes, queue jobs, worker jobs, and media uploads; keep those limits conservative for a staging run.

Example PowerShell setup for a local SQL Server:

```powershell
$env:ConnectionStrings__LoadTestDatabase = "Server=localhost;Database=EmergencyResponse_LoadTests;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
dotnet run --project tests/EmergencyResponse.LoadTests --configuration Release -- --profile Smoke --scenario mixed
```

Reports are written under `TestResults/loadtests/<run-id>/`. NBomber writes text, HTML, CSV, and Markdown reports. `run-summary.json` records environment, build, dataset, route P50/P95/P99, error rate, process memory, and SQL command count/duration percentiles. Generated reports are ignored by Git; retain selected summaries as CI artifacts.

## Dataset Size

The `Small` profile seeds 100 synthetic incidents, 2 status histories each, 50 escalations, 20 situations, 50 completed resource assignments, and 1,000 notifications. `--dataset Large` seeds 10,000 incidents, 50,000 status histories, 5,000 escalations, 2,000 situations, 10,000 assignments, and 100,000 notifications. Recreate the dedicated database before changing dataset sizes. Dataset content is synthetic.

`--geography Clustered|Uniform|Hotspot|Boundary` controls synthetic incident coordinates for map and geography checks. These distributions are for exercising queries; they are not evidence of geographic accuracy or scale.

## Scenarios

| `--scenario` | Requests/workload |
|---|---|
| `health` | `/health/live` |
| `incident-read` | Paged incident list |
| `incident-detail` | Detail reads from a fixture ID pool |
| `incident-create` | Synthetic writes with fake AI in `LocalTestHost` |
| `dashboard` | Weighted summary, grid, severity, trend requests |
| `dashboard-combined` | Four dashboard endpoints in one refresh iteration |
| `map` | Incident map, resource map, open situation map |
| `resource` | Resource list, command center, summary, and detail reads |
| `resource-concurrency` | Competing operators assign and dispatch one shared resource; reports 200, 409, and invalid-state responses |
| `situation` | Situation command center and detail |
| `notification` | Inbox, summary, mark-read, mark-all-read |
| `copilot` | Application overhead with deterministic fake chat/search |
| `copilot-real` | Real provider calls, capped at 25 and requiring an entered cost estimate |
| `signalr` | Long-polling connections, forced reconnect, then REST snapshot refresh |
| `signalr-delivery` | Creates a synthetic incident and verifies `DashboardUpdated` delivery |
| `media` | Bounded synthetic image/video uploads to temporary test storage; removes the dedicated temporary directory and fails if cleanup leaves it behind |
| `mixed` | Synthetic non-uniform operator read workload |
| `rate-limit` | Sends 12 incident writes against a 10/minute limit and checks no rejected write persisted |
| `queue` | Holds outbox publishing, creates 1,000–10,000 AI jobs, resumes publisher, checks backlog recovery |
| `worker` | Enqueues jobs into the real local incident-AI queue, adds a deterministic fake-provider delay, and measures completion/failure throughput |
| `chaos` | AI, search, weather, SQL latency/failure and SQL recovery contracts |

The mixed profile weights incident/dashboard reads most heavily, with map, detail, notification, resource, and situation calls represented. Copilot and writes remain separate so their cost and side effects are visible.

## Workload Profiles

| `--profile` | Workload |
|---|---|
| `Smoke` | 5 users for 30 seconds |
| `Baseline` | 20 users for 5 minutes |
| `Stress` | 25, 50, then 100 users in 2-minute stages |
| `Spike` | 10 users, rapid ramp to 100, then down to 10 |
| `Soak` | 25 users for 1 hour |
| `SignalR` | 50 connections for 5 minutes |
| `Media` | 50 virtual users for 1 minute; media upload count is separately capped |
| `Burst` | NBomber injects 1 then 25 new virtual users/second; apply to `incident-create` for write bursts |
| `Create` | Configured incident-create virtual-user arrival rate for 5 minutes |
| `Copilot` | Configured Copilot virtual-user arrival rate for 1 minute |
| `StressLarge` | 25, 50, 100, 200, then 400 virtual users in 2-minute stages |

NBomber enables warm-up by default (15 seconds) to allow JIT, EF model setup, and pools to stabilize. Override with `--warmup-seconds 0` only when deliberately measuring cold start.

## Metrics

Record run ID, environment, build SHA, scenario, profile, duration, dataset, users, requests, successes, failures, requests/second, P50/P95/P99, process working set/managed heap, SQL command count and P50/P95/P99, outbox pending count and oldest age for queue runs, plus provider token/cost metrics for separately authorized real-AI runs. Existing OpenTelemetry SQL instrumentation should be used alongside these local summaries to inspect query shape, locks, deadlocks, connection pools, and CPU/IO. The custom interceptor records SQL command duration and injected delay, not SQL server CPU or lock waits.

For N+1 inspection correlate traces with endpoint spans and SQL spans. Prioritize dashboard, incident list, situation/resource command centers, map, knowledge retrieval, and notifications. Add indexes only after the slow query and write cost are measured.

## Baseline

Start with `--profile Smoke --scenario mixed`, followed by one scenario at a time. Store selected summaries in `docs/testing/results/`; preserve detailed raw NBomber output as CI artifacts. Never report embedded TestServer numbers as production capacity.

## Stress, Spike, and Soak

Use increasing stages until sustained errors, tail-latency explosion, exhausted CPU/memory/SQL, growing backlog, or incorrect behavior. Stop the run at that point. A spike run should include the decline phase and confirm recovery/backlog drain. A soak run should run at moderate load for one to four hours and compare periodic heap, working set, queue age, and disconnect trends. PR CI intentionally runs only a short smoke.

## SignalR

The hub is `/hubs/incidents`; this runner uses authenticated long polling so it can exercise TestServer in local mode. Each scenario user holds a connection for ten seconds, reconnects, then fetches a current dashboard snapshot. Set `--signalr-connections 10`, `50`, `100`, `250`, or `500` with the `SignalR` profile to vary connection count. `signalr-delivery` separately creates an incident and verifies `DashboardUpdated`. SignalR is a notification channel; SQL/API snapshots remain authoritative. For Azure scale-out, use Azure SignalR Service in staging and benchmark the deployed arrangement.

## Copilot

`copilot` uses fake chat, embeddings, and search for application-only overhead. Real AI is a separate `copilot-real` scenario, external staging only, and capped to at most 25 requests. Before enabling it, estimate provider cost and enter `--real-ai-cost-estimate-usd <estimate>` together with `--real-ai true --real-ai-max-requests <n>`. Never use the mock run to claim model latency, and never include provider latency in ordinary API SLOs.

## Media

The `media` scenario sends synthetic JPEG/MP4 payloads through the real upload/controller/security/hash/database path. Test storage streams into a unique temporary directory; the runner deletes it after uploads and fails if cleanup leaves artifacts. The frame extractor and visual AI stay fake, so this does not benchmark FFmpeg CPU or real frame extraction. Default payloads are 10 MiB images and 50 MiB videos; the count is capped at 20 unless overridden, with a hard maximum of 50. Run this scenario only when the developer machine has enough disk and memory for the configured concurrent payloads. The controller allows up to 10 MiB per image and 100 MiB per video.

## Queue

The queue scenario inserts identifier-only AI work into SQL outbox rows while the fake publisher is unavailable, observes pending count and oldest age, then restores publishing and waits for recovery. It verifies durable outbox retention and publication recovery with local in-process workers. The separate `worker` scenario puts jobs through the real local AI queue and reports processed/failed counts, backlog, time to first completion, and jobs/minute while varying `--worker-ai-delay-ms`. It measures the currently configured single local worker and cannot tune worker count. Neither scenario claims Azure Service Bus throughput or exposes per-job in-memory channel depth/age. Use Azure Service Bus and worker telemetry in staging for queue-age, dead-letter, retry, worker scaling, and throughput conclusions.

## Database

The load-test interceptor records completed EF command durations and command counts. Existing OpenTelemetry/SQL instrumentation is the source for server resource use, plans, logical reads, locks, deadlocks, and pool utilization. Use a separate SQL instance for staging performance work.

## Known Bottlenecks and limits

- `LocalTestHost` is in-process and shares CPU and memory with NBomber.
- The local message bus uses bounded in-process channels and does not emulate Azure Service Bus scale-out.
- AI, search, weather, and storage defaults are fake; real provider calls are separate and bounded.
- The CI smoke uses a small synthetic dataset; it catches API/test-host regressions but does not establish capacity.
- The fake frame extractor does not measure FFmpeg CPU or real frame-extraction pressure.

## Release Thresholds

See [performance-gates.md](performance-gates.md). CI gates smoke errors/correctness. Latency stays report-only until a stable staging runner/baseline exists.
