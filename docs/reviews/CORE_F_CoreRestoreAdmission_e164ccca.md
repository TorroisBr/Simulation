# Independent exact-tip review: CORE-F restored-source admission

## Verdict

`VALIDATED_CANDIDATE` — no actionable findings remain on the reviewed tip.
This review does not authorize or perform Core-canonical promotion.

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`
- Candidate commit: `e164ccca18c6ce89c69628f148fa58593e748127`
- Candidate tree: `204b0ec706bf62f3d009ef992ac8ee5d7f4d6b38`
- Base: `3cbc25c1ad02d0c679c854c6d2519a61ae2aa83e`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Independent reviewer: `/root/core_restore_admission_review`

The exact implementation diff is limited to
`DailyV1RuntimeAdvanceStage.cs` and
`DailyV1ApplicationSessionGateTests.cs`. The reviewed source/test SHA-256
identities and validation artifact identities are recorded in
`docs/validation/CORE_F_CoreRestoreAdmission_e164ccca.md`.

## Review coverage

The independent review checked restored-session provenance during exclusive
replacement, ordinary completed-boundary admission, staged-target provenance,
stale source-census rejection and preservation of the current session on
rejection. It also checked that a retry with the matching fresh census can
publish the replacement, that the application admission gate remains held
through validation/publication, and that owner identity/cardinality/revision
and epoch requirements are not weakened.

The candidate accepts `RestoredContinuation` provenance for a current source
session only in the replacement-validation path. Ordinary completed daily
boundary admission remains `CompletedAdvance`-only, and the staged replacement
target remains `RestoredContinuation`-only. This preserves the bounded source
replacement contract without broadening ordinary capture/admission semantics.

## Validation evidence reviewed

The reviewer verified the exact candidate identity and source hashes against
the retained evidence records. Existing validation was not rerun during review.

- CoreValidation cold and warm: 748/748 PASS each.
- Unity `SimulationRuntimeAdmissionTests`: 223/223 PASS.
- Unity `SimulationRuntimeOrchestrationTests`: 13/13 PASS.
- `git diff --check`: PASS.
- Original reports, logs and manifests remain owner-only outside Git; they were
  not copied into the repository.

The artifact sizes, hashes, run IDs, input digests and source/test identities
are listed in the linked validation record. No claim is made for full Unity
EditMode, Smoke, LongRun, detached .NET continuation, or complete CORE-F
acceptance.

## Scope and disposition

This review validates the bounded restored-source replacement admission
correction as a candidate checkpoint. It does not close CORE-F, alter Core
canonical, or grant canonical promotion. The independent review found no
remaining actionable issue on the exact reviewed tip.
