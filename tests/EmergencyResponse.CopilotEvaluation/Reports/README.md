# Evaluation reports

Each run writes `summary.json`, `cases.jsonl`, `metrics.json`, and `report.html` under `Reports/YYYY-MM-DD/<run-id>/`. Microsoft evaluator response cache data is stored under `Reports/cache/`. CI uploads these reports as artifacts. Generated outputs and cache data are ignored by Git; do not commit them.
