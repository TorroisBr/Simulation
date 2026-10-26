# CORE-F P12-F Core-bound capture — independent exact-tip review

**Verdict: PASS / VALIDATED CANDIDATE.** No actionable findings remain.

- Candidate branch: `codex/core/CORE-F-runtime-app-session-boundary-20261025`.
- Candidate commit/tree: `e1470e40c98cdd3cbf2b0ff2d6d2f67ebd0b5eba` / `f4f5a18982d151168f7ee2fd03e18e690bbd5ce3`.
- Review base: `28bd6d40aa6733a2c18d48ded461dc88014b9aa5`.
- Core canonical inspected: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Review scope: complete candidate delta from the actual base, including the earlier Core-session/P12-F binding and the exact-tip reentrancy correction, tests, and validation evidence.

## Finding resolved

The prior review identified that later P12-G source-token checks omitted `DailyV1P12GContinuationSaveOperation`. Those checks now all receive the Core operation. The coordinator revalidates immediately after target checks and after boundary admission. Continuation-image composition validates the same active operation and exact source token before and after composition, rather than falling back to Unity token validation on the Core-bound route.

Three Unity regressions close the operation from callbacks at `OwnersStaged`, `TargetChecksCompleted`, and `BoundaryAdmitted`. Each proves no image/restored session is returned, the exact active Core session remains published, the operation count returns to zero, and a later save operation can begin. The conditional legacy route remains unchanged.

The earlier positive findings remain: PoliticalKnowledge is bound once to the selected graph and its exact PersonStore, source snapshots are tied to the Core operation witness and census, and legacy capture remains separately routed.

## Independently checked evidence

- Focused regression: 3/3 passed.
- Full Unity EditMode: 3,269/3,269 passed.
- Official Smoke: 5/5 passed.
- All three successful manifests report a stable, identical validation-input digest. Changed Unity production/test file hashes match candidate source bytes.
- The normalized full EditMode archive and manifest verify against the protected original and contain all 3,269 results.
- CoreValidation 820/820 cold/warm remains applicable because the correction changes no Core source/test input; the reviewed Core input hashes match the prior run manifest. The runner reported `worktreeLibraryAbsent=False`.
- `git diff --check`: PASS.

Evidence boundary: the Unity/CoreValidation run manifests carry the pre-correction base commit/tree fields, not the final candidate commit identity. Source/test input hashes and the stable input digest establish tested byte applicability to the exact candidate. The failed no-result compile attempt is retained but excluded; the corrected focused run and required full suites passed. Unrelated `ProjectSettings` modifications were not changed or included.

## Disposition

This review validates one intermediate P12-F capture-boundary slice only. It does not promote Core canonical, declare the shared Daily-v1 application complete, or establish detached .NET continuation or full CORE-F acceptance.
