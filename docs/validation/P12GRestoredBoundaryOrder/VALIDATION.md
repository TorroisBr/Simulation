# P12-G capture-before-staging regression validation

**P12 canonical base:** `02009f9063dd252bd4b177fd6aef1e74dcd947f5`
**Candidate branch:** `codex/phase12/P12GCurrentCanonicalInventoryRefresh`
**Code candidate:** `22de553fb48507c71041a9dfb401c74ac83735d3`
**Candidate tree:** `79bf3ac5365fbf653b2cd513f7edaa1c9a590ddb`
**Assets tree:** `9e915e339bbf398747b17b2c308754756ec63538`
**Reviewed design seam:** `65a16e0cae85aa0b8fbc29bd596b05e0f06c07df`

## Change and result

The test `P12FOwnerPackageCapturesBeforeRootStagingAndStagesAggregateAgainstTheSameAttempt` now calls `P12FDailyV1OwnerCapture.TryCapture` after opening the staging-attempt identity but before `P12CContinuationRootStager.TryStage` allocates staged roots. It then stages the retained detached F capture against the same attempt and preserves the same-attempt assertion. This exercises the already reviewed ActorChoice source-side exact-zero check before C root allocation.

Unity `6000.3.9f1`, using `Tools/UnityValidation/Invoke-UnityValidation.ps1`:

| Suite | Result | XML | XML SHA-256 | Archived log | Archive SHA-256 |
|---|---:|---|---|---|---|
| `P12CPrivateRootCompositionTests` | 53/53 PASS, 0 failed, 0 skipped | `Raw/EditMode-20261009-181001-77c7f7db0178425a828faf7f52feee41.xml` | `3EC6382142A492C6545049115047BCE3589CBA68D4460E434B9A19EC0AFA9A9D` | `Raw/EditMode-20261009-181001-77c7f7db0178425a828faf7f52feee41.log.zip` | `06FC058DD29C3C49E4870EE94EE9D5931B2412D88D24752ADFC8C8EE54F24EEA` |

`git diff --check` passed for the code change. Unity emitted an initial PackageCache missing-file diagnostic during first import; the package file appeared in the completed cache and the harness completed with all 53 tests passing.

## Evidence boundary

This is focused regression evidence for the capture order only. It does not implement or prove the P12-G restore coordinator, complete live-profile inventory, package-wide hydration, completed-boundary admission, coherent active publication, whole-graph validation, P12-A readiness, P13 readiness, or Phase 12 completion. ALL EditMode and official Smoke were not rerun for this test-only change.
