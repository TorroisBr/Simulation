# Independent exact-tip review — selected TravelParty start through Core

**Verdict: VALIDATED_CANDIDATE / PASS.** No actionable implementation or test-integrity findings.

## Reviewed identity

- Candidate tip reviewed: `aa8abb7d79a0b8dd7a5989dd9a3400203a8633cb`
- Implementation commit: `97213fca8ce070abd2d758b99b1d8db6d614c077`
- Implementation code tree: `fd95a2d6a1a7e3d779a32ec558867f6b43ab9119`
- Candidate parent: `3f9e0b1f653951b83d7b4f8b487aa69d08216a0b`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Branch: `codex/core/CORE-F-restore-stage-transaction`
- Review scope: full 11-file implementation/test diff plus the validation record and public package.

The reviewer inspected the exact Core owner binding, participant and role planning, route/cost resolution, session gate, P12 preparation/admission, mutation and rollback ordering, revision/epoch paths, mixed-roster fallback, and all affected test assertions. No previous assertion was weakened. The selected Core path fails closed for foreign selected bindings. Genuine unselected roster members retain the prior whole-roster Unity compatibility path. The Unity callback only prepares/adopts the P12 operation and projection for the Core commit; it does not replace the authoritative Core transition.

## Evidence review

The official CoreValidation, focused P12, full EditMode and Smoke results are recorded in [`VALIDATION.md`](../validation/CORE_F_SelectedTravelPartyCoreStart_20261109/VALIDATION.md). The reviewer verified the exact candidate source identities and the full EditMode evidence package. No code or test source changed after validation. The full EditMode package verifier passed; the full and filtered Unity input snapshots match.

The CoreValidation raw TRX has no public DotNetTrx package because the approved adapter requires a separate source-context schema. The filtered Smoke XML cannot be packaged separately because its aggregate suite count retains the full-suite count; the same five Passed identities are present in the verified full EditMode package. These limitations are accurately recorded and do not change the test outcomes reported.

## Limitations

This is an incremental selected TravelParty-start slice. It does not complete general `DailyV1Application.TryStart`, detached restoration, the fixed .NET host, separate-process continuation, full CORE-F acceptance, or Core-canonical promotion. The review is not canonical promotion. Protected ProjectSettings and unrelated local evidence were not part of the candidate diff and remain untouched.