# CORE-F P12-E Core Justice owner capture — exact-tip review

**Verdict:** `NEEDS_CHANGES`
**Review date:** 2026-10-29 (America/Sao_Paulo)
**Reviewer:** independent read-only review; no candidate source or evidence artifacts were changed.

## Exact identities

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Reviewed candidate tip: `38f4bbe815d96ef595a1d0797c32f24395cf1bc8` (tree `12bdddfb7a540ca3e28e8e67738d1f0cf4691644`)
- Reviewed implementation commit: `0893555a063ec8848264150ffd70c2a067006e0a` (tree `f4aa6da5236ea96d98cbd84598057a490c557278`)
- Implementation base: `a23ed4ad03e332bcfd147bc42a8ef459155d3ad4`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote preflight: `origin/codex/core/CORE-F-core-owned-runtime-application` resolved to the reviewed candidate tip; `origin/codex/core/canonical` resolved to the stated Core canonical.
- Code ancestry: the implementation base is an ancestor of the reviewed implementation commit. The later candidate commit is validation documentation/package bookkeeping; the four reviewed code/test files are unchanged from `0893555a`.

The complete code diff is the four-file change from `a23ed4ad` to `0893555a`: `DailyV1P12ECoreOwnerSourceCapture.cs`, `P12EJusticeRecordsSnapshotAdapter.cs`, `P12EDailyV1OwnerPackage.cs`, and `SimulationRuntimeAdmissionTests.cs`. No P12 profile, owner schema, section wire format, or accepted owner family changed.

## Review findings

### Finding 1 — add a populated Person-bound case for the new Core capture path

`DailyV1P12ECoreOwnerSourceSnapshots.TryCaptureCoreJusticeRecords` now reads the selected Core `JusticeRecordsOwner` and resolves each row's optional materialized Person through the selected graph's exact `PersonStore`. The added populated integration case, `SelectedCrimeJusticeDailyStageContinuesAcrossPublicDailyAdvances`, creates a warrant and sentence in the active selected graph, but it does not explicitly establish or assert a reciprocal materialized Person binding for that Justice target. The other added Core-source assertions occur in a nonempty Person/Genealogy restore case whose Justice rows are empty. The existing focused cases that cover both target identity forms and one-sided Person binding rejection exercise the legacy Unity adapter capture/stage path, not this new selected-Core source-capture branch.

Add one focused regression using the active selected Core graph with a materialized `PersonRuntime` reciprocally bound to the Justice target. Assert that the Core snapshot retains the exact `PersonId`, that the Unity projection remains equivalent, and that the encoded Justice row payload/image preserves the same identity. This closes the currently unproven reachable branch without changing product semantics. The production code's two-way store checks look correct on inspection; this finding is a test-coverage gap, not a demonstrated implementation defect.

## Checks that passed

- The capture selects `continuationSaveOperation.Session.OwnerGraph.JusticeRecordsOwner` and the active save-operation boundary requires the exact source token/vector and a successful revalidation.
- The selected-path Unity adapter requires the census `JusticeSystem` projection to be the exact installed projection over that Core `JusticeRecordsOwner`. Legacy calls with no selected Core graph continue through the prior Unity-owned capture path.
- Justice and receipt census rows are each required to occur exactly once. The Justice row is Required and must match the combined Core wanted/sentence row count and local owner revision. The receipt row is Required, cardinality one/revision zero, and has the same exact Unity owner identity as the Justice witness; the existing Unity adapter also checks the live receipt revision.
- Core capture preserves wanted and sentence owner-list order, unique owner-instance rows, exact NPC/City graph membership, exact warrant object-to-ordinal relation, and all serialized row values. Snapshot structural validation confirms sentence target/City/Person identity agrees with its warrant. The optional Person branch checks both the materialized-NPC index and reverse `PersonId` lookup resolve the same exact `PersonRuntime`.
- Freshness is rechecked around source capture and encoding through `TryRevalidate`; owner revision and both census witnesses are checked again after row copying.
- Package encoding uses the Core Justice snapshot for active Core save operations only after field-by-field equality with the Unity projection. The added integration test compares encoded owner-row payload bytes and revision against the composed continuation image.
- Test diff is additive. It adds exact-source Justice capture assertions and corrects the restored-session owner reference to `coreSession`; no existing assertion was removed or weakened.
- The candidate's Unity input snapshot identifies HEAD `0893555a` and tree `f4aa6da5`. All four changed files match its recorded content. Two files have LF checkout bytes in this review worktree while the validation snapshot recorded CRLF; converting those files to CRLF reproduces the recorded byte counts and SHA-256 values exactly. The files' Git blob identities and code tree remain unchanged.
- The public result-only Unity package passed the official verifier: one XML entry, archive SHA-256 `ef03ce87ac2b3aebdfebd6d4fa5148bccfc41e3440e40de72522a7858ec1fbd2`. The XML contains 3,279 test cases, all Passed, with zero failures, errors, inconclusive, or skipped results. The 11 `P12EJusticeRecordsOwnerSnapshotTests` and both changed `SimulationRuntimeAdmissionTests` are present and Passed. A read-only scan found no absolute workstation paths or credential-like strings in the normalized XML or validation record.
- The validation record reports CoreValidation cold/warm 936/936, focused Justice 11/11, full EditMode 3279/3279, and official Smoke 5/5. CoreValidation original TRX/logs/manifests remain private; this review does not claim to have independently inspected those private artifacts.
- `git diff --check a23ed4ad..0893555a` passed.

## Disposition

Do not treat this candidate as `VALIDATED_CANDIDATE` until Finding 1 is addressed or independently resolved with exact-source evidence. This review does not promote code, reopen Phase 12, or claim CORE-F completion. The candidate and its validation package are preserved unchanged.
