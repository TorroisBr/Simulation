# CORE-F P12-E Justice Core capture — exact-tip review

**Verdict:** `VALIDATED_CANDIDATE`
**Review date:** 2026-10-29 (America/Sao_Paulo)
**Reviewer:** independent read-only review; candidate code, tests, and validation artifacts were not modified.

## Exact identities and remote preflight

- Repository origin: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate tip: `dbb813fd625f369f76b5e50fe3d59eeaf5f8ad75`
- Candidate Git tree: `4f1f48294def701e26d5eb0d7498a2d9621375e5`
- Actual comparison base: `a23ed4ad03e332bcfd147bc42a8ef459155d3ad4`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Refreshed remote candidate and canonical resolve to the exact SHAs above.
- Candidate ancestry from the comparison base is clean.
- Reviewed production implementation remains commit `0893555a063ec8848264150ffd70c2a067006e0a`, tree `f4aa6da5236ea96d98cbd84598057a490c557278`.
- The added test source is blob `0c73f20fe7437fed6eb45e293499e4d2926ac79c`, SHA-256 `5132e3ac13b530a2e3b7f5ac91283bcf5a67473f469b57a7f1de662ab568a532`.
- The prior exact-tip review of `38f4bbe815d96ef595a1d0797c32f24395cf1bc8` is `NEEDS_CHANGES`, durably recorded at `b977df21e86a636b88e9d10d2cef084537702be5`. That review found no production defect and identified one test-coverage gap.

## Finding closure and implementation review

The prior review required a populated selected-Core Justice capture with a materialized Person reciprocally bound to the Justice target, plus assertions for the captured PersonId and encoded image.

The candidate closes that finding additively in `SimulationRuntimeAdmissionTests.SelectedCrimeJusticeDailyStageContinuesAcrossPublicDailyAdvances`:

1. It registers a Person through the selected runtime, binds that Person to the selected Justice target through the runtime's existing binding API, and asserts the target's exact `PersonId`.
2. It exercises the existing selected Justice operation and public daily-advance path, then captures through the active Core continuation-save operation.
3. It asserts the exact PersonId on both the populated wanted row and sentence row, while retaining the existing identity, owner revision, Justice/receipt witness identity/cardinality/revision, token freshness, P12 encoding, and restoration assertions.
4. It compares the Core Justice encoded row payloads with the composed continuation image, including the encoded row identities and payload bytes.

The production source was unchanged from the previous review. Its selected-Core capture resolves the optional Person through the selected graph's exact `PersonStore`, requires the forward and reverse lookups to identify the same materialized Person, and revalidates the owner/token witnesses around capture. The package only uses that Core snapshot after checking equivalence with the Unity projection, including Person identity. The newly populated test now exercises this branch directly. The complete base-to-candidate diff was reviewed; the only new executable delta since the prior review is additive test coverage. No existing assertion was weakened, no production behavior changed, and no profile or wire-format scope expanded.

The test separately confirms the bound PersonId on both row kinds, rather than only confirming that the Person exists elsewhere in the owner graph. The prior review's remaining source, identity, ordering, mutation/revision, token, scope, and compatibility conclusions remain applicable because the reviewed production tree is unchanged.

## Exact-source validation and evidence

The changed test SHA-256 is `5132e3ac13b530a2e3b7f5ac91283bcf5a67473f469b57a7f1de662ab568a532`. The focused, full EditMode, and Smoke runs share stable validation-input digest `2a47a8bcf356796bfd171495bef521a7cdfdb4e3795ed0f71d8e55333b9b4d1b).

- Focused populated Justice capture test: **1/1 passed**; protected run-manifest SHA-256 `da199b7e2ed4760d09aaa36694942653aeefcde173d26f4dd300a6dbbb6d25d1`.
- Full Unity EditMode: **3279/3279 passed**, zero failed/skipped/inconclusive; protected run-manifest SHA-256 `9b8496164d925fa142fc81991bead890b996ee9a1a2f3dc184d31f33b3a4b90e`.
- Official Smoke filter: **5/5 passed**, zero failed/skipped/inconclusive; protected run-manifest SHA-256 `83c469af06ebd9c2d8a32227ca1fe3755b894d199a8b29f55c1a43f915dc4ca9`.
- Existing CoreValidation cold/warm: **936/936 each**; applicable because this fix changes only a Unity test fixture, not Core code or tests.
- `git diff --check` on the exact base-to-candidate range: **PASS**.

The privacy-safe full EditMode package contains one normalized XML entry. I independently checked its declared and actual ZIP SHA-256 (`2ea04b2f7ca356700d9547ec5b4344a2762937f046e0449eb73580ffac9da82d)), all 3,279 result cases (all Passed), presence and passing result of the changed test, and all five Smoke cases. Its manifest SHA-256 is `3dbe73bef1718877d06e993b5731afd41728bf8671a78a2ab47a81af98fa48ba`. The recorded official packager private original-to-normalized verification passed (`runs=1`, `entries=1`). A read-only privacy scan found no absolute workstation paths or credential-like strings in the public package or manifest.

The official packager rejected the filtered focused and Smoke reports because their parent Unity suite count did not match the filtered descendants. Their original reports remain protected and unmodified; the complete verified EditMode package independently contains the focused test and all Smoke identities. No original report was edited or reinterpreted.

## Remaining gaps and disposition

No actionable finding remains for this bounded capture slice. There is no separate newly added malformed one-sided selected-Core Person-link test; this is a non-blocking coverage follow-up because the required reachable reciprocal-binding case is now executed, while the selected-Core capture's two-way identity checks and existing rejection coverage remain unchanged.

This verdict validates only the bounded P12-E Justice capture candidate. It does not promote the candidate, complete P12-E or CORE-F, close a Phase, or claim full CORE-F acceptance. Canonical promotion remains a separate workflow; the shared application/host integration and other recorded CORE-F obligations remain open.
