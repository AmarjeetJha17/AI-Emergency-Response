# Performance Gates

These are initial engineering release targets. They describe desired behavior; no target is an achieved capacity claim until a run report records the environment, dataset, build, and observed results.

| Subsystem | Primary measure | Initial gate |
|---|---|---:|
| Incident read | P95 | < 500 ms |
| Incident create | P95 | < 750 ms |
| Dashboard snapshot | P95 | < 1 s |
| Map snapshot | P95 | < 1 s |
| Resources | P95 | < 750 ms |
| Situations | P95 | < 1 s |
| Notifications | P95 | < 500 ms |
| Copilot | Separate application and provider runs | Measure separately |
| AI worker | Throughput and oldest job age | Measure separately |
| SignalR | Connection success and event latency | > 99% connection success |
| General API errors | Error rate | < 1% |
| Duplicate job side effects | Count | 0 |
| Critical workflow correctness | Successful contract checks | 100% |

Do not use average latency as the principal release metric. Report P50, P95, P99, request rate, and errors. A relative regression above 20% should be reviewed even when the absolute limit still passes. `EmergencyResponse.LoadTests` can compare per-route summaries using `--compare-baseline`; comparison is report-only unless `--enforce-regression true` is set. CI smoke gates correctness and error rate while reporting latency without making it a release blocker.

The embedded `LocalTestHost` smoke shares the load-generator process and uses ASP.NET Core TestServer. Its latency is useful for fast regression checks, not deployment capacity. Use an isolated staging environment, a separate load generator, and managed SQL/SignalR/provider dependencies for deployment measurements.
