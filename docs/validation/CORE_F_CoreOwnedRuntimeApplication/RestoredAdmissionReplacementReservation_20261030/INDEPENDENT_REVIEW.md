# Independent review — restored replacement admission reservation

**Verdict: PASS for the reviewed test correction.** This record closes the specific missing negative case found on the prior candidate. It does not claim Unity integration validation, complete application startup, CORE-F acceptance, or canonical promotion readiness.

## Identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Exact candidate: `54e1b4224f28ba4d23f5f1be4e22cd9dbb42f350`
- Candidate parent: `ec144a1cc025d57b8b89cdf355b8b20a517c13e5`
- Production baseline in this continuation: `299e6a9b0e9eb2a5e4af504c58d0e9d4dd48ecef`
- Reviewed change: one argument in `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs`.

## Review

The earlier negative assertion passed `(null, null)`, proving only that an initial restore is denied while a session is active. The candidate now passes the active `sourceSession` with a null replacement reservation, exercising the replacement request without its required reservation. It asserts rejection and that no admission context is returned.

The positive branch remains within the exclusive operation created for that same `sourceSession`. It verifies restored-context creation and bootstrap lineage before creating and publishing the replacement candidate. No assertions were removed or weakened. The existing production admission guard requires the owner thread, exact expected-session identity, published-session identity and replacement-capture authority; no production change was needed for this finding.

Independent exact-tip review by `/root/restore_core_cut_audit` returned PASS for `54e1b4224f28ba4d23f5f1be4e22cd9dbb42f350` and confirmed that the prior finding is closed with no additional findings.

## Validation and limits

- Official CoreValidation run: `b7f20ddacefa4a07b3b5697899b2154a`.
- Cold and warm CoreValidation: 938/938 passed in each run.
- The official manifest reports stable inputs and binds the updated test source SHA-256 `dd1d867046250bd677b43c61cf3e02fc4fd7b497d7a33a3f8a1d815ad33c1118`.
- `git diff --check` passed for the candidate source change.
- Private runner evidence remains in its protected E: evidence root.
- Focused official Unity EditMode case `P12GPoliticalBindingRestoreTests.IntegratedMilitaryAndSiteKnowledgeOwnersPersistRestoreAndContinueWithExactParity`: 1/1 passed, 0 failed or skipped.
- Unity run: `5654693c6e404f02a361834924b01cf4`; manifest reports `inputStable=true`, Unity executable stable, and current candidate tree `c10fdbcfe766ace0a1a68d55f40732524e965791`.
- Retained private XML SHA-256: `e037fc52e7f692dee7a427abaaf5b0a699ecdb2606ca0dd76a329cb832b71ec6`; log SHA-256: `326b2e9b9fa0042827fa82ea57e1250261db0006b907e056187b3465d5e879c0`.
- The earlier no-result Unity runs remain failed-to-start attempts and are not counted as test failures. This focused pass covers the app-backed initial image restore path; the exact replacement-reservation negative/positive cases are covered by CoreValidation. Full EditMode, official Smoke, and full CORE-F acceptance remain outstanding.

The CoreValidation run used a checkout containing a Unity `Library` directory; it makes no Library-independence claim. No canonical branch was changed.