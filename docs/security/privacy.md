# Privacy and Data Minimization

## Collection and access

- Do not collect identity or contact details unless the workflow needs them.
- Treat anonymous incident descriptions, exact coordinates, addresses, and media as sensitive.
- Keep original media in private storage. Serve it through application authorization; do not expose permanent blob or filesystem URLs.
- Restrict knowledge administration, incident access, and exports with the same permission and department/scope checks used by the application views.
- Do not put secrets in incident descriptions or uploaded documents.

## AI context

Build AI context from a purpose-specific snapshot containing only required incident facts. Do not pass complete database entities, phone numbers, password metadata, unrelated claims, or internal identity identifiers to the model. Use `ICopilotExecutionContext` as the trusted authorization context; provide roles, department, incident/situation identifiers, and operational permissions only when needed for the task. Review every new AI input field for necessity.

## Logging and telemetry

Log metadata needed to operate and troubleshoot the service: opaque entity identifiers, event type, elapsed time, model/version, and correlation identifier. Do not log authorization or cookie headers, request bodies, credentials, full incident narratives, prompts or responses, file contents, connection strings, or full documents. Exceptions must be reviewed for embedded payloads before they are returned or logged. Keep telemetry attributes free of personal or high-cardinality sensitive values.

## Exports

Authenticate and authorize before generating PDF or spreadsheet exports. Apply the same department, ownership, and record filters as the dashboard, minimize included fields, and audit the export action without copying the exported content into the audit event. An export endpoint must never widen a caller's normal record scope.

## Media and processing artifacts

Validate uploads before storage, keep containers private, and use short-lived delegated access only when a workflow requires it. Delete extracted video frames and temporary processing files after analysis unless an approved evidence workflow explicitly retains them. Do not permanently retain derived media by default.
