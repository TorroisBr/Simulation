# CORE-F Selected TravelParty Start Execution — Validation

Status: validated and independently reviewed implementation candidate; this record does not claim CORE-F completion or Core-canonical promotion.

## Scope

The change moves the fully selected TravelParty start transaction into `Simulation.Core`. The Core operation owns allocation, selected owner-plan commits, group charging, party-store publication, active party IDs, the start event, spatial discovery, and compensation. Unity retains authoring projection, P12 section admission/recording, and the legacy fallback for mixed or unselected rosters. A fully selected graph with mismatched Core bindings fails closed.

## Exact validation input identity

Both Unity runs used repository `https://github.com/TorroisBr/Simulation`, branch `codex/core/CORE-F-restore-stage-transaction`, base commit `a50ed01df035cc14c642e9d13b6159f9c4a16cd9`, committed tree `6aaa257435aa7e06dff773a7721a59e749b60728`, and the same stable validation-input inventory SHA-256 `58dfe41cc96012f2dcab35f9209e30f756b9971b12274c48e93ddf6505bfcb72`. The protected run manifests bind the modified source inputs to their individual paths, byte lengths, and SHA-256 values. The relevant implementation and project/test files were rechecked against those captured manifests after validation; they match.

CoreValidation source identity is bound by protected manifest run `ed2ffc1364834c3d93e0cf32d873796f`, base commit/tree above, and working-tree input digest `8272ad48f7e2976d8557f7f870cd434d0ac24595e8729029248083909e062b61`.

## Results

| Gate | Result | Evidence |
| --- | --- | --- |
| CoreValidation cold | 1067/1067 passed; 0 failed, skipped, or inconclusive | Official runner, SDK 9.0.315; private run `ed2ffc1364834c3d93e0cf32d873796f` |
| CoreValidation warm | 1067/1067 passed; 0 failed, skipped, or inconclusive | Same protected run and input digest |
| Unity ALL EditMode | 3302/3302 passed; 0 failed, skipped, or inconclusive | Run `0504a486980d4fb380e44502c763b00d`; input inventory stable |
| Official filtered Smoke | 5/5 passed; 0 failed, skipped, or inconclusive | EditMode filter `Smoke`, run `646ee7149aa94b30b19027902502fdc3`; input inventory matches ALL EditMode |
| Evidence package | Verified | Sanitized ALL EditMode package `Unity/coref-travelparty-start-editmode.zip`; archive SHA-256 `3ce4a01b0799bba320da326a609c4165af0360058e57cc98bd41674e9162fbcc`; package manifest SHA-256 `869e1eb2f8a7dc7d079602d40d0672a1e853a06c7d556e52211bde66caf3db2f` |
| `git diff --check` | Pass | Candidate diff |

The separately filtered Smoke report is retained in protected local evidence and is not included in the result-only archive: the packager correctly rejects the filtered report's inconsistent enclosing-suite counts. All five Smoke identities also appear as passing cases in the packaged complete EditMode report. The protected original XML reports and raw logs remain unchanged. The .NET TRX files and complete private CoreValidation manifest remain in protected local retention; no public .NET package is claimed because the approved TRX adapter is still marked not delivered in the validation evidence policy.

## Independent review

Independent exact-tip implementation review passed. See [`CORE_F_SELECTED_TRAVEL_PARTY_START_EXECUTION_REVIEW_D916BBBB.md`](../../design/CORE_F_SELECTED_TRAVEL_PARTY_START_EXECUTION_REVIEW_D916BBBB.md) for the exact reviewed commit/tree, scope, and evidence verdict. The review does not claim full TravelParty completion, P12 completion, or CORE-F acceptance.