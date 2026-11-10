# CORE-F action-slot P12 admission review

## Reviewed checkpoint

- Candidate: `b30157564e9d41026e45b00f833c17268ed70bc8`
- Candidate tree: `9aa171d92d4824a9184bdd09d0f0f79eb005dad8`
- Base: `3f7c520197db81b8755b3508b5cccb355c15705d`
- Independent implementation review: **PASS**
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

## Review result

The Core selected actor-turn boundary now derives selected action-slot P12 ownership from its admitted execution state and performs the owner-section admission and committed-revision publication through that state. Unity supplies the live census protocol and the projection-prepared witness; it no longer exposes the Core precommit or postcommit decision methods. Exact graph and actor identity checks remain in place, and failed census checks continue to fail closed.

An earlier review of candidate `4d69c798` found that the focused one-section test setup had also been inserted into the unrelated Justice projection rejection test. Candidate `b3015756` removes that duplicate setup. The Justice test’s original preconditions are restored; no assertions were weakened. The earlier package under `docs/validation/CORE_F_ActionSlotP12CoreAdmission_20261110/Unity/` is retained as historical evidence for the superseded candidate and is not used for this review.

## Validation evidence

- Official CoreValidation on the corrected source passed **1,088/1,088 cold and warm** in the full profile (run `30b68dd7572c418e98bc0349dfd62a99`). The compatible `-SkipLab` profile also passed **1,088/1,088 cold and warm** (run `e41279d20f9f4d3c9fb172bbacfb80a3`).
- Full Unity EditMode passed **3,303/3,303** (run `8f9de16bc77c4cfcbf2041043de65228`). Its verified result-only package is `docs/validation/CORE_F_ActionSlotP12CoreAdmission_20261110/UnityCorrected/coref-action-slot-p12-20261110-corrected-full.zip`, SHA-256 `0e33495d7c217f28b3899573617c7275530928bc93046c91d03bea222e6064f7`.
- Official Smoke passed **5/5** (run `c0ceb25b315a4a178d89646c91e8fd9c`).
- `git diff --check` passed for the corrected source change.

## Evidence limitations

The independent reviewer could not reopen and re-hash the evidence package during its final pass; its verdict is an implementation review, not an independent evidence audit. The official Unity packager independently verified the corrected full EditMode package. The filtered Smoke XML is retained unchanged in protected local evidence, but the result-only packager rejects it because filtered suite `testcasecount` metadata still describes the full suite. The native CoreValidation run also remains protected locally: the DotNetTrx adapter requires its private source-context format and a clean exact-tip input inventory, while this worktree contains protected local ProjectSettings/document changes and unrelated untracked evidence. No context was fabricated and no local changes or reports were altered to force packaging.

The official validation input snapshots were stable during their runs. They include the pre-existing local ProjectSettings change, which remains unstaged and untouched; final promotion still requires reviewable evidence bound to a clean exact candidate input tree. These results do not establish full CORE-F acceptance, P12-A completion, or readiness for Core canonical promotion.