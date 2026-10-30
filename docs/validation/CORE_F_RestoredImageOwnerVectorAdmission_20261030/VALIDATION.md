# CORE-F restored image owner-vector admission

## Candidate scope

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Tested base: `dbb813fd625f369f76b5e50fe3d59eeaf5f8ad75`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Changed production files:
  - `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` — SHA-256 `cd23f3eb8c845667167c30666b51967ece224f9599c0905ca57f41da72a94af8`
  - `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs` — SHA-256 `f0e24904569f787378fbe9e07a2b24a844e1dc71f013ade438431f3a43d5ffe7`
- Test sources and production behavior outside these two files were unchanged.

The application now requests the private target's unadmitted owner-vector census through its host port and performs the persisted-image comparison under the exact Core restore-staging authority. The Unity restore coordinator continues to construct the compatibility projection and run its existing domain-binding and invariant checks. Core restored-boundary finalization still binds the candidate guard, recaptures the census, issues the restored token, and publishes factual-read state.

This moves the P12 image-to-target census operation into the application. It does not complete the restore coordinator migration, expose an incomplete public `TryStart`, establish detached .NET hosting, or complete CORE-F.

## Validation

- Official CoreValidation, SDK `9.0.315`, `-SkipLab`: cold **936/936 PASS**, warm **936/936 PASS**; run `52f4d5d0ca0146b0a8c380bd23dd83b4`; private manifest SHA-256 `ced8a84921f56d2b6a243b61fb97af83d840da2e551e0698cd38553802c38f4d`. Original cold and warm TRX SHA-256: `c10421be182846d21611e5fd868a1396998dc7524d97f6ed835338c121d1bb57` and `504c821ae362ce6a422862043acc8addecc2f279deaef17bea58a73c283b1680`.
- Focused detached-image restore test `P12GOwnerEntryQualificationTests.IntegratedMilitaryAndSiteKnowledgeOwnersPersistRestoreAndContinueWithExactParity`: **1/1 PASS**; private manifest SHA-256 `7790bba9d786e1942fdf4586e211dd2126dccb727f2721bb4ced143234c96908`.
- Focused atomicity test `SimulationRuntimeAdmissionTests.DailyV1RestoreRejectsPrivateTargetRosterMutationAfterOwnerVectorCaptureAndKeepsSourceRetryable`: **1/1 PASS**; private manifest SHA-256 `4574d41e0107b36340d2c3b642c87df6dc7406482efb04d846aa2ad073a52a3a`.
- Full Unity EditMode: **3279/3279 PASS**, with zero failures, skips, or inconclusive cases; run `d89d8cd6c380464c88c4ea32102c2cf7`; input digest `6051ad3f0051becc1af6328b8a2489a85d96210ca2ae3b378e86483ad79a4c61`; private manifest SHA-256 `2011c25899dabfe12b0812b8c6a595f171b171a35efda7e6da8b53ace3f89437`; original XML SHA-256 `6021a470eb7c1a715d2d31a8f073e7f37b892c7f41a2e0dc235fab36c4065b53`.
- Official project-standard Smoke filter `Smoke`: **5/5 PASS**, zero failed or skipped; run `73212fd99fa64bf4bb88d728e86ee0c3`; same input digest as full EditMode; private manifest SHA-256 `b8c0695faae27a87bc5214a1fa769d1c20ac2e565f8eba957debcc41958f9893`; original XML SHA-256 `49e0da8f2c897a7bfbb25656e516c698a299bd5c7d881668c6c73adf287cb9d5`.
- `git diff --check`: PASS.

The full EditMode result-only archive is `UnityEditMode/coref-restored-owner-vector-admission-editmode-20261030.zip`; archive SHA-256 `7b3eb8c9101a8af55904b1ec3fabce2c4237df49b60286e9cb45da101ed206b8`, package manifest SHA-256 `ba71ce5b8252c267e4208ba0a843903347c7a45b63ad0aac56c4f4a8b932e993`. The approved verifier reported `VALID: runs=1 entries=1`; a local-path and credential-pattern scan of the public manifest found zero matches. The package contains normalized result XML only. The original Unity reports and raw logs remain unchanged in owner-only E: evidence storage.

The CoreValidation runner emitted its private `simulation-core-validation/v1` manifest. The separate public TRX packager requires a verifiable `dotnet-trx-source-context-v1` adapter, which was not emitted for this run. No public .NET TRX projection is claimed; original reports, logs, and the private manifest remain unchanged in owner-only E: evidence storage.

A preliminary `-TestCategory Smoke` invocation selected zero tests and is not counted; the project-standard `-TestFilter Smoke` run above executed all five required cases. The initial focused Unity compile also caught a local-variable name collision in this change; it was corrected before the passing focused and full runs.

## Preserved changes and limits

The pre-existing `ProjectSettings` edits, untracked files, and unrelated private evidence were not staged, modified, or removed. This remains an unpromoted CORE-F candidate. Shared application startup, the remaining restore-composition migration, detached .NET host, and separate-process T/T+1/T+2 continuation remain open.
