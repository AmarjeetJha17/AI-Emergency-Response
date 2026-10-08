# Test Inventory

Coverage is an initial audit of the repository structure and existing tests. `Present` means at least some relevant automated coverage exists; it does not imply complete workflow coverage. Gaps below are intended backlog and must be updated as tests land.

| Subsystem | Unit | Integration | Security | Failure path | AI evaluation | Important missing coverage |
|---|---|---|---|---|---|---|
| Incident Management | Present | Present | Partial | Partial | Partial | Extended citizen-create → operator-read → admin resource dispatch path; AI completion and downstream escalation still need a composed workflow |
| Identity / RBAC | Present | Present | Partial | Partial | — | Exhaustive role × declared permission mapping is now checked; department-scope IDOR and endpoint coverage for every operation remain |
| AI Incident Analysis | Partial | Partial | — | Partial | Partial | Fact-level metrics, confidence calibration, unavailable provider workflow |
| Severity / Priority | Present | Partial | — | — | — | Severity boundaries and monotonic invariant exist; priority thresholds have representative boundary combinations, but not every adjacent score is reachable with current weighted inputs |
| Duplicate Detection | Present | Partial | — | Partial | Partial | 0/.74/.75/.89/.90/1.0 boundary and preservation invariants |
| Visual / Video Analysis | Partial | Partial | Partial | Partial | Partial | Labelled hazard dataset and critical false-negative metrics |
| Dashboard | Partial | Partial | — | — | — | API contract, performance, stale/degraded source behavior |
| Live Map | Partial | Partial | — | — | — | Scope filtering, load baseline, dependency degradation |
| Resources | Present | Present | Partial | Present | — | SQL row-version stale-write conflict now proves no silent overwrite; full responder workflow and API-level race remain |
| Escalations | Partial | Partial | Partial | Partial | — | Permission matrix and concurrent resolution |
| Situations | Present | Partial | Partial | Partial | Partial | radius/time boundaries, chain linkage, authorization scope |
| Notifications | Present | Partial | Partial | Partial | — | Read-state concurrency and retention behavior |
| External Context | Partial | Partial | — | Partial | — | Provider outage behavior and stale context contract |
| Prediction | Partial | Partial | — | Partial | Partial | Temporal backtesting and model metrics |
| Knowledge / RAG | Partial | Partial | Partial | Partial | Partial | Recall@K, citation, groundedness, abstention benchmark |
| Copilot | Partial | Partial | Partial | Partial | Partial | Tool selection/arguments, cross-user IDOR, tool-output injection |
| Controlled AI Actions | Present | Partial | Present | Partial | Partial | Full proposal/approval/execution rejection matrix |
| Security | Partial | Partial | Partial | Partial | Partial | Incident ownership IDOR and full role-permission matrix are covered; resource-by-resource IDOR, rate-limit aliases, export authorization, log-leak tests remain |
| Reliability / DR | Partial | Partial | — | Partial | — | Incident outbox persistence is checked in the lifecycle integration test; SQL outage, queue redelivery, restore drill and measured RTO/RPO remain |

Recent additions include `IncidentApiContractTests` (pagination shape, authentication, validation ProblemDetails), `ResourceConcurrencyTests` (two SQL contexts with a stale row-version write), and `IncidentOperationalLifecycleTests` (incident persistence/outbox, operator read, resource assignment and dispatch). These are integration tests and require the configured isolated SQL Server database. The lifecycle test deliberately stops at dispatch; it does not claim to verify AI completion, responder arrival, escalation resolution, dashboard events, or audit records as one end-to-end chain.

`Partial` is intentionally conservative: the repository contains tests in these areas but does not yet prove the requested end-to-end operational contract. The inventory should be reviewed with each subsystem test change.
