# AI Evaluation Strategy

AI evaluation is separate from deterministic unit and integration tests. The CLI has an offline fixture-validation mode for pull requests and opt-in live modes for provider-backed scoring. Offline fixture validation checks the evaluation inputs and known contract cases; it does **not** claim to score a current model.

## Models Evaluated

Live candidate and judge deployments are configured separately using `AI_EVAL_CANDIDATE_*` and `AI_EVAL_JUDGE_*` environment variables. Azure-hosted evaluation uses `DefaultAzureCredential`. The judge deployment should differ from the candidate deployment where practical to reduce correlated evaluation errors. Foundry safety evaluation additionally requires the `AI_EVAL_SUBSCRIPTION_ID`, `AI_EVAL_RESOURCE_GROUP`, and `AI_EVAL_FOUNDRY_PROJECT` settings.

## Prompt Versions

Set `AI_EVAL_PROMPT_VERSION` for each candidate prompt revision. Results record model, deployment, prompt version, dataset version, mode, timestamps, per-case scores, and aggregate metrics. Live incident analysis invokes the production `IncidentAnalysisService`; visual evaluation invokes `IncidentVisualAnalysisService`. Quality evaluation uses the Microsoft.Extensions.AI Relevance, Completeness, Groundedness, and Task Adherence evaluators. Safety evaluation uses the Microsoft Foundry Indirect Attack and Protected Material evaluators.

## Dataset Versions

`Datasets/dataset-manifest.json` is the authoritative schema and dataset version. A dataset change requires a version change and updated counts. The current manifest describes 50 incident-analysis cases, 9 synthetic visual-image cases, 3 synthetic video sequences, 30 RAG cases, 16 Copilot tool-selection/action-safety cases, and 8 structured security cases. These are starter benchmarks and should grow before release decisions rely on aggregate scores.

The incident dataset stores expected facts instead of expected prose and includes easy, normal, difficult, and adversarial examples. Visual assets are locally generated synthetic PNGs; no private evidence is committed. `Data/*.jsonl` contains offline RAG, abstention, tool-use, and security contract fixtures.

## Incident Analysis

Live evaluation reports incident-type and department accuracy, critical-fact precision/recall, numeric extraction MAE, and confidence-range compliance. Severity and priority are not model targets: AI evidence extraction is measured separately from deterministic decision engines.

## Visual Analysis

Live evaluation reports hazard precision/recall, critical hazard recall, emergency classification accuracy, and confidence range. Trapped people, immediate danger, structural damage, fire, flood, and road blockage are high-priority recall signals. The synthetic benchmark is a smoke benchmark, not a substitute for a labelled real-world validation set.

The live video benchmark sends synthetic JPEG frame sequences through the production visual-analysis service. It reports video critical recall and verifies that any critical hazard present in a frame remains represented in the aggregate result. Video cases are simple synthetic progression sequences and do not replace labelled real-world video evaluation.

## RAG

Offline fixtures calculate retrieval Recall@3/5/8, citation validity, abstention agreement, and policy checks over fixture data. Live quality evaluation scores generated answers against supplied evidence contexts. Retrieval and generation are measured separately so retrieval misses can be distinguished from unsupported generation. The Microsoft reporting library caches unchanged judge evaluations under `Reports/cache`.

## Copilot

The versioned tool-selection dataset describes expected tools and arguments. Live quality scenarios exercise answer relevance, completeness, groundedness, and task adherence. Deterministic tool-contract and action-safety evaluators remain independent gates; model wording is never compared to a single golden sentence.

## Tool Selection

`ToolSelectionEvaluator` checks required tools, exact expected arguments, and forbidden tools in `Datasets/Copilot/*.json`. `ActionSafetyEvaluator` verifies expected side-effect counts in `action-safety.json` and denies natural-language, unapproved, expired, wrong-role, stale-state, and duplicate execution cases.

## Grounding

`CitationEvaluator` requires the cited source to exist, be in the retrieved set, and support the claim. `GroundingEvaluator` scores required facts and unsupported claims deterministically. These checks complement rather than defer to an LLM judge.

## Security

Offline security fixtures cover direct prompt injection, document injection, tool-output injection, secret exfiltration, and action bypass. Live Foundry safety checks are opt-in and require configured Foundry access. Side effects are expected to remain absent for adversarial cases. Add sanitized, machine-checkable cases when new attack patterns are found.

## Predictions

`PredictionEvaluator` reports MAE/RMSE for forecasts and precision/recall/F1/ROC-AUC/PR-AUC/Brier score for classification when probability outputs are available. Evaluation must preserve temporal train/validation/test boundaries and rolling-origin forecasts; random splits are prohibited for future prediction. The current prediction dataset is a schema/template contract, not a labelled model benchmark.

## Metrics

Metrics are reported individually. Do not collapse safety, grounding, retrieval, and response quality into one score. Binary facts use precision/recall/F1; categorical extraction uses accuracy; numeric extraction uses MAE/tolerance; retrieval uses Recall@K; forecasts use MAE/RMSE.

## Thresholds

Initial thresholds live in `EvaluationThresholds.json`. They are engineering gates and require calibration against an adequately sized, reviewed dataset. Safety-critical recall and citation correctness should receive stricter scrutiny than stylistic quality.

## Regression Policy

`--compare <approved-summary.json>` compares critical metrics with the approved baseline. A decline greater than 0.02 on CriticalFactRecall, CitationCorrectness, Groundedness, or IndirectAttackDetection fails the run. `--update-baseline` writes a new baseline only after a passing run and should be used only for an intentionally approved model/dataset change.

## Evaluation Frequency

Pull requests run offline fixture validation with no provider credentials. Use `--live incident-analysis`, `--live visual`, `--live video`, `--live copilot`, `--live rag`, or `--live security` to select provider-backed suites; `--live all` runs incident analysis, visual image/video analysis, Copilot/RAG quality, and Foundry safety. Live evaluation is manual/nightly and should use a small PR subset before full benchmark execution. Cached evaluation is appropriate for repeatable offline comparisons; set `AI_EVAL_DISABLE_CACHE=true` when explicitly measuring fresh model behavior.

## Release Gates

Build, deterministic tests, schema/contract tests, offline fixture validation, and critical safety tests are required CI gates. Provider unavailability is a runtime condition and does not turn the PR fixture gate into a network-dependent model call. A release candidate must additionally pass a live evaluation against a reviewed baseline before operational use.

Generated `summary.json`, `cases.jsonl`, `metrics.json`, and `report.html` files are stored under `Reports/YYYY-MM-DD/<run-id>/` and are ignored by Git. The Microsoft Reporting package stores judge response cache data under `Reports/cache/`; CI should upload reports as artifacts. The self-contained HTML report works without extra tooling.

## Known Limitations

The current RAG/security starter sets are intentionally small, and offline rows are contract fixtures rather than current model outputs. The visual/video data are synthetic. Live evaluation requires configured Azure OpenAI and Foundry deployments and was not run in local validation. The Microsoft Safety package is prerelease. LLM-as-judge scores are probabilistic and may vary; use caching for regression comparisons and inspect per-case diagnostics.

AI evaluation is probabilistic. Metrics are evidence of system quality, not proof of correctness. Critical operational decisions remain subject to deterministic rules and human review.
