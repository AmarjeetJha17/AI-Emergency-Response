# AI Emergency Response & Disaster Management Platform

Six-slide portfolio presentation. Speaker guidance appears under each slide. Facts reflect the repository and its dated local evidence. The DEMO18 records are synthetic exercise fixtures.

## Slide 1 — AI Emergency Response & Disaster Management

**Portfolio project**

Local synthetic-data demonstration
.NET 10, ASP.NET Core, SQL Server

This is an engineering demonstration, not an operational emergency service.

**Speaker guidance:** Introduce the coordination problem and set the boundary: the platform supports a portfolio walkthrough; it is not connected to real emergency authorities and is not approved for live operations.

## Slide 2 — Emergency operations workflow

- Residents submit incident reports with location and reported facts.
- Authorized reviewers inspect status, impact, related reports, and uncertainty.
- The dashboard and map present incident and resource context.
- Human permissions govern operational commands; AI output remains advisory.

**Intended users:** Citizens reporting incidents; Responders and Operators reviewing work; EmergencyAdmin and SystemAdmin users with higher-level controls.

**Speaker guidance:** Explain that a report is an observation, not a verified fact. Show the distinction between unknown counts and zero. Mention that DEMO18's overlap is a manual exercise grouping, not a confirmed duplicate or automatic cluster.

## Slide 3 — Application capabilities and available evidence

The application includes incident reporting and review, an operations dashboard, a Leaflet map, a resource command center, Copilot, situations, and recipient-scoped notifications.

![Current local operations dashboard](screenshots/dashboard-2026-10-18.png)

**Evidence caption:** Owner-supplied local Operator capture from 2026-10-18. It displays 12 active/unassigned incidents, eight critical incidents, six open escalation alerts, and eight unread alerts. Source revision and dataset identity were not supplied; this screenshot is not a claim of DEMO18 or responsive acceptance.

**Speaker guidance:** This is a bounded screenshot of a local desktop view. Do not treat the shown counts as validated demo fixtures or evidence of message delivery. The resource, situation, Copilot, and responsive workflows remain incomplete or unverified in the current evidence set.

## Slide 4 — Architecture and technology

```mermaid
flowchart LR
  Browser[Browser: Razor MVC + JavaScript] --> Web[ASP.NET Core Web]
  Web --> API[ASP.NET Core API]
  Browser --> Hub[API SignalR hub]
  API --> App[Application contracts and rules]
  App --> Domain[Domain entities and states]
  API --> Infra[Infrastructure]
  Infra --> SQL[(SQL Server / EF Core)]
  Infra --> AI[Configured AI models and embeddings]
  Infra --> Search[Azure AI Search / retrieval]
  Infra --> Storage[Private media and documents]
  Infra --> Outbox[SQL transactional outbox]
  Outbox --> Bus[Azure Service Bus]
  Bus --> Consumers[API-hosted consumers]
```

**Stack:** C# and .NET 10, ASP.NET Core MVC/Web API, EF Core, SQL Server, Razor, JavaScript, SignalR, Bootstrap, Syncfusion, Leaflet, Azure AI integrations, ML.NET, xUnit, Docker, and Bicep.

**Speaker guidance:** The Web app calls the API, while the browser currently connects directly to the API SignalR hub. The API hosts consumers for selected jobs; there is no standalone Worker host, and some queues remain process-local.

## Slide 5 — AI assistance, evidence, and permissions

- AI extraction and Copilot can depend on configured external providers and knowledge Search.
- Show actual grounding and citation state. An unavailable provider is an unavailable result.
- Deterministic rules calculate severity/priority; AI output does not dispatch resources.
- Copilot proposals require explicit approval and revalidation. Current executors support notes or tags, not dispatch.
- Human review remains necessary for uncertain reports, overlapping counts, and operational decisions.

**Speaker guidance:** The seeded dataset contains no generated AI output. The three linked flood reports are manually grouped and their reported counts can overlap. A visible notification fixture does not prove delivery. Never invent citations, predictions, or provider success.

## Slide 6 — Engineering evidence and release status

**Dated local baseline, 17 Oct 2026:** Release build passed with 49 warnings and 0 errors; 572 unit tests passed; 89 isolated SQL integration tests passed; 28 UI JavaScript checks passed; four JavaScript syntax checks passed. These results predate subsequent API startup, SignalR logging, and Copilot memory changes. A focused Copilot fix and regression test are now in the worktree; the current SDK/project-reference resolution failure prevented compilation and test execution.

**Release boundary:** No verified public host or Azure staging deployment. Current screenshots show selected local desktop views, but the complete browser workflow, responsive acceptance, Copilot answer, and resource/situation workflows remain pending. Durable-job recovery, model quality, several security/recovery paths, video scanning/tooling, and pilot acceptance remain open. API startup applies pending migrations in Development and Test before seeding; confirm the selected database and review/backup retained local data before launch. Azure deployment remains out of scope.

**Speaker guidance:** These tests establish their asserted code paths, not production capacity, visual rendering, live AI quality, or operational readiness. Close with the next step: resolve the local migration target, run the real local walkthrough, capture corrected screens, and continue the release-readiness checks. Azure deployment remains out of scope.

## Sources and evidence boundaries

- Repository overview and architecture: [README.md](../../README.md).
- Local setup and migration instructions: [demo-script.md](demo-script.md).
- Synthetic fixture lifecycle: [synthetic-demo-data.md](synthetic-demo-data.md).
- Feature classifications and release status: [feature-verification.md](feature-verification.md).
- UI and viewport status: [ui-ux-verification.md](ui-ux-verification.md).
- Image provenance and capture limits: [screenshots/README.md](screenshots/README.md).

The dashboard image is an owner-supplied local capture dated 2026-10-18; its source commit and dataset identity are unknown. The older Map Console screenshot containing an access token is excluded. The indexed screenshot register records the visible Resource Command Center loading state, empty Situation Center state, and absent Copilot response. No external facts or fabricated application results are included.
