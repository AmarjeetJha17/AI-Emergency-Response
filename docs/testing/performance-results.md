# Performance Results

This file is the index for selected reviewed benchmark summaries. No benchmark numbers are recorded here until the scenario has actually run and its environment, build, and dataset are known.

For each retained result, include:

- Run ID, date, commit/build SHA, scenario, workload profile, duration, and warm-up.
- Environment and topology (local TestServer, staging API, SQL tier, worker topology, SignalR service).
- Dataset size/version and geography distribution.
- Users/connections, requests, successes, failures, requests/second, and P50/P95/P99.
- Process/host CPU and memory, SQL query and resource metrics, queue depth/oldest age, and SignalR delivery/reconnect metrics where relevant.
- Provider model/deployment, token count, retries, 429s, estimated/actual cost, only for a separately controlled real-AI run.
- Known limitations, bottleneck findings, and comparison to the accepted baseline.

Keep full NBomber and trace exports in CI or controlled artifact storage. Do not commit large raw reports or secrets/tokens.
