# Security Baseline

## Authorization

The application has both coarse roles and fine-grained permissions. Permissions
are derived from authenticated role claims by the server. Request JSON cannot
grant roles, departments, or permissions. New sensitive endpoints should use a
permission policy; legacy role policies remain supported during migration.

## Rate limiting

The API uses named fixed-window policies configured under `Security:RateLimits`:

| Policy | Default |
| --- | ---: |
| authentication | 5 requests/minute |
| incident-create | 10 requests/minute |
| media-upload | 10 requests/minute |
| copilot | 30 requests/minute |
| admin-api | 60 requests/minute |

These are initial abuse-protection defaults. Production deployments must load
test limits against expected operator traffic and coordinate limits with any
upstream gateway or WAF.

## File security

Uploads are checked for filename safety, supported extension and MIME pairing,
maximum size, magic bytes, optional malware scanning, and SHA-256 hashing.
Private storage generates keys independently of client filenames. Canonical path
containment is enforced for generated and caller-supplied media keys.

## Web security

Web responses include `X-Content-Type-Options`, `Referrer-Policy`,
`Permissions-Policy`, and a CSP report-only policy. Production HTTPS enables
HSTS through the existing ASP.NET Core pipeline. The Web authentication cookie
is HttpOnly, Secure, and SameSite=Lax. State-changing MVC requests use
antiforgery tokens.

## Operational requirements

- Keep JWT signing keys, AI credentials, Search credentials, scanner credentials,
  and connection strings outside source control.
- Do not log passwords, tokens, API keys, full documents, or unnecessary
  personal data.
- Review CSP reports before moving from report-only to enforced mode.
- Supply a malware scanner implementation before enabling
  `Security:MalwareScanningEnabled`.
