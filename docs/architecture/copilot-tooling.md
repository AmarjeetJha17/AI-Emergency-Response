# Copilot Tool Calling

Phase 14 Part 2 exposes current operational context to the Emergency Response
Copilot through bounded, read-only application tools.

## Safety boundary

The model receives only `AIFunction`s backed by application services and
purpose-built DTOs. No mutation function is registered. The copilot cannot
create incidents, change severity or priority, assign or dispatch resources,
acknowledge or resolve escalations/situations, or send notifications.

Each request creates a scoped `ICopilotExecutionContext` from the authenticated
principal and the trusted page context. User ID, roles, department, and page
IDs are never supplied by the model as authorization data. Incident access is
checked again inside each tool, with department-scoped access for responders
and broader access only for operator/admin roles.

## Available tools

- Incident and trusted current incident.
- Situation and bounded member incidents.
- Open escalations.
- Resource summary and advisory recommendations.
- Latest incident environmental observation.
- Current-user notifications.
- Advisory escalation-risk prediction.
- Authorized emergency knowledge search.

Results use `CopilotToolResult<T>` with success, data availability, safe error
codes, and retrieval timestamps. Tool arguments are validated and collection
results are bounded. Recommendations and predictions are explicitly advisory.

## Invocation and audit

`FunctionInvokingChatClient` automatically executes requested functions and
continues the conversation for at most five iterations. Tools are supplied per
copilot request and have descriptions that state their read-only behavior.

Tool calls are correlated to `CopilotInteraction` through
`CopilotToolInvocation`. Only bounded argument metadata, result type, status,
failure category, and duration are persisted; full operational result payloads
are not written to generic audit logs. Tool call/failure/duration metrics use
tool names and timing, not question content.

## Provider degradation

Azure AI Search remains optional. When it is unavailable, live read-only tools
can still provide current application data, while knowledge procedure answers
must identify search unavailability or insufficient evidence. Tests use
deterministic fake services and do not require Azure credentials.
