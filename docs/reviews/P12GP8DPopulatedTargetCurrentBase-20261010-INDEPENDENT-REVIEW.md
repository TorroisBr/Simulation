# P12-G P8-D Populated Target — Current-Base Independent Review

## Verdict

**PASS — VALIDATED_CANDIDATE**

- Current canonical/base: `4a11f19c791d9be08e11d52a6f28f26bff05bc70`
- Exact candidate tip: `c9af972f2ecb627be32c2b33ab37163304912971`
- Current-base code commit: `f21c2e11ceb2829ca736434aacd4d1c8ce78fb03`
- Current-base code tree: `9a1bd4c6befbb1ea4a19ca5d6612130b19d5471d`
- Tested Assets tree: `fb53856cec5ed6ca36c170aab7eb6dee90ca3d46`
- Unchanged tested source blob: `27d1be30ba8aa02d02f476c8f737021e20c80b78`

## Exact-tip and drift review

The current-base revalidation classifies the change `BASE_DRIFT_ONLY`: canonical moved from `cabf47185bf4b5d157157a4cb48b4f897db25127` to `4a11f19c791d9be08e11d52a6f28f26bff05bc70` through a docs-only Person-position State correction/review. The P8-D test blob is byte-identical to the previously reviewed source, and the Assets tree is unchanged. The candidate is a clean fast-forward from current canonical; its test change remains test-only, with the rest of the added paths being retained validation artifacts and the current-base revalidation record. No production, ProjectSettings, or profile changes appear.

The unchanged `p8d-route-observation-populated-target` case registers the same stable Person in source and uninterrupted control, constructs a valid direct observation for the existing staged P8-A Location with observed/received day equal to the source completed boundary, and performs the normal `TryRecordObservation` write in the private target. It witnesses the exact target owner identity at cardinality/revision 1/1. The normal target-vector validation rejects the explicitly-empty P8-D target with `TargetOwnerVectorFailed` / `OwnerCoverageIncomplete` before publication. The shared harness checks source session/token/health/graph preservation, deterministic continuation parity, and valid restore retry.

## Validation and limits

The retained validation manifest is tied to the unchanged tested Assets tree and records focused 22/22, ALL EditMode 2,848/2,848, official Smoke 5/5, and `git diff --check` PASS. The focused and Smoke XML outcomes were checked previously; the current-base revalidation requires no Unity rerun because both executable Assets tree and test blob are unchanged. Manifest XML/log hashes are retained but were not recomputed during this review.

This is only target rejection evidence. It does not claim populated P8-D hydration, P12-G/P12-A readiness, complete owner/shared-epoch coverage, capture eligibility, P13 readiness, or Phase 12 closure. The current State's identification of this P8-D case as next is consistent with the work now under review; no status mismatch was found.