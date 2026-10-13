# Queue Backlog and Consumer Recovery Runbook

Owner: Technical support lead/SystemAdmin. Status: Draft; durable staging recovery and backlog telemetry pending.

## 1. Symptoms

Old pending jobs, growing outbox/Service Bus depth, dead letters, repeated retries/faults, missing consumer heartbeat or unchanged AI status despite healthy HTTP endpoints.

## 2. Impact and severity

Delayed AI/context/duplicate work can make assistance operationally stale. Core intake continues only when its transaction and required durable enqueue contract succeed. HTTP liveness cannot prove queue recovery. Loss of required work or duplicate operational effects triggers suspension.

## 3. How to confirm the issue

Identify transport and job kind first: SQL outbox, one of the incident-ai/visual-analysis/duplicate-detection/external-context Service Bus queues, or a process-local channel. Inspect pending enqueue timestamps, oldest age, status/attempt count, consumer logs and dead-letter reason. A completion-age histogram is not oldest pending age. queue_age/worker_lag/worker_fault_total are declared without confirmed runtime recording; use authorized durable-state diagnostics while this gap is unresolved.

## 4. Immediate containment

Pause optional generating workloads if backlog grows; preserve outbox/queue/dead-letter records and correlation IDs. Notify operators that pending is incomplete. Never purge, clone or bulk replay unknown messages. Keep the API at its approved single-replica limit while process-local paths remain.

## 5. Authorized recovery steps

Support verifies consumer health, SQL connectivity, Service Bus identity/queue roles, provider throttling and poison messages. Restore a failed dependency first. For durable jobs, inspect idempotency/current state and perform a controlled restart/retry through approved deployment/application controls; process one sample before increasing throughput. Classify dead letters and record an authorized replay or retained investigation outcome. Do not create a standalone Worker or add replicas without the durable transport design.

For process-local jobs, a restart may discard work. Reconcile authoritative pending states and use a supported authorized reprocessing path where available; otherwise record lost/unrecoverable work and stop that capability. Do not claim the local channel recovered durably.

## 6. Verification after recovery

Observe processing progress, declining oldest age/depth and successful state transitions within approved backlog bounds. Test one duplicate redelivery and assert one intended effect with complete history/audit. Reconcile pending, completed, failed and retained dead-letter counts. Verify notifications and that optional retry does not repeat dispatch/approval effects.

## 7. Escalation contacts

Support lead/SystemAdmin → operations lead → pilot owner; security reviewer for tampered jobs, unauthorized effects or evidence gaps. [Named contacts](../pilot-scope.md#participants-and-accountability) and baseline backlog tolerance are entry prerequisites.

## 8. Audit and evidence requirements

Record transport/queue/job identifiers, before/after depth and oldest age, retry/dead-letter categories, restart/replay approval, timestamps, idempotency checks and affected incidents. Keep message payloads and credentials out of logs/evidence summaries.

## 9. Conditions for returning to normal operation

Required durable work reconciled and recovery verified; no duplicate effects; backlog within approved age limits and alerts available. Unproven process-local recovery keeps that capability excluded. Formal suspension requires [owner resumption](../pilot-go-no-go.md#stop-and-resumption-authority), not a healthy HTTP response alone.
