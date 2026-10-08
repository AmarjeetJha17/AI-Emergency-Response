# AI Emergency Response Platform — Threat Model

## Assets

- Incident records and locations
- Uploaded media
- Operator and responder identities
- Resources, situations, and escalations
- Knowledge documents and Copilot conversations
- Copilot action proposals and audit records
- Authentication tokens and external API credentials

## Trust boundaries

1. Public Internet to Web
2. Web to API
3. API to Application services
4. Application to Infrastructure
5. Application to SQL Server
6. Application to AI providers and Azure AI Search
7. Application to external environmental providers
8. Uploaded content to AI processing

## Primary threats and controls

| Threat | Asset/surface | Existing or planned mitigation | Verification |
| --- | --- | --- | --- |
| Broken access control / IDOR | Incidents, resources, knowledge | Server-side permissions, ownership and department checks, safe 404 behavior | Authorization integration tests |
| Account compromise | Authentication | JWT validation, secure Web cookie, rate-limited login, logout invalidation | Authentication tests and deployment review |
| CSRF | Web state changes | MVC antiforgery tokens and AJAX antiforgery header contract | CSRF integration tests |
| XSS and unsafe browser content | Web UI | Output encoding, security headers, CSP report-only inventory | Browser/security-header checks |
| Malicious file upload | Media and knowledge files | Size, MIME, magic-byte, parser, hash, scanner hook, private storage | Invalid-file and traversal tests |
| Path traversal | Private media | Server-generated keys and canonical path containment checks | Path traversal tests |
| Prompt injection | Incident text, documents, Copilot | Data/instruction separation, bounded tools, policy validation, no model authorization | Copilot security tests |
| Action approval bypass | Copilot actions | Explicit API approval, authenticated approver, state revalidation, authoritative command service | Approval bypass and stale-state tests |
| Replay/concurrency abuse | Action proposals | Lifecycle state machine and serializable claim | Duplicate execution tests |
| API abuse / denial of service | Auth, uploads, Copilot | Differentiated named rate limits and bounded request sizes | Rate-limit tests/load tests |
| Secret leakage | Logs, browser, configuration | Secret-free configuration, minimized audit payloads, no token logging | Configuration and logging review |
| Audit tampering | Audit records | Append-only audit event creation and restricted audit access | Audit integrity tests |
| Unauthorized knowledge retrieval | Knowledge base | Role/department/effective-date security trimming | Knowledge isolation tests |

## Residual risk

The malware scanner is an application extension point until a deployment supplies
a scanner implementation. When enabled, the default configuration fails closed
if the scanner is unavailable. CSP remains report-only until browser reports
confirm the required trusted source inventory.
