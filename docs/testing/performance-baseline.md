# Performance Baseline

These are initial engineering targets for controlled environments, not contractual SLAs. Exclude external asynchronous AI work from incident-submit timing. Record environment, dataset size, concurrency, build, and dependency versions with every run.

| Workload | Initial target |
|---|---|
| Public incident submission | P95 < 1 second, excluding asynchronous AI |
| Incident retrieval | P95 < 500 ms |
| Dashboard snapshot | P95 < 1 second |
| Map snapshot | P95 < 1 second |
| Standard operational command | P95 < 750 ms, excluding external dependencies |
| Copilot | Measure separately; model latency and token cost are reported |

See [load-testing.md](load-testing.md) for the NBomber project and run profiles. The k6 script remains a network-level staging workload. Capture requests/second, P50/P95/P99, error rate, CPU, memory, SQL utilization, queue depth/age, AI latency, and AI cost per request. PR latency is initially report-only; functional thresholds and error budgets remain enforced. Establish a stable runner baseline before using latency as a release blocker.
