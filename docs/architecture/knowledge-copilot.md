# Emergency Response Copilot

The Emergency Response Copilot is a grounded, read-only assistant for
authorized responders, operators, and emergency administrators. It explains
documented procedures and identifies verification steps; it does not dispatch
resources, modify incidents, change severity or priority, acknowledge
escalations, issue evacuation orders, or close operational records.

## Knowledge lifecycle

Knowledge documents and chunk metadata are stored in SQL Server. Uploaded
documents are kept in private storage outside `wwwroot` and move through:

```text
Draft -> Processing -> Active -> Archived
                    \-> Failed
```

Supported formats are PDF, DOCX, TXT, and Markdown. Text is extracted into
page-aware, heading-aware paragraph chunks. Each chunk has a deterministic
content hash and search document ID so indexing can be retried safely.

Documents are versioned rather than edited in place. Effective dates and
active status are applied to retrieval, while archived versions remain
available for audit and future historical workflows.

## Retrieval

Azure AI Search is the retrieval provider. The logical index is
`emergency-knowledge` and contains searchable chunk content, vector embeddings,
document/version/page metadata, effective dates, and access metadata.

Queries combine full-text search and vector similarity. Results are filtered by
the authenticated user's roles and department, global visibility, active
status, and effective dates. Endpoint authorization and application-level
access context remain mandatory even when the index filter is present.

The provider is disabled when no Search endpoint is configured. Search
unavailability is reported explicitly and never falls back to an ungrounded
LLM answer.

## Grounding

The copilot receives only retrieved authorized chunks and explicitly supplied
current incident or situation facts. Current facts and knowledge sources are
labeled separately in the prompt. Structured responses contain:

- answer
- grounding status
- source citations
- verification notes
- source sufficiency

When no result meets the configured threshold, the service returns
`InsufficientEvidence` and directs the operator to consult the applicable
procedure. Citations are generated from retrieved result metadata and are not
accepted as authoritative model claims without source records.

## Security and configuration

Knowledge administration requires `EmergencyAdmin` or `SystemAdmin`.
Knowledge search and copilot queries require `Responder`, `Operator`,
`EmergencyAdmin`, or `SystemAdmin`. Roles and departments are derived from the
authenticated server-side principal and user record; clients cannot submit
their own access context.

Azure Search and Azure OpenAI credentials must be supplied through user
secrets, environment configuration, Azure CLI credentials, or managed
identity. They must not be committed to appsettings, browser code, or source
control.

## Audit and future work

Each copilot request stores its user, question, optional incident/situation
context, model/deployment, prompt version, retrieved document numbers, answer,
citations, and grounding status in `CopilotInteractions`.

Semantic ranking, multimodal image input, mutation tools, citizen-facing
partitions, preview ACL enforcement, and Azure resource provisioning are
intentionally deferred until hybrid retrieval quality and authorization
behavior are evaluated against a fixed question/source dataset.
