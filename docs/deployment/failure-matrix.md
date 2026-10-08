# Dependency Failure Matrix

| Dependency | Impact | Platform behavior |
|---|---|---|
| SQL Server | Critical | Readiness fails; requests that need authoritative persistence return controlled service unavailable responses. |
| AI provider | Partial | Incident creation continues; AI is marked failed or degraded for retry and operator review. |
| Azure AI Search | Partial | Copilot knowledge retrieval is degraded; core incident workflows continue. |
| Weather provider | Partial | External context is unavailable; incident operations continue. |
| Azure Maps | Partial | Map layer is degraded; incident operations continue. |
| Storage | Partial or critical | Media upload/access is unavailable; core incident actions continue when possible. |
| SignalR | Partial | Operations continue; clients reconnect and refresh state. |
| AI queue | Partial | AI work is delayed and remains visible for recovery. |
| Notification subsystem | Partial | Core incident operations continue; notifications can be retried. |
| Prediction engine | Partial | Predictive features are unavailable; deterministic workflows continue. |

The application must distinguish liveness from readiness. Liveness reports whether the process is running. Readiness fails for required dependencies such as SQL; optional dependency failures should be reported as degraded without taking incident intake offline.
