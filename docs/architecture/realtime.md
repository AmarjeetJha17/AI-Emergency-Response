# Real-Time Dashboard

The Emergency Operations Dashboard uses ASP.NET Core SignalR.

## Flow

Incident or AI workflow
    ↓
DashboardUpdated event
    ↓
Dashboard operator SignalR group
    ↓
Web /Dashboard/Snapshot
    ↓
Update KPI cards, Syncfusion Grid and charts

## Important

SignalR notifications are hints that dashboard data changed.
The browser retrieves authoritative state from the Dashboard Snapshot endpoint.

The dashboard does not trust event payloads as the source of truth.

That last point is important.

A SignalR event should say:

"Something changed."

not carry the entire dashboard state.
