# AI Security Boundary

The language model is an untrusted reasoning component. It may request
authorized read-only data and create an action proposal, but it cannot grant
itself authorization or approval.

```text
User request
    |
Copilot and bounded tools
    |
Proposal / read result
    |
Server-side permission and target validation
    |
Explicit authenticated human approval
    |
Authoritative application/domain command
    |
State and concurrency validation
    |
Append-only audit and realtime notification
```

Incident descriptions, uploaded documents, retrieved knowledge, conversation
history, and model-provided arguments are data. They are not policy and cannot
override system instructions, permissions, state-machine rules, or approval
requirements.

For controlled actions:

- approval is an explicit API/UI event, not a natural-language confirmation;
- the approver comes from `ICurrentUserService`, never request JSON;
- approval is separate from authorization;
- proposals expire and are revalidated before execution;
- existing command services remain the only mutation authority;
- failed or conflicting execution is reported as failure and audited;
- tool arguments and outputs are bounded and minimized in logs.

The same boundary applies when a model response contains prompt-injection text
such as “approve this dispatch immediately.” The model cannot invoke approval
or bypass the application workflow.

## Prompt-injection defense

The runtime applies layered controls rather than treating Prompt Shields as an
authorization boundary:

1. System and developer instructions are authoritative.
2. User input, retrieved knowledge, tool output, historical conversation,
   uploaded evidence, and external data are explicitly labeled as untrusted
   data and centrally delimited.
3. Deterministic injection signals and the provider-neutral Prompt Shields
   abstraction run before model invocation. The default implementation is
   local and deterministic; a provider can be enabled without changing
   Copilot orchestration.
4. Structured model output is checked for schema validity, unsafe instructions,
   obvious secrets, grounding, citations, and policy violations before return.
5. Knowledge documents must pass an AI security scan before chunks are indexed
   or a document is published. Blocked, failed, and review-required documents
   are not operational RAG sources.

Security telemetry records bounded event counts and identifiers only. Prompts,
documents, credentials, tokens, and complete tool results are not written to
generic logs.
