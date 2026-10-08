# Recovery Objectives

## Initial engineering targets

These are engineering targets for planning and drills, not contractual service levels or Azure guarantees. Validate them through measured disaster-recovery exercises before treating them as operational commitments.

### RTO — Recovery Time Objective

Target maximum time to restore the platform after a major outage: **60 minutes**.

### RPO — Recovery Point Objective

Target maximum acceptable data loss: **15 minutes**.

## Recovery priority

1. Incident records
2. Operational situations
3. Escalations
4. Resource assignments
5. Authentication and authorization data
6. Audit records
7. Knowledge documents
8. AI analysis and prediction history
9. Notifications
10. Derived dashboards and cache data

Measure actual RTO and RPO during each drill. Azure SQL point-in-time restore, geo-restore, and failover groups are recovery mechanisms with different recovery times and replication tradeoffs; the application targets above require validation against the selected deployment architecture.
