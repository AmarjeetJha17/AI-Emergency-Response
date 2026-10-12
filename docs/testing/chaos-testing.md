# Chaos Testing

## Safety boundary

Fault injection lives only in `EmergencyResponse.LoadTests` test-host service replacements and EF interceptors. The API has no runtime chaos switches. The `chaos` command currently requires `LocalTestHost` and a dedicated SQL database whose name includes `LoadTest`; it never targets a production endpoint.

```powershell
$env:ConnectionStrings__LoadTestDatabase = "Server=localhost;Database=EmergencyResponse_LoadTests;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run --project tests/EmergencyResponse.LoadTests --configuration Release -- --scenario chaos
```

The run report lists every check and returns a non-zero process exit code if a contract fails.

## Implemented deterministic faults

- AI fake unavailable: an incident must still be created and remain readable.
- Message publisher unavailable: an incident still commits and its outbox work remains pending in SQL.
- Search fake unavailable: Copilot must return insufficient grounding with no citations and require human verification.
- Weather fake unavailable: incident creation/read still works, and synthetic weather must not be presented as provider data.
- SQL delay: inject 100, 250, 500, and 1,000 ms before each EF command and record the observed request time/status.
- SQL failure and recovery: fail intercepted commands, then remove the fault and verify dashboard recovery.
- Outbox publisher outage/recovery: see the `queue` scenario; jobs remain in SQL until publication recovers.
- SignalR disconnect/reconnect: see the `signalr` scenario; it reconnects and refreshes an authoritative REST snapshot.

## Contracts and current gaps

The initial SQL failure test confirms the HTTP failure path and subsequent recovery; it does not kill the database service or characterize every timeout/retry boundary. Weather currently has no persisted explicit degraded-state response contract, so the test checks continued incident functionality and absence of fake weather values. Search degradation is tested through the Copilot response contract. Azure provider 429/timeout/5xx behavior, Azure Service Bus dead-letter and worker scaling, storage outages, OS/network-level SignalR loss, and real dependency circuit behavior require a staging integration harness and remain outside local fake injection.

Do not equate a SignalR disconnect with lost state. Clients reconnect and refresh from API/SQL. Do not equate an AI outage with incident loss: the incident and its outbox work are durable before processing.
