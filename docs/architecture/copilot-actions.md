# Controlled Copilot Actions

The Emergency Response Copilot can create controlled action proposals. It
cannot execute an operational mutation directly.

## Action flow

1. A read-only Copilot tool gathers current context.
2. An enabled `propose_` tool persists an action proposal.
3. The application evaluates authorization and policy and assigns expiration.
4. An authenticated operator explicitly approves or rejects the proposal.
5. Approval revalidates the proposal state and calls the authoritative
   application command service.
6. The resulting state is verified and the proposal is marked succeeded or
   failed.
7. Each lifecycle step appends an audit record and emits bounded telemetry.

The first enabled action rollout contains only `CreateIncidentNote` and
`AddIncidentTag`. Resource, escalation, situation, and dispatch actions remain
disabled even though their domain enum values are reserved for later phases.

## Authorization versus approval

Authorization answers whether the current user may propose or approve an
action. Approval is a separate explicit decision about one persisted proposal.
Approval never grants authorization, and a natural-language response such as
“yes” is not an approval event.

The server records the proposal actor and approval actor from the authenticated
Identity user. It never trusts an `approved` flag or actor ID supplied by the
client.

## Execution and concurrency

Approved actions execute synchronously in the approval request for this
increment. The executor rechecks expiration and status, claims the proposal
with a serializable transaction, and invokes the existing incident annotation
service. Copilot code never updates incident state tables directly.

Only `Approved -> Executing -> Succeeded/Failed` is valid. Rejected, expired,
already executing, and already completed proposals cannot execute again.

## Expiration and audit

Proposals default to a five-minute lifetime through `Copilot:Actions` options.
Expired proposals cannot be approved or executed and are never silently
renewed.

Action lifecycle events are append-only `AuditLog` rows with the proposal ID,
Copilot interaction ID, correlation ID, target incident, actor, status, and
safe result metadata. Raw model prompts, credentials, and unrestricted tool
payloads are not copied into generic logs.

## UI behavior

Copilot responses render enabled proposals as action cards with explicit
**Approve** and **Reject** controls. The UI describes a proposal as pending
until the authoritative command returns success. There is no browser-facing
unapproved `/execute` endpoint and no ambiguous chat confirmation path.
