# CORE-F selected action-status boundary validation

Date: 2026-10-30

## Candidate input

- Base commit: `ceb030343353614b12820b7528ec7e0f2aeeaffc`
- Base tree: `ee6811b8415430abe3183a56e35e9a76e6e1f171`
- CoreValidation source input digest: `ab32af100bfb97715dd1c31fc9e0ea5851ffa52b8d945fce2c4be743b4cb790f`
- The validated change is limited to the selected action-status P12 boundary and its tests.
- Exact changed-source SHA-256 values:
  - `Assets/_Project/Scripts/SimulationRuntime.cs`: `b600a5ad5eede2d5992b2e9a44850a4f567a994d7d5857ce5cea2b39205a4f23`
  - `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedActionExecutionStage.cs`: `fff5e4e663d189c024f6a44929a04a5e8efac1b8861fe8df4c4df6b853d8e1ce`
  - `tests/Simulation.Core.Tests/DailyV1NpcStatusOwnerTests.cs`: `7980910fb00fcd6fc4f0e83639500a7ea5107cf51e026fd9a9031d938ef2eda1`

## Validation results

- Official CoreValidation, run `9087f83e2ef14229a05813f050769cf0`: **968/968 cold and 968/968 warm**; build and both test executions succeeded.
- Focused Unity `SimulationRuntimeAdmissionTests`, run `bf35dcca4755439793a24eff2f42a470`: **230/230 passed** on the exact `SimulationRuntime.cs` and Core stage source hashes above; input snapshot was stable.
- Full official Unity EditMode, run `ed611d8cadac44899390411081bfed66`: **3,283/3,283 passed**, zero failed, skipped, or inconclusive; input snapshot was stable. This suite includes all five official Smoke cases, **5/5 passed**.
- The full Unity run has a verified privacy-safe package in `Evidence/` produced by the packager from Core canonical `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`. The package verifier matched it to the original run manifest.
  - `coref-selected-action-status-20261030.manifest.json`, SHA-256 `1166bb4588547c5a8860aec418f2bc917414615d87e8c624ab228bf9fe1fdc14`
  - `coref-selected-action-status-20261030.zip`, SHA-256 `51b9d5f7a613d1d875607575ba2524fe7506a52de94b802c8261c2115a5c0535`
- The public package manifest contains no absolute local filesystem paths.
- `git diff --check`: **PASS**.

## Evidence limits

The original CoreValidation TRX files and full manifest remain in the protected local evidence store and are not included in this public Unity package. The approved privacy-safe .NET TRX adapter described in `docs/operations/VALIDATION_EVIDENCE_POLICY.md` §11 has not been delivered, so this record does not claim a normalized public .NET evidence package or satisfy any final CORE-F promotion evidence gate. The private original is retained for the independent reviewer to inspect by run ID.

Two later 39-test diagnostic attempts are not evidence for this input: run `84e339cc83fa4ac182f82f3d6bb16253` had an unstable input snapshot; run `693bc22f712846c5847808006672addb` used older `SimulationRuntime.cs` and Core stage hashes (`fab16b9a88bc90cf80b0ffa8ef59411e4fe80a7dcf012c62321bdbf2f71e429c` and `a5c57ed634399e44febecd395b660f2f385eaf552601fa8525bef772255368dc`). The stable full-suite run above matches the current changed production-source hashes.

This is a bounded candidate validation record. It does not claim full CORE-F completion, Core canonical promotion, detached-host readiness, or final evidence-policy compliance.
