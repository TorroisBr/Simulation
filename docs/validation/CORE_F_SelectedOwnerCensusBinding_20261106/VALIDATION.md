# CORE-F selected-owner census binding validation

## Scope

This candidate binds the existing selected-profile owner-census source to the exact `DailyV1ExecutionBoundaryState`. Core now owns the capture invocation, permitted outer scope, and mutation-epoch comparison. The existing Unity `ContinuationCensusProtocol` remains the census reader and source adapter; this change does not claim Core ownership of all 89 census readers, a detached .NET census, or complete CORE-F acceptance.

## Source identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Validated base commit: `6a50adcb188e28af020590b98238e61bb45d9f04`
- Base committed tree: `9ae264536b96673ae9bf813d325b717abf1bd642`
- CoreValidation working-input digest: `5bf19e1b69ed65a3552899296b811dd0766336fcc1c71b3611c47401513589cf`
- Unity input snapshot digest: `240d80735cb31c27c89630f89f6477269a4f692c21a88c876bfa0d12d10a40b2`
- A post-run comparison found all 1,974 files in the Unity input snapshot byte-identical to the current worktree.

## Validation

| Gate | Result | Evidence identity |
|---|---:|---|
| CoreValidation cold | 1,022/1,022 PASS | Run `0996b1a27d5f44dc905990841519ed36`; private manifest SHA-256 `d9b61ce567e0f88f8f988b76a8a005f6496f92ce725e9eb6bfd7e5fc7e32df46` |
| CoreValidation warm | 1,022/1,022 PASS | Same run and manifest |
| Focused restored-owner capture test | 1/1 PASS | Run `479ea24258504a7c9414da35db056c9a`; XML SHA-256 `5cb9d1c888b6cb90f11b465e3e44f1b91fd3f3a14c82969c02bd068715413616` |
| Full Unity EditMode | 3,300/3,300 PASS | Run `b05cfea8108d466e85dd2ac3dac4130c`; input stable; full XML SHA-256 `004aa609deab8a577843bef56651a71929a234ba56cdbb769e8cf218da9e2e12` |
| Official Smoke filter | 5/5 PASS | Run `bf083f37837a47aba98c8325793ebad3`; each of its five `(full test identity, result)` records is present and passed in the full EditMode result |
| `git diff --check` | PASS | Checked after implementation |

The full Unity EditMode result is packaged using the approved evidence adapter. Its manifest is `Evidence/coref-selected-census-editmode.manifest.json`; archive SHA-256 is `f495ac3091dca076fba8058fda4bbbff7d75d61c60297075dede3419dcc996ed` (174,644 bytes). The official verifier reported `VALID: runs=1 entries=1` against the retained original run manifest.

The original CoreValidation manifest and cold/warm TRX files remain in protected local retention, unchanged. The approved TRX public packager requires a `dotnet-trx-source-context-v1` record; the CoreValidation runner emitted `simulation-core-validation/v1` instead. The adapter rejected the runner manifest (`source context contains unsupported key 'runId'`). No source-context record was fabricated, no TRX was rewritten, and no public Core TRX package or original-to-public comparison is claimed. Private manifest SHA-256 is recorded above; report SHA-256 values are cold `182f08325b41eec777f56ae890af4ee3ec0760252b4df5c7ca858f33b6af2a1f` and warm `16501f95b807fd7e3e0fbcb8770a4f1ed109a7d0a5cecf7a0858d3cf0a5654e3`.

## Evidence boundary

The tests establish the selected source identity binding and Core-mediated capture boundary for this candidate. The provider registrations and 89-family capture implementation remain in the existing Unity source adapter. This checkpoint does not establish a detached .NET host, complete owner-source extraction, all-profile census support, or full CORE-F completion.
