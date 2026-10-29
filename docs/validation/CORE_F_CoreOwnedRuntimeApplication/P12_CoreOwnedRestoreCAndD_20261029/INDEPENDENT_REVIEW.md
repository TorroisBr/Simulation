# Independent exact-tip review: CORE-F P12 C/D restore integration

## Verdict

**VALIDATED_CANDIDATE** for the bounded P12-C/D restore integration. This review applies only to the exact candidate below and does not authorize Core-canonical promotion or claim CORE-F completion.

## Reviewed identities

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch at review: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `6c8fa641fdf022094f68700a1e16b5a23c614e7c`
- Candidate tree: `3646fef48b28c069840984466a13139b4f8a3a48`
- Immediate base: `9e8daed31185fbb311691ec473b0dff6052e7ec6`
- Base tree: `02375ae23c3f94e05e72472b58db6dea55983989`
- Current Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` (`codex/core/canonical`)
- The remote candidate ref pointed to the reviewed commit when checked. This record is on a separate review branch whose parent is the candidate.

## Scope and diff identity

The complete base-to-candidate diff has exactly 14 paths: the 11 source/test paths in the reviewed C/D implementation and three scoped validation files under `docs/validation/CORE_F_CoreOwnedRuntimeApplication/P12_CoreOwnedRestoreCAndD_20261029/`. It contains no ProjectSettings, private evidence, or untracked user files. The candidate worktree's unrelated ProjectSettings changes and untracked evidence remained outside the commit.

The validation record declares tracked implementation diff SHA-256 `911c8722b434e7e0c22a01558eda97c2ff06fd8eb0ed047f029435f539118950`. I verified content identity directly against both exact-source input inventories: all six Unity-side changed source/test files match the packaged Unity input snapshot, and all five Core-side changed source/test files match the protected CoreValidation input snapshot; zero changed-file mismatches were found. Both snapshots identify the same base commit/tree and stable working input digest recorded in `VALIDATION.md`.

## Code review

The application/Core path checks the exact restore reservation before image inspection, admits and decodes once, and stages C/D from the same immutable decoded image snapshot. Staged roots remain detached and unpublished until the complete restore package succeeds. Failed staging does not replace the active session or consume its reservation, so the caller can dispose or retry through the existing lifecycle.

P12-D reconstructs the spatial owner and runtime identity index with their recorded revisions; validates exact typed identity membership, global ID uniqueness, owner cardinalities, selected City/NPC IDs and definitions, and restored City/NPC membership; and stages the selected owners before publication. Unity compatibility projections bind to those exact Core-owned owners and the exact identity index without re-registering identities or changing census revision. Legacy no-application restoration retains its existing path.

Added tests cover reservation rejection and retry, malformed-image handling, staged identity collisions/order/cardinality/revisions, exact Core-to-Unity owner binding, and restored continuation parity. No pre-existing assertion was weakened. No actionable code or test finding remains in this bounded diff.

## Validation evidence

- CoreValidation: cold and warm 932/932 PASS; no failed, skipped, inconclusive, timed out, or unexecuted tests. The protected manifest is run `73bd517624f9435bbc39704836693f08`; its SHA-256 is `ae9cdb85346530df9547f5ec33f79a4fd4d644e6e6498880ff54e57e8454fd1d`, matching the committed validation record. Its stable working-input digest is `abea529ba354b5de47344841533e0085b0cff773a10f80ab0e4836ffc3d29f7f`.
- Unity EditMode: 3,279/3,279 PASS, no failures, skips, or inconclusive cases. Run `d0b5f3cf8a8a4fc2bc96a4e06c67f52d`; its input snapshot identifies the stated base and tree. The archive contains one normalized XML report with coherent totals of 3,279 passed and zero failed/skipped. Archive SHA-256 `fdcb4d83fb434fae65cb07bd547ee35641a82c0eefd6a0b2ec51fcadcbc3e64e`; manifest SHA-256 `b676672de6bdd9454a63701d003cd239aed3da2026af73c6f3a3650f25b69a2b`. Both match the committed record and manifest declarations; package verification passed.
- `git diff --check`: PASS as recorded for the frozen implementation input.

The validation record notes that this checkout contained Unity Library; the .NET result does not claim independence from Unity Library.

## Limitation and integration constraints

The new application/Core path decodes the admitted image once. The pre-existing no-application legacy fallback still performs its prior second C decode after full image decoding; this change does not broaden or alter that fallback contract. If a future contract requires a global one-decode guarantee across the legacy path, handle it as a separate reviewed change.

This review validates only the bounded C/D candidate. It does not establish complete CORE-F integration, detached .NET host readiness, phase closure, or canonical promotion. Canonical promotion must run its separate current-base preflight and preserve the exact scope above.
