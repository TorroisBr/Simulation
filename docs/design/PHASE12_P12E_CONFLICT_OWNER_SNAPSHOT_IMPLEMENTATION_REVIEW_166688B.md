# P12-E Conflict owner snapshot implementation review — exact tip

**Result: `NEEDS_CHANGES` — the implementation is bounded and the recorded suites pass, but two explicit accepted-design regressions are missing.** This is an independent review of the immutable candidate object; it does not edit or promote candidate or canonical code.

## Exact revisions and scope

- Candidate branch/ref: `codex/phase12/P12EConflictCurrentBaseIntegration` at `166688b74016ac092fc5403b193eb21bef1b0b5e` (parent `bfc20e779047cd1d26b6c07428425785fb38d153`).
- Review base/current P12 canonical: `f5d99cb7008023d14a0ed16ea2149a7d7c18def1`; remote canonical ref matched this SHA during review.
- Candidate root tree: `44ec87878a41eb271ff0af6cce57a1bbabfa88f5`. Its committed `Assets` tree is `3b4387f6cc887f69431fdccd92a30d1e73c21cd9`, unchanged across the code-bearing integration, current-base revalidation, and final validation-record commits.
- Accepted Conflict owner design: `9ea0e122ce7916ac4f5cd0b5332db6a980c6346d`; its independent design review is PASS at `f8107d770655b7ba7126a499a55f5bebd2dbb768`.
- Current-base revalidation against `f5d99cb` and its independent document review are recorded in the candidate; the latter is `bfc20e779047cd1d26b6c07428425785fb38d153`.

The complete diff from the current canonical adds the detached Conflict snapshot/factory, seven focused test methods, a narrow factory seam in the shared `PersistentConflictWarBattleStores.cs`, and design/validation records. It does not change `SimulationRuntime`, bootstrap, War/Battle implementation, P12-D owners, `ProjectSettings`, or existing `.meta` files in the committed candidate. The shared-store change remains limited to Conflict staging and does not alter War/Battle paths.

## Review findings

1. **Missing negative `RecordCount` regression.** The accepted design requires import rejection for negative revision and negative count, as well as count/list mismatch. `StageRejectsMalformedIdentityRelationshipsAndCardinalityWithoutReturningPartialOwner` covers negative revision and a positive count/list mismatch, but does not exercise `RecordCount < 0`. The implementation rejects it in `TryBuildRecords`; add the explicit regression so the required contract is protected.
2. **Missing wrong-profile capture regression.** `CaptureRejectsTokenAndRequiredWitnessMismatches` covers `CompletedCoreSequence = 0` and witness/vector mismatches, but every token uses the Daily-v1 admission context. The implementation checks `AdmissionContext.Profile`; add a token with a different profile and assert fail-closed capture, as required by the accepted design.

No production-code defect was found in the reviewed slice. Capture checks the exact token/vector and unique Required schema-v1 `p12e.conflicts` witness, installed owner identity, cardinality and revision before and after detached copying. Exported values are immutable and deterministically ordered. Staging validates lifecycle/date, IDs, side and binding parent relations, cardinality, and Force references against the supplied staged ArmedForce store; it preserves exact revision, accepts historical bindings to a now-terminal Force, and leaves the staged output null on failure. Revision saturation is retained and subsequent writes reject before mutation. The adjacent War/Battle authorities and their references remain separate.

## Validation evidence checked

`docs/validation/P12EConflictOwnerSnapshot/VALIDATION.md` binds the tested committed `Assets` tree to `3b4387f6cc887f69431fdccd92a30d1e73c21cd9`. All nine XML hashes (with Windows checkout line endings), compressed-log hashes, and decompressed raw-log hashes match the manifest. The XML results report zero failures:

- Conflict snapshot 7/7; Conflict census 1/1; Conflict/War/Battle state 9/9.
- Battle owner staging 6/6; War census 1/1; P12-D owner package 26/26; P12-D receipt regression 13/13.
- ALL EditMode 2635/2635; official Smoke 5/5.
- `git diff --check f5d99cb7008023d14a0ed16ea2149a7d7c18def1 166688b74016ac092fc5403b193eb21bef1b0b5e` passes.

The manifest and artifacts are documentation-only additions after the code-bearing tree; the candidate's committed `Assets` tree remains exactly `3b4387f6cc887f69431fdccd92a30d1e73c21cd9`.

## Checkout note and disposition

The existing candidate checkout contains unrelated local edits to `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset`, plus untracked `ArmedForceSpatialPosition.cs.meta`, `ArmedForceSpatialPositionTests.cs.meta`, and `P17ARuntimeTests.cs.meta`. They were preserved and excluded from this exact-commit diff; none was copied into this review branch. The review branch is based directly on candidate tip `166688b74016ac092fc5403b193eb21bef1b0b5e` and adds only this record.

After adding the two required focused cases, rerun the affected owner snapshot suite and regenerate exact-tree validation evidence as needed, then request a fresh exact-tip review. This record does not authorize promotion; P12-E remains in progress.
