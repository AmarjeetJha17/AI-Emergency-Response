# Test Strategy

## Scope

This strategy covers deterministic domain and application behavior, API and persistence integration, authorization and security, AI quality and safety, performance, and recovery. Tests use the existing UnitTests, IntegrationTests, and CopilotEvaluation projects.

## Test levels and categories

The largest test layer is deterministic unit coverage. Integration tests exercise the API and persistence boundaries with isolated test data. Security tests focus on permission decisions, ownership, scope, and misuse. AI evaluation is offline and machine-checkable by default; live provider checks are a separately controlled smoke run. End-to-end and performance tests cover operational workflows and workload behavior.

Tests use xUnit `Category` traits with the stable values `Unit`, `Integration`, `Security`, `AI`, `Performance`, `Resilience`, `Smoke`, and `EndToEnd`. CI can select a category with `dotnet test --filter "Category=Unit"` (substitute the desired category). Legacy tests that have not yet been classified remain included in unfiltered runs and are tracked in the inventory.

## Environment and test data

Unit tests use in-memory objects or local SQLite where relational semantics matter. API integration tests run against SQL Server, use the fixed `EmergencyResponse_IntegrationTests` database, and apply EF migrations. The suite is serialized and resets that database once before its collection runs. The local connection is in `tests/EmergencyResponse.IntegrationTests/appsettings.Test.json`; CI provisions a disposable SQL Server 2025 Developer service. Never point this setting at `EmergencyResponseDb` or any shared/production database. Test identities, media, and AI dependencies are synthetic or deterministic. No real citizen data, secrets, or production service credentials belong in fixtures.

Run integration tests only when SQL Server is available at the configured connection. In CI, the SQL Server container uses an ephemeral test-only password; it is not a production secret. Override `ConnectionStrings__TestDatabase` locally when your SQL Server host, authentication mode, or port differs. The API test host keeps the production JWT path available for existing authentication tests and selects the test-claims scheme only when `X-Test-UserId` is present. Copilot action services use `TimeProvider`, and the test factory supplies a controllable clock for expiry scenarios.

## Authorization and API contracts

Permission tests now exercise every declared permission against every configured role, alongside explicit role expectations. Incident API contract checks verify pagination shape, unauthenticated behavior, and validation ProblemDetails. Incident ownership has an IDOR test. Department-scope and resource-by-resource IDOR coverage, plus the remaining media/export contracts, are still incomplete and tracked in `test-inventory.md`.

## AI evaluation

Evaluation validates schemas, facts, citations, confidence ranges, tool choice, safety properties, and abstention rather than exact prose. Pull requests run offline JSONL contract fixtures and validate versioned JSON dataset counts and visual asset references. The evaluation console has opt-in Azure OpenAI live runners for incident analysis, image/video evidence, Copilot/RAG quality, plus Azure AI Foundry safety evaluators; these run manually or nightly when credentials are configured. Incident evidence extraction and deterministic severity/priority decisions are evaluated separately. RAG evaluation measures retrieval, citation correctness, groundedness, relevance, and abstention. Live provider results are artifacts and must be compared with an approved baseline before release.

## Security, performance, and reliability

Security coverage includes authorization matrices, IDOR, rate limits, controlled actions, and secret-safe logging. k6 is used for load profiles. PR performance smoke is 10 virtual users for two minutes; scheduled baseline is 50 virtual users for ten minutes. Performance targets are initial engineering targets, not SLAs. PR latency results are report-only until runner variance is understood; workload correctness and error thresholds remain enforced.

Reliability tests inject dependency failures and redeliver messages. AI or optional integration failures must degrade only their feature; SQL failure must return a controlled failure where authoritative persistence is required. Liveness measures process health; readiness includes required dependencies. Restore and failover exercises are recorded separately from ordinary CI.

## Coverage policy and release gates

Coverlet reports are stored under `TestResults/` and published as CI artifacts. Target line coverage is 70% overall, 80% for Application/Domain, and 90%+ for critical decision engines. The current measured unit-test baseline is about 5.3% across instrumented assemblies, so CI publishes coverage but does not yet enforce those target thresholds; raising coverage and adding an enforceable threshold check remains required work. Coverage is a signal, not proof. Current release checks include build, categorized unit/security/integration tests, offline AI proxy evaluation, and coverage artifact publication. External provider availability is a runtime condition and does not fail an ordinary software build. Mutation testing of critical rules is optional and nonblocking initially.

## CI stages

Commit/PR: restore → build → categorized unit tests → integration tests → security tests → offline AI evaluation → coverage report → package. The k6 workload is provided for non-production manual/scheduled runs; a provisioned performance environment is not yet part of this workflow. Deployment later adds staging deployment and smoke checks before a separately approved production promotion.

## Quality Principle

No single metric proves system quality.

The release decision is based on:

- Functional correctness
- Authorization correctness
- Security behavior
- AI evaluation
- Grounding
- Reliability
- Performance
- Observability
- Recovery capability
