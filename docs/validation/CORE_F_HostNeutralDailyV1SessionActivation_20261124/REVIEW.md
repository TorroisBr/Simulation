# CORE-F Host-Neutral Daily-v1 Session Activation

Date: 2026-11-24

## Review disposition

**VALIDATED_CANDIDATE** — independent exact-tip review passed with no outstanding findings. This is a candidate checkpoint only. CORE-F remains incomplete; no Core canonical promotion or Phase closure is recorded here.

## Exact identities

- Repository: `TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate implementation tip: `bbb305790596036efbf41051bd48a71ae711b03e`
- Reviewed source tree: `c66c13e7b7b8afe9066e2c1863bd23bce71876ba`
- Candidate parent: `4619456216d3fb663596eccc48f622cf6a265926`
- Overall implementation base: `e2f7da68e7b71201db4ec26cfb67a57e43a69409`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Live remote preflight: candidate branch matched `bbb305790596036efbf41051bd48a71ae711b03e`; Core canonical matched `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

## Delivered behavior at this checkpoint

- Core composes and seals the selected runtime-operation inventory against the existing operation contract. Tests cover the expected IDs, inventory readiness, unknown-operation rejection, duplicate registration, and incomplete/mixed operation sets that must not seal.
- Core-only Daily-v1 Genesis startup publishes the exact prepared Core session through the normal application transaction; it does not assign `ActiveSession` manually.
- The selected N=10, C=2 proving profile composes and seals all 299 owner-census rows across 89 families, including explicitly empty owners, P12-F owners, Justice records, and P18 receipt authorities. Tests bind providers to the exact live Core owner instances and check their cardinality and revision baselines.
- A complete Core-owned daily advance increments the absolute day and completed sequence, leaves the runtime-operation inventory quiescent, returns the boundary to capturable readiness, and records real owner revision/cardinality and shared-epoch changes. An equivalent host-neutral Genesis/advance run matches the ordered owner facts and sequence.
- CrossCityBuy partial owner commit now fails the Core census protocol closed and revokes the prior capture token. It cannot be normalized into an ordinary action failure followed by a capturable completed boundary.

## Independent exact-tip review

- Reviewer: independent agent `keyed_sale_core_review` (not the candidate author).
- Verdict: **VALIDATED_CANDIDATE**; no new findings on the final test-only change.
- The prior review of production commit `4619456216d3fb663596eccc48f622cf6a265926` confirmed the CrossCityBuy partial-commit correction. The final review inspected the added exact-tip regression at `bbb305790596036efbf41051bd48a71ae711b03e` and confirmed it creates a valid capture token before injecting the partial result, then verifies protocol faulting and token revocation.
- Reviewed files include the full candidate diff from the actual base, including authority, deterministic ordering, failure closure, and test coverage.

## Validation evidence

- Focused CrossCityBuy partial-commit/capture-token regression: **1/1 PASS**; run ID `34c38f4a8ad14a68879fce6e933fbc07`.
- Official CoreValidation: **1210/1210 PASS cold and 1210/1210 PASS warm**; run ID `51a3e3ae91414cda8a38d661ec434605`.
- Official Unity EditMode: **3327/3327 PASS**, stable inputs; run ID `f17d41c1d8784b43a65a3afcf85408f4`.
- Unity Smoke: **5/5 PASS**; LongRun: **7/7 PASS** in the validated regression set.
- `git diff --check`: **PASS**.
- Current source SHA-256 values:
  - `Packages/com.simulation.core/Runtime/Application/DailyV1ActorTurnExecutionStage.cs`: `B858C5008A75731AF90109F49DA3A1B604CD33F4FA9CFE853A191248957FBDF4`
  - `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs`: `8F1541F4A09C499D17781DAF79B67548A3E62849F11A758366272B712503EEC9`
- The final `bbb30579` commit is test-only relative to the production fix at `4619456`. The CoreValidation manifest identifies that parent as its source commit, while the final production and test input hashes match the reviewed candidate. The Unity snapshot records an earlier commit label, but the reviewed production input hash matches this candidate and the final test-only file is outside Unity's compiled input set.

## Evidence boundary and remaining scope

Original reports, logs, and manifests remain in the protected validation store and were not copied into this repository. The raw Core TRX remains private; this record does not claim independent parsing of any raw XML that was not available to the reviewer. The independent reviewer checked the available run metadata and source-input hash correspondence.

This checkpoint proves Core Genesis startup and one in-process Core daily advance. It does **not** prove durable save, detached restore, continuation in a fresh process, full CORE-F acceptance, or canonical promotion. The existing P12 populated-receipt limitations remain unchanged.

## Preservation

Only this review record is intended for the documentation checkpoint commit. Existing ProjectSettings edits, the earlier validation note, protected/private evidence, and all unrelated untracked files are intentionally excluded and remain untouched.