# Portfolio SignalR CORS diagnosis and verification

## Observed on 29 October 2026

- The Central US API is provisioned successfully and has `AI__Enabled=false`.
- A targeted Azure management query returned no `Cors__...` environment settings and no ingress CORS policy. No secrets were queried.
- HTTPS OPTIONS to `/hubs/incidents/negotiate?negotiateVersion=1`, with the portfolio Web Origin and POST preflight headers, returned 204 from Kestrel **without** `Access-Control-Allow-Origin` or `Access-Control-Allow-Credentials`.
- Unauthenticated POST to that endpoint returned 401 with `WWW-Authenticate: Bearer`.

The request reaches the application; authentication is not intercepting OPTIONS. The API's Staging configuration has no default trusted origins. The isolated API Bicep template previously defaulted `webOrigin` to empty and omitted its CORS environment setting. This explains the missing preflight headers.

## Prepared configuration

`infra/portfolio-demo-api.bicep` now always supplies `Cors__AllowedOrigins__0`, defaulting to the exact trusted origin:

```text
https://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io
```

The application reads `Cors:AllowedOrigins`. Its shared registration rejects wildcards, credentials in URLs, paths, queries, fragments and trailing slashes. Outside Development/Test, absent configuration still grants no cross-origin access. It does not infer trust from request headers or add a global production fallback.

The existing policy supports the methods and request headers used by negotiation, WebSockets negotiation, server-sent events and long polling. Credential support is retained for explicitly trusted origins because the SignalR browser client defaults to credentialed requests and affinity cookies may be needed. It does not use `AllowAnyOrigin`. CORS runs before rate limiting, authentication and authorization; the hub retains `[Authorize]` and bearer-token authentication. CORS is a browser HTTP control, not a replacement for hub authorization or a WebSocket authentication mechanism.

The image reference and secret definitions are unchanged. The currently deployed image already supports the CORS setting; the new application-side configuration validation takes effect only in a future image built from this source. This task does not build/push an image, deploy, rotate secrets or alter Azure settings. Do not run the native-secrets deployment script merely to apply CORS: that workflow requests fresh secrets and must receive a separate review of its complete preview.

## Separate IncidentVolume failure

Browser GET `/Dashboard/IncidentVolume` is a same-origin MVC action that makes a server-side authenticated API request to `/api/predictive/incident-volume?horizon=24`. Browser CORS does not govern that server-side call.

The API forecast controller has no `AIEnabledActionFilter`. `IncidentVolumeForecastService` uses local ML.NET time-series forecasting over SQL data, and is registered even with `AI:Enabled=false`. Insufficient historical data returns an unavailable forecast result via HTTP 200; it is not an AI-disabled 503.

The Web action converts upstream HTTP errors (other than 401/403), malformed JSON and non-caller timeouts into a generic 503. Consequently, the browser status alone cannot identify the upstream cause. The initial short API log sample contained no incident-volume request. A subsequent read-only, aggregate Log Analytics investigation on 29 October established native forecast failures: two `DllNotFoundException` markers for `MklImports`, two `FftUtils` type-initialization markers and two missing `libiomp5.so` markers. No raw logs, exception messages or request bodies were printed. This explains a forecast failure path independently of CORS and Azure OpenAI. See [the release gap review](portfolio-release-gap-review.md) for the unresolved Linux-image dependency and proposed rollout gates.

A local diagnostic invoked the real forecast service with the SQL Server provider and an interceptor that blocks every connection opening: its query translated successfully and the interceptor stopped execution before any SQL access. This rules out a translation failure in the current source for that initial query, but does not verify deployed SQL connectivity, forecast execution or persistence permissions. Do not label the failure an expected AI-disabled response or claim the forecast is operational.

Important: the forecast GET can persist an `OperationalForecast` when a forecast is available. Do not call it as part of a strictly read-only investigation. Do not broaden SQL permissions to make it work.

## Read-only verification plan after a separately approved correction

1. Read only the API's CORS settings, AI flag, ingress settings, revision readiness and image identity. Confirm the exact Web origin is configured, AI remains disabled and there is no ingress wildcard policy.
2. Send this unauthenticated preflight (PowerShell). It does not negotiate a connection or access SQL:

   ```powershell
   rtk proxy curl.exe -sS -D - -o NUL -X OPTIONS `
     'https://ca-er-api-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io/hubs/incidents/negotiate?negotiateVersion=1' `
     -H 'Origin: https://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io' `
     -H 'Access-Control-Request-Method: POST' `
     -H 'Access-Control-Request-Headers: authorization,content-type,x-requested-with,x-signalr-user-agent'
   ```

   Expect 204, the exact Origin in `Access-Control-Allow-Origin`, credentials `true`, POST allowed and the requested headers allowed. A 204 by itself does not prove success.
3. Repeat with `Origin: https://untrusted.example`. Expect no allow-origin or allow-credentials header. It may still return 204; browser access must remain denied.
4. Repeat an unauthenticated negotiate POST, displaying response headers only. Expect 401 Bearer, with CORS headers for the trusted Origin. Do not paste authentication tokens into commands or logs.
5. In an authorized browser session, verify SignalR negotiation/transports and connection status without creating or editing incidents. Keep tokens and connection IDs out of shared diagnostics. Do not reload dashboard forecast panels as a strictly read-only test, because of the forecast GET persistence behavior above.
6. Investigate the existing IncidentVolume failure using existing request correlation IDs and sanitized API/Web log categories/statuses. Do not dump response payloads, connection strings, tokens or raw exceptions. If historical logs are insufficient, report that limitation and plan a separately reviewed reproduction rather than guessing or changing permissions.

No Azure configuration or data changes are part of this plan.
