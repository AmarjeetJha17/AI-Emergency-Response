# k6 workload

Install k6 and run against a dedicated non-production environment:

```powershell
$env:BASE_URL = "https://test.example.invalid"
$env:BEARER_TOKEN = "<short-lived test token>"
k6 run tests/performance/k6/incident-api.js
```

The manual k6 profile defaults to 10 VUs for two minutes. A scheduled baseline can use `VUS=50 DURATION=10m`. The initial profile measures incident reads and dashboard reads; expand it as stable auth fixtures and media/search API contracts are added. This profile is not currently wired into CI. Never use a production token or send real citizen data in load tests.
