# P12-E War Owner Snapshot Design — Exact-Tip Independent Review

**Result: PASS — bounded design is compatible on its stated base.** Implementation handoff remains conditional on a fresh P12 canonical/source revalidation after the Conflict snapshot implementation is promoted. This record does not mark the War slice implementation-ready against a later base.

## Exact evidence

- Candidate branch: `codex/phase12/P12EWarOwnerSnapshotDesignA4CE`
- Candidate: `eefefe271cfc1553be120b292015a313a08128dd`
- Candidate root tree: `0a5265df14ac71461995983fea2f7c56b69fec76`
- Reviewed document: `docs/design/PHASE12_P12E_WAR_OWNER_SNAPSHOT_DESIGN.md` (blob `c0378484b14f7a36d20847e132a9a038951a2953`)
- Exact parent and P12 canonical at review: `a4ce0abcf261226f4b52fbacc8df9ef8f67a0de2`
- Effective architecture: `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`; architecture/Roadmap/Execution Model blobs `25843842688239cdc3b80988b2e28dbaa16b4987`, `d03e144544ab25371b71db64538c0de47ae8381c`, `fcd49f34ec8b2f0321cc444bf7b3e612ffd963b2`
- P12 Brief/State at the exact base: `31d9e1b4df41fc994f0a747274e35c4ef40b6e3a` / `700a1fad188049f23819c0beae23ea4e34e1ad1f`
- Candidate is a one-file docs-only addition; its actual parent is the stated base and `git diff --check` passes.

## Findings

No blocking design findings.

- **Accepted owner scope:** P12 Brief and the accepted E design include exact export and private staged hydration for profile-selected official owners. P12 State records Battle and ArmedForce/manpower/position snapshots promoted, E still open, and the `p12e.wars` section Required. The proposal handles populated base War rows as supported owner truth; day-zero emptiness is only the fixture. It preserves one War authority and duplicates neither Conflict nor ArmedForce rows.
- **Exact owner values and mutation contract:** The proposal accounts for every base `PersistentWarRecord` value: WarId, creation/lifecycle/end day, optional ConflictId, all sides and bindings, parent IDs, names, and ArmedForce references. Current `PersistentWarStore` source confirms the three supported Daily-v1 base writes (`TryRegister`, `TryAddParticipantBinding`, `TryEnd`) each increment local revision once after preflight; failures leave rows/count/revision unchanged. It preserves the existing two-side minimum with no new upper bound, uniqueness/parent rules, and the distinction between registered inactive Forces retained in historical bindings versus active Forces required for new bindings.
- **Capture boundary:** `p12e.wars` is the existing Required schema-v1 census section for the exact installed `PersistentWarStore`, count, and local revision. The design binds a detached immutable row set to the existing completed-boundary token/vector, checks the same witness around copying, and relies on P12-B quiescence. It adds no lock, epoch, admission, or eligibility claim.
- **Staged relations:** Current constructors/invariants support the proposed dependency order: staged ArmedForce, staged Conflict, staged War, then staged Battle. War validates optional Conflict and Force references against those exact parents; Battle’s existing staged factory explicitly receives ArmedForce, Conflict, War, spatial authority, and LocalTopology arguments. The design leaves Battle downstream and P12-G responsible for global identity checks, remaining graph validation, guard binding, and publication.
- **Conflict handoff boundary:** The exact Conflict owner design `9ea0e122ce7916ac4f5cd0b5332db6a980c6346d` has independent design review PASS at `f8107d770655b7ba7126a499a55f5bebd2dbb768`. Its current implementation candidate `ae1047169cb41e7d8b01125a3dde8d744280dd33` exposes the expected typed staged Conflict result from the exact staged ArmedForce parent, but is not promoted. The War proposal correctly requires revalidation against the promoted Conflict API before implementation; War and Conflict also share `PersistentConflictWarBattleStores.cs`, so their edits must be serialized.
- **P17 boundary:** The current P12-E contract and P17 records say P17-A adds optional strategic state to this same War owner and selected Daily-v1 rejects that state. The proposal omits no admitted field: capture fails closed for any non-null `PersistentWarRecord.P17A`, the DTO has no P17 field, and staging rejects unsupported data. P17 configuration/concession behavior and its tests remain outside this War slice and unchanged.
- **Architecture and evidence:** Architecture §92A’s continuation-aware owner requirements are met by stable IDs, exact retained values/revision, typed relations, and private reconstruction before publication. §85A does not add a GUI/demo requirement to this persistence-only owner design. The proposed empty/populated, reference, rejection, overflow, detachment, failure-atomicity, and P17-rejection coverage is appropriate. This is design review only; no code was changed and no Unity/tests were run.

## Readiness and limits

The design is accepted as a bounded P12-E War owner snapshot/private staged-hydration proposal on exact base `a4ce0ab`. Before implementation, refresh P12 canonical and revalidate the War source and exact staged Conflict API after Conflict promotion; keep edits to the shared War/Conflict/Battle store file serialized. This review does not promote a capability, establish P12-E completion, claim P12-A readiness, unblock P13, or alter P17 scope.
