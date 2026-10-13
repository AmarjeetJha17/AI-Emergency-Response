# Pilot Monitoring and Alerts

Status: Specification only. Azure export, dashboards, scheduled checks, alert rules and notification delivery are **not verified**. Missing telemetry means unknown health; required unknown signals block entry.

## Sources, baseline and configuration

Use Azure Monitor/Application Insights (or an approved OTLP collector/export path), Container Apps platform logs, SQL/Storage/Service Bus diagnostics and application traces. Metric declarations in `EmergencyResponse.Application` do not prove recording or export. Exact cloud queries, workspace IDs, alert resource IDs and action-group recipients must be recorded after provisioning; never write credentials here.

Poll public Web/API HTTPS liveness/readiness every minute through an approved availability monitor. Probe private SQL from a VNet-connected authorized workload using its runtime identity. Do not expose private SQL or business data to make monitoring easier. Worker signals refer to current API-hosted consumers; a standalone Worker monitor awaits implementation.

Before entry collect five approved two-hour staging sessions with the agreed synthetic workload and dataset. For each rate retain denominator, per-route distribution and five-minute windows. Fix defects before treating their behavior as baseline. The owner/security reviewer approve the baseline artifact and alert policy. For warning rates below use `W = max(5%, 2 × baseline P95 five-minute rate)`; for queue age use `A = max(60 seconds, 2 × baseline P95 oldest-pending age)`. Zero-count baselines use those floors. These are provisional formulas requiring approved results, not achieved service levels.

Check five-minute windows every minute. “Two windows” means two consecutive five-minute buckets. Under 20 qualifying requests/jobs/attempts per bucket, label a rate inconclusive; counts, health probes and safety events remain actionable. Separate expected negative-test requests from normal traffic. Never filter an unexpected failure out as a test.

## Alert and response matrix

| Signal and source | Trigger | Accountable role and immediate response | Current gap/status |
|---|---|---|---|
| API 5xx / request telemetry | >1% with ≥20 requests per bucket for two windows; any false persistence claim immediately | Support lead checks route/correlation/dependency; operations lead suspends affected workflow if correctness unknown | Export, baseline and rule pending |
| API P95 / request durations by route | Existing incident read 500 ms, create/resources 750 ms, dashboard/map/situations 1 s, notifications 500 ms exceeded with ≥20 samples for two windows | Support diagnoses SQL/allocations/queues; >20% comparable baseline regression requires review | Staging latency pending |
| Readiness / scheduled HTTPS probes | Two consecutive one-minute failures; check liveness separately | Support diagnoses required dependencies; operations pauses dependency-reliant scenarios | Availability tests/rules pending |
| SQL connectivity / readiness + private runtime probe | Any failed database operation investigated; two probe failures escalate | Support uses [database runbook](runbooks/database-outage.md); never claim an uncommitted incident saved | Runtime private probe pending |
| Consumer fault / logs, heartbeat | Any new fault investigated; missing expected heartbeat for two polling intervals or repeated fault escalates | Support uses [queue runbook](runbooks/queue-backlog.md) | `worker_fault_total` declared, no runtime recording found; collector/heartbeat coverage pending |
| Oldest pending work / outbox timestamps and per-queue enqueue age | >A for two windows; no drain during a five-minute recovery interval escalates | Support checks durable path, consumers, retries and throttling; owner approves backlog tolerance | `queue_age`/`worker_lag` declared without runtime recording; oldest-pending sampling needed. Completion-age histograms cannot prove pending-job age |
| Dead-letter queue / Service Bus count and message metadata | Any new dead letter investigated immediately | Support classifies poison/transient/security failure; no blind replay or purge | Azure diagnostics and `queue_deadletter_total` recording pending |
| AI-provider failures / execution logs, completed/failed counters, provider diagnostics | Rate >W with ≥20 attempts for two windows; any circuit-open/outage investigated | Support uses [AI runbook](runbooks/ai-dependency-outage.md); verify core persistence and explicit degraded state | Provider/export/rule pending |
| Copilot grounding / `emergency.ai.security.grounding_failures`, reviewed evaluations | Any failure reviewed; repeat failures suspend affected Copilot scenario; unsupported advice presented as fact triggers stop | Security reviewer/operations lead verify sources and output gating | Export/delivery and safety UI checks pending |
| Failed authentication / auth results | Rate >W with ≥20 attempts for two windows; ≥5 unexpected failures per five minutes for one account investigated | Security reviewer checks legitimate tests vs abuse; use [security runbook](runbooks/security-incident.md) | Sanitized logs/rule pending; no token/password logging |
| Authorization denials / 403 and scope-protected 404 plus audit | Unexpected denial investigated; same rate/count policy as authentication; any actual unauthorized execution/exposure triggers immediate stop | Security reviewer correlates actor, scope and target | Negative tests expected; dedicated instrumentation/export pending |
| Resource conflicts / 409 + histories | Any unexpected conflict reviewed; >W with ≥20 commands for two windows prompts workload review | Operations reconciles fresh state; any silent overwrite triggers immediate stop | Endpoint outcome export pending; correct conflicts are not failures |
| SignalR / client negotiate/reconnect, snapshot outcome | Connection success ≤99% over a session with ≥100 attempts prompts review; any reconnect with failed authoritative refresh stops dependent decisions | Operations refreshes snapshots; support checks hub auth/groups/network | Client delivery/reconnect/snapshot telemetry pending; low sample rate inconclusive |
| Storage / Blob and authorized application access diagnostics | Any unexpected access/upload failure investigated; two synthetic authorized checks fail consecutively escalates | Support isolates media workflow; any unauthorized exposure triggers security stop | Private runtime checks, diagnostics and rule pending |

Link critical triggers to [stop and resumption](pilot-go-no-go.md#stop-and-resumption-authority). Whole-pilot safety suspension never waits for a rate denominator.

## Delivery, ownership and monitoring failure

Use an approved Azure Monitor action group to notify the support lead and operations lead; security signals additionally notify the security reviewer. Names, contact channel, backup and action-group IDs are pending in [pilot scope](pilot-scope.md). During staffed sessions acknowledge urgent signals within five minutes and notify the pilot owner if unacknowledged. Preserve alert firing, notification receipt, acknowledgment and recovery times.

If required monitoring stops delivering data, pause new affected test activity, investigate monitoring, and reverify state before continuing. Do not treat missing samples as zero failures.

## Verification register

| Required check | Rule/source ID | Baseline/threshold approval | Controlled trigger and notification evidence | Result/reviewer |
|---|---|---|---|---|
| API health/error/latency | Pending | Pending | Not executed | Pending |
| SQL, Storage and Service Bus | Pending | Pending | Not executed | Pending |
| Consumer faults/backlog/dead letters | Pending | Pending | Not executed | Pending |
| AI/grounding/security outcomes | Pending | Pending | Not executed | Pending |
| SignalR reconnect/snapshot | Pending | Pending | Not executed | Pending |
| Alert acknowledgment and escalation | Pending | Pending | Not executed | Pending |

Test triggers only on isolated synthetic staging workloads. Record recovered alerts and absence of credential/payload leakage. Follow [observability architecture](../architecture/observability.md) as a design reference; the source/runtime checks and this pending register take precedence over claims of deployed monitoring.
