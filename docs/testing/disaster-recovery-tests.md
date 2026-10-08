# Disaster Recovery Tests

Recovery testing is separate from routine unit tests. The first repeatable local drill seeds synthetic data, submits incidents, queues AI work, pauses and resumes a worker, verifies queue recovery and duplicate delivery idempotency, backs up the SQL test database, restores to a new isolated `_Test` database, verifies migrations/schema, runs API smoke checks, and confirms media references.

Record StartedAt, DetectedAt, RecoveryStartedAt, ServiceRestoredAt, data-loss estimate, measured RTO/RPO, failures, and corrective actions. Failure-injection coverage includes SQL unavailable, AI/search/weather/storage unavailable, SignalR disconnect, queue redelivery, worker failure, dead-letter path, circuit breaker, migration failure, readiness failure, and liveness stability. Never restore over a production or developer database in an automated test.
