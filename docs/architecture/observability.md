# Production Reliability, Observability & Resilience Architecture

## 1. Overview & Core Principles

The **AI Emergency Response System** is designed for mission-critical operations where downtime, data loss, or blind failures cannot be tolerated. The reliability and observability architecture rests on four pillars:

1. **Full Telemetry Observability**: Unified distributed tracing, operational metrics, and contextual logging using .NET standard diagnostic APIs (`System.Diagnostics.ActivitySource`, `System.Diagnostics.Metrics.Meter`) and OpenTelemetry SDKs.
2. **Graceful Degradation**: The core incident reporting and dispatch platform operates deterministically. If external AI services or network models experience throttling or outages, the system degrades gracefully into deterministic mode without dropping emergency reports.
3. **Non-Destructive Execution History**: Failed AI analyses or visual evaluations never delete, overwrite, or invalidate underlying incidents or uploaded media. Historical execution attempts, failure categories, and retry schedules are recorded cleanly.
4. **Active Resilience & Manual Operator Controls**: HTTP clients utilize automated transient retry pipelines and circuit breakers via `Microsoft.Extensions.Http.Resilience`. Furthermore, operators possess explicit manual reprocessing endpoints to retry failed AI, duplicate, or visual jobs on demand.

```
                    EMERGENCY PLATFORM
                            │
             ┌──────────────┼──────────────┐
             ▼              ▼              ▼
        Observability   Resilience      Health
             │              │              │
          Logs           Retries        Liveness (/health/live)
          Metrics        Timeouts       Readiness (/health/ready)
          Traces         Breakers       Worker Heartbeats
             │              │              │
             └──────────────┼──────────────┘
                            ▼
                     OPERATIONS CENTER
```

---

## 2. Distributed Tracing & OpenTelemetry

### 2.1 ActivitySource & W3C TraceContext
All asynchronous operations and background job executions are instrumented using the centralized `ActivitySource`:
- **Source Name**: `EmergencyResponse.Application`

Activities span critical boundaries:
- `AI.IncidentAnalysis`: Traces incident intake to AI fact extraction, decision scoring, and automated escalation checks.
  - Tags: `incident.id`, `ai.model`, `incident.type`. No PII or raw prompt text is exposed in trace tags.
- `AI.VisualAnalysis`: Traces image/video frame extraction and vision analysis.
  - Tags: `incident.media_id`.
- `AI.SituationAnalysis`: Traces situation clustering re-evaluations and operational summaries.
  - Tags: `situation.id`.

### 2.2 Instrumentation & Exporters
Both `EmergencyResponse.Api` and `EmergencyResponse.Web` configure OpenTelemetry:
- **Tracing**:
  - `AddAspNetCoreInstrumentation()`: Automatically traces inbound HTTP requests.
  - `AddHttpClientInstrumentation()`: Injects W3C `traceparent` headers into outbound HTTP calls.
  - `AddSqlClientInstrumentation()`: Captures SQL query spans.
  - `AddConsoleExporter()` / `AddOtlpExporter()`: Ready for local debugging and OpenTelemetry collector ingestion (e.g., Jaeger, Tempo, Application Insights).

---

## 3. Correlation ID Tracking

Every HTTP request passing through `EmergencyResponse.Api` is tracked by `CorrelationIdMiddleware`:
- Inspects incoming `X-Correlation-ID` header.
- If present, adopts the supplied identifier; if absent, generates a new cryptographic `Guid`.
- Attaches `X-Correlation-ID` to the outgoing response headers.
- Pushes `CorrelationId` into the Serilog/logging scope, ensuring every subsequent log message within that request pipeline includes the correlation identifier.

---

## 4. Operational Metrics & Telemetry

Custom operational metrics are declared in `EmergencyResponseTelemetry` using `Meter: EmergencyResponse.Application`:

| Metric Name | Type | Unit | Description |
|---|---|---|---|
| `emergency.incidents.created` | Counter | count | Total incidents reported |
| `emergency.ai.analyses.completed` | Counter | count | Successful AI analyses completed |
| `emergency.ai.analyses.failed` | Counter | count | AI analyses that encountered errors |
| `emergency.duplicates.detected` | Counter | count | Duplicate match candidate detections |
| `emergency.situations.created` | Counter | count | Operational situations detected |
| `emergency.escalations.opened` | Counter | count | Automated escalation events triggered |
| `emergency.notifications.created` | Counter | count | Authoritative notifications generated |
| `emergency.ai.jobs.enqueued` | Counter | count | Jobs placed onto in-memory channels |
| `emergency.ai.jobs.processed` | Counter | count | Jobs dequeued and dispatched |
| `emergency.ai.queue.length` | UpDownCounter | count | Active pending queue backlog |
| `emergency.ai.analysis.duration` | Histogram | ms | Execution duration of AI incident analysis |
| `emergency.duplicate.detection.duration` | Histogram | ms | Execution duration of vector duplicate search |
| `emergency.visual.analysis.duration` | Histogram | ms | Execution duration of visual analysis |

---

## 5. Health Checks & Graceful Degradation

The platform exposes dedicated health endpoints separating process liveness from operational readiness:

### 5.1 Endpoints
- **Liveness (`GET /health/live`)**:
  - Predicate: returns `200 OK` as long as the ASP.NET Core Kestrel runtime process is running and responding to TCP traffic.
  - Used by Kubernetes/Docker liveness probes to detect process hangs.
- **Readiness (`GET /health/ready`)**:
  - Evaluates tags matching `["ready"]`:
    - `SqlServerHealthCheck`: Verifies `ApplicationDbContext.Database.CanConnectAsync()`.
    - `BackgroundWorkerHealthCheck`: Verifies background workers have reported a healthy heartbeat within the 10-minute threshold.
    - `AIProviderHealthCheck`: Verifies AI configuration.
- **Aggregated (`GET /health`)**:
  - Full system health status.

### 5.2 Graceful Degradation Principle
If the AI provider is unavailable or unconfigured:
- `AIProviderHealthCheck` returns `HealthStatus.Degraded` rather than `HealthStatus.Unhealthy`.
- Inbound incident creation continues unabated.
- Deterministic rules assess base priority and dispatch recommendations.
- AI jobs are queued or marked for automatic/manual retry without interrupting emergency call center operations.

---

## 6. Background Worker Reliability

Background workers (`IncidentAIBackgroundWorker`, `VisualAnalysisBackgroundWorker`, `IncidentSituationBackgroundWorker`) report their lifecycle state to the thread-safe `IWorkerHealthState` singleton:

```csharp
public interface IWorkerHealthState
{
    void MarkSuccess();
    void MarkSuccess(string workerName);
    DateTime? LastSuccessUtc { get; }
    DateTime? GetLastSuccessUtc(string workerName);
}
```

- Uses lock-free `Interlocked.Exchange` and `ConcurrentDictionary` to record timestamps per worker.
- `BackgroundWorkerHealthCheck` inspects worker timestamps:
  - If no success recorded yet: reports `Degraded`.
  - If last success exceeded 10 minutes: reports `Unhealthy`.
  - Normal operation: reports `Healthy`.

---

## 7. HTTP Client Resilience & Safe Retries

All outbound HTTP communication between `EmergencyResponse.Web` and `EmergencyResponse.Api` leverages `Microsoft.Extensions.Http.Resilience`:
- Each typed client (`IAuthApiClient`, `IIncidentApiClient`, `IResourceApiClient`, etc.) chains `.AddStandardResilienceHandler()`.
- Provides out-of-the-box Polly v8 resilience strategies:
  1. Total Request Timeout (30s)
  2. Transient Retry (exponential backoff with jitter)
  3. Circuit Breaker (opens on sustained failure rates)
  4. Attempt Timeout (10s per request)
- **Retry Scope**: Strictly applies to transient failures (HTTP 5xx, network drops, 408 request timeouts, 429 rate limits). 4xx client errors (e.g. 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found) are never retried.

---

## 8. Non-Destructive AI Execution History

Underlying domain entities maintain persistent execution telemetry across attempts:

### 8.1 Schema Attributes
- `AIAnalysis` & `VisualAnalysis`:
  - `AttemptNumber`: Incremental attempt index (1, 2, 3...).
  - `RetryCount`: Number of retries triggered for this job.
  - `FailureCategory`: Categorized string (`"Timeout"`, `"RateLimited"`, `"ProviderUnavailable"`, `"SchemaValidation"`, `"TransientNetwork"`, `"Unknown"`).
  - `LastAttemptAtUtc`: Timestamp of the latest execution attempt.
  - `NextRetryAtUtc`: Scheduled timestamp for subsequent automated retry using exponential backoff ($2^{\text{RetryCount}} \times 5\text{s}$).
  - `InputTokens`, `OutputTokens`, `TotalTokens`: Recorded token consumption from AI models.

### 8.2 Non-Destructive Guarantee
When AI processing or duplicate detection fails:
- The incident entity is **never deleted**.
- Incident title, description, citizen reporter info, geolocation, and media records remain 100% intact.
- The failure reason and category are written to the analysis record.
- SignalR updates notify the dashboard of failure state so operators are informed immediately.

---

## 9. Operator Reprocessing API Endpoints

Privileged operators, emergency admins, and system administrators can manually reprocess failed or stale jobs:

| Endpoint | Method | Role Required | Description |
|---|---|---|---|
| `/api/incidents/{id}/ai/reprocess` | `POST` | Operator+ | Re-enqueues incident AI analysis job for fresh processing. |
| `/api/incidents/{id}/duplicates/reprocess` | `POST` | Operator+ | Re-runs embedding generation and candidate duplicate search. |
| `/api/incidents/{id}/visual/reprocess` | `POST` | Operator+ | Re-enqueues all attached media items for visual analysis. |
| `/api/situations/{id}/reanalyze` | `POST` | Operator+ | Re-executes multi-incident cluster summarization and AI situation analysis. |
