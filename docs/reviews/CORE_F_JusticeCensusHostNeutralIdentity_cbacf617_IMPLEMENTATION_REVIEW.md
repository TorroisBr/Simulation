# Independent Candidate Review — CORE-F Host-Neutral Justice Census

## Verdict

**VALIDATED_CANDIDATE** for the bounded Justice census witness change. No actionable implementation or test-integrity finding was identified. This review does not promote Core canonical or complete CORE-F.

## Exact identities

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Reviewed implementation: `cbacf61759a6403c316148c9a3a15b4c5fa2b073`
- Implementation tree: `e57c6ba1b0193e0eea1dfea8fcf81daf1640c7ea`
- Exact base/parent: `71c48a255b9e28d3142cf57650d5b5cc3ae66afd`
- Current candidate tip: `a48ceef71ad164ab0f906434925998f9a6c0715f` (docs/evidence-only after the reviewed implementation; tree `09c7829e68ff9399ae10904b97bc50b3b16a3dbc`)
- Core canonical and matching remote ref: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote candidate ref matched current tip `a48ceef71ad164ab0f906434925998f9a6c0715f` at review.

The complete implementation diff from base contains exactly two files: `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` and `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs`. The subsequent candidate-tip commit adds only the compatibility and validation records/package. Existing unrelated worktree changes were outside the candidate and left untouched.

## Review findings

The new Core helper requires the selected Core Justice owner, a non-null host identity, and a nonnegative receipt revision. It derives the wanted-plus-sentence record cardinality with checked arithmetic and derives the record revision from that exact owner. It creates exactly the existing Justice-record and Justice-P18-receipt witness rows, with the same reference identity, the existing schema, record cardinality/revision from Core, and the fixed receipt cardinality of one plus the host-supplied receipt revision. Invalid owner facts and arithmetic overflow fail closed.

The validator reconstructs those expected facts and rejects missing or duplicate Justice rows, wrong schema or role, identity alias changes, stale revisions, and cardinality mismatches. Both normal capture and restored-target capture use this validator before completing their owner-vector composition. P12 section identifiers, roles, payloads, persisted identity, and wire semantics are unchanged. The P12 owner ledger continues to require the shared Justice identity, wanted-plus-sentence cardinality, owner-local record revision, and receipt slot/revision.

The added regression supplies one opaque host-local identity for both rows and verifies it is shared and distinct from the Core owner object. It then exercises the Core capture gate and checks Core-derived record cardinality/revision. Its rejection matrix covers absent/duplicate rows and wrong schema, role, identity, cardinality, and revision for both record and receipt rows. The Unity implementation remains unchanged: its port still verifies that `JusticeSystem.CoreOwner` is the exact selected graph owner and returns the `JusticeSystem` identity and receipt revision.

The existing host-port contract requires an exact projection identity for the supplied selected Core owner. A future detached host must implement that binding itself and supply its corresponding receipt revision; this candidate does not implement or validate a detached Justice adapter. That is a recorded integration limit, not a defect in this helper.

The General Architect's focused compatibility review passed for this exact candidate/tree and confirmed that the opaque shared identity is compatible with the accepted P12 contract. No architectural reconciliation was required.

## Validation evidence

- Official CoreValidation run `3d97c194e2374835af3bfdaed6ccb1ac`: **1,078/1,078 cold and 1,078/1,078 warm**, zero failed or unexecuted; `inputs.stable=true` and generated dependencies stable. The protected manifest SHA-256 is `6a412294e7e9b0ec4ab2fdbd82bf556198c4e9cd3a7819400afa91caf992d5e0`; original cold and warm TRX SHA-256 values are `659c79e81e525cd3e403efdfbcf1a6d860419126c7b7f466899cafa76e0e080b` and `2d960fc6d471e60e5fe296bd33b209f923677c82bf8b43a5b86f8d8514643154`. Both changed candidate files match the protected manifest: Core source `0f03b210a802ff3806b70705a84d32c5d60ce30bcdbec2af36ef974648fc2ae2`; test source `80c9005766e0d20dde2b00ccbcdc9bd5d8a9305bd0a59479783c2ee055a65aef`. The manifest records the parent as committed HEAD with those exact candidate files as stable working-tree inputs.
- Official Unity EditMode run `2ebf81e1c35844d18ac5e6097b2ab8dc`: **3,303/3,303**. Its stable input snapshot identifies the implementation commit/tree above. The captured source hash for `DailyV1RuntimeAdvanceStage.cs` is `2b4452732ae52d8348ddffe337f2b016f389d92358be969c7d244a3cde409bb6`; it matches the candidate source after the run's CRLF line-ending representation is normalized against the LF Git source. Original XML and raw-log SHA-256 values are `baa4b140837faa01f2b4eb8ea06f6dc1c421ac3a0375817dae82f380cf3bdb38` and `3bafd932ee9b73ae7183d944148ca43acde0f564b141764e5fdc358ceaed920a`.
- Official filtered Smoke run `0fe237cc493f4720859d61221a71a8c9`: **5/5**; its cases are included in the full EditMode package. Original Smoke XML SHA-256: `a1f932c548442952ddd5030e9dd1b583e99c4668727a7a8f5a366804f2248506`.
- Privacy-safe Unity package archive SHA-256: `398bf46aca556fc9a115d8d521b3e8525ffd80b8a781096a9c173a5615f8fbec`; package manifest SHA-256: `047f5508097d9c2b6a0773d7bc090cdadc7d4312e50446d460e6e6adde1140bf`. The recorded official verifier result is valid, with one run and one archive entry.
- `git diff --check`: PASS on the implementation diff and committed implementation.

Protected raw reports and logs remain private; this record contains no raw evidence or workstation paths. The review did not rerun tests.

## Scope and limits

This is a bounded census-projection capability only. It does not implement a detached .NET host or its adapter binding, change P12 persistence behavior/profile scope, claim full CORE-F integration, or authorize canonical promotion or phase closure. Full CORE-F runtime/restore and separate-process continuation remain outstanding.