# Copilot Evaluation, Grounding, and Memory

The emergency copilot is an advisory reasoning interface. Operational
services, approved knowledge documents, and audit records remain authoritative;
the language model is not a source of truth.

## Trust pipeline

Requests pass through input validation, trusted user/context binding, bounded
read-only tools and authorized knowledge retrieval. Structured model output is
then checked for citation validity, supported claims, and policy violations
before it is returned or persisted as an assistant message.

Retrieved document IDs are carried through the request and citations are valid
only when they resolve to the actual retrieved context. Invalid citations
downgrade the response and are never rendered as verified evidence. Tool and
document content is treated as untrusted data, as is conversation history.

## Conversation memory

Conversations are owned by the authenticated Identity user and may be bound to
an incident or situation. The API checks ownership for every read, write,
archive, and feedback operation. Recent messages are bounded by
`CopilotMemoryOptions`; summaries are historical context and cannot override
current tool results. System and tool messages are stored only for internal
audit and are not automatically shown to operators.

The initial retention defaults are 90 days for conversations and 365 days for
audit data. They are configurable engineering defaults, not legal retention
requirements. Automated cleanup is intentionally deferred.

## Offline evaluation

`tests/EmergencyResponse.CopilotEvaluation` runs without Azure credentials or a
live model. JSONL fixtures cover grounding, retrieval, tool-use expectations,
refusal, citation validity, and abstention. The runner produces a uniquely
named JSON report under the evaluation output `results` directory and tracks:

- Recall@3, Recall@5, and Recall@8
- citation validity rate
- policy pass rate
- abstention pass rate

The Microsoft.Extensions.AI evaluation packages are pinned consistently at
10.10.0 for future provider-backed evaluators. Repository-owned deterministic
checks remain the CI baseline because model-quality scores can vary between
runs. Deterministic safety and code tests may fail CI; quality trend changes
should be reviewed as report regressions rather than treated as proof of
operational correctness.

Run the offline suite with:

```powershell
dotnet run --project .\tests\EmergencyResponse.CopilotEvaluation
```

Do not configure model temperature to zero for the deployed Azure model.
