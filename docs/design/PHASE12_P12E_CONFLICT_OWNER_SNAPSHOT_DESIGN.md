# P12-E PersistentConflictStore owner snapshot and staged hydration design

**Status:** Current-canonical, docs-only owner design candidate. It does not amend architecture, add a new Phase/checkpoint identity, implement code, or promote capability. The accepted P12-E prerequisite authorization applies. Independent exact-tip design review is required before this owner slice is marked ready for implementation.

**Exact baseline:** P12 canonical `a4ce0abcf261226f4b52fbacc8df9ef8f67a0de2` (`codex/phase12/canonical`), refreshed before authoring. The accepted E design at this base is `15aaee09d3295cff81a48e166b620c89f5156346`; the existing passive Conflict census design is `4b5625b8ac6e8a5ef32771e4fc57b893163c7cf5`.

## 1. Accepted boundary and source finding

This is one P12-E owner slice for the existing, selected Daily-v1 `PersistentConflictStore`. The current live profile inventory already admits `p12e.conflicts` as a **Required** section with schema 1, exact installed owner identity, `Count`, and local `Revision` (`PHASE12_OWNER_COVERAGE_INVENTORY.md`, “P12-E military owner registration and current Daily-v1 writer audit”; `PHASE12_P12E_CONFLICT_CENSUS_DESIGN.md`, “Fixed section”). The empty authored day-zero state is only an initial fixture. Populated rows are supported factual owner state under the accepted E contract and must be exported and staged exactly.

A current production call-site audit found no normal post-publication Daily-v1 writer for the three public store mutations. That is a source finding about the current selected profile, not an exclusion: the Required owner and its supported direct authoritative API facts remain in scope. P16-A/P17-A movement and War-specific facades remain outside Daily-v1 and outside this Conflict slice. Preserve the existing Conflict owner semantics; do not add hostility, resolution, War, Battle, casualty, control, or other gameplay behavior. In particular, Conflict, War, and Battle are distinct authorities as the architecture states.

The existing P12-E technical design requires exact immutable owner sections and private staged candidates, and preserves causal values, stable identities, typed references, revisions, and terminal facts. The P12-E ArmedForce/manpower/position and Battle snapshots are already promoted according to current `PHASE12_STATE.md`; this design adds only the missing exact Conflict section. It does not complete P12-E, implement whole-profile integration/publication, establish capture eligibility, or change P12-A/P13 status.

## 2. Owner and snapshot contract

### Owner identity, cardinality, and revision

- Capture from the exact installed `Runtime.ConflictStore` instance already bound to the selected runtime's ArmedForce owner and mutation guard. The owner reference is an in-process capture check, not serialized identity and not a second owner.
- Consume exactly one `p12e.conflicts` section from the same P12-B completed-boundary token/vector used by other E owner snapshots. Require the exact Required role, schema 1, owner reference, row cardinality, and local revision; reject absent, duplicate, substituted, or mismatched witnesses.
- The owner snapshot has `SchemaVersion = 1`, `RecordCount`, the exact nonnegative `PersistentConflictStore.Revision`, and a detached row list. `RecordCount` equals both the source `Count` and the detached list length. Every `ConflictId` occurs exactly once; rows and child collections use the source's deterministic ordinal identity ordering.
- Preserve the local revision exactly on hydration. Do not replay `TryRegister`, binding writes, or `TryEnd` as a way to reconstruct it. The per-owner revision is not a global mutation epoch.

### Detached row values

Copy each authoritative `PersistentConflictRecord` into scalar/value DTOs without retaining a live owner, record, list, or mutable source collection:

- Conflict: `ConflictId` string, `CreatedAbsoluteDay`, `ConflictLifecycleState`, and nullable `EndedAbsoluteDay`.
- Side rows: parent `ConflictId` string, `ConflictSideId` string, and `DisplayName` (including the source's normalized empty string).
- Participant-binding rows: `ConflictParticipantBindingId`, parent `ConflictId`, `ConflictSideId`, and `ArmedForceId`, each as its exact typed-ID value string.
- Owner header: schema version, record count, and local revision.

All DTO fields are immutable after construction. Lists are defensive copies exposed as read-only collections. Preserve names, identifiers, lifecycle, dates, side order, bindings, and the exact revision; do not synthesize an outcome or infer consequences from terminal state.

### Capture consistency

Follow the existing Battle owner snapshot pattern: validate the P12-B token is for the admitted Daily-v1 profile and a successful completed boundary; require `token.OwnerSections` to be the exact supplied shared vector; confirm the exact installed parent-owner references; read the detached source row view once; copy values; then re-read `Count`, `Revision`, and the matching unique Required owner-section witness. Reject any mismatch or change during the copy.

This check relies on P12-B's already-completed boundary/token and runtime-wide quiescence contract. It does not create an atomic multi-owner read, lock, thread-safety guarantee, owner-thread proof, or a second capture scope. The current passive census contract remains only identity/count/local-revision evidence.

## 3. Existing writers and revision semantics

`PersistentConflictStore` has exactly three supported successful mutation entry points in the current source (`PersistentConflictWarBattleStores.cs`):

| API | Successful change | Local revision | Top-level row count |
|---|---|---:|---:|
| `TryRegister(PersistentConflictRecord, ...)` | Inserts one unique Conflict with its initial sides and bindings. | Exactly +1 after all validation and overflow preflight. | +1 |
| `TryAddParticipantBinding(ConflictId, ConflictParticipantBinding, ...)` | Replaces the immutable parent record with one additional binding. | Exactly +1 after all validation and overflow preflight. | Unchanged |
| `TryEnd(ConflictId, endedAbsoluteDay, ...)` | Replaces an active record with its ended lifecycle/date. | Exactly +1 after day and overflow preflight. | Unchanged |

All preflight/rejected calls preserve record values, cardinality, and revision. Coverage includes invalid/null records and IDs, duplicate Conflict identity, too few/duplicate/mismatched sides, invalid/duplicate/mismatched binding, missing side, missing or (for a newly added binding) inactive ArmedForce, missing Conflict, already-ended Conflict, end day before creation, faulted mutation guard, and revision saturation (`long.MaxValue`). At saturation each otherwise-valid operation fails before any record, child list, count, or revision changes. Initial registration retains existing behavior: its pre-existing participant binding may reference an inactive but registered Force; adding a new binding requires an active Force. Hydration validates registration/existence and the owner's existing invariants, not current Force activity, because lifecycle may have changed after the binding was recorded.

The owner currently has no remove, replace, or public restore API. Its public record objects and nested collections are immutable/read-only; export still copies them into detached scalar DTOs. No extra write path, event-derived revision, or P12-B epoch behavior is introduced by this design.

## 4. Private staged reconstruction and dependency order

Add a typed `PersistentConflictOwnerSnapshot` (or equivalent bounded value object) and a private owner factory following the existing `PersistentBattleOwnerSnapshot` / `PersistentBattleStore.TryCreateFromOwnerSnapshot` pattern.

1. Validate snapshot schema, nonnegative revision/count, exact `RecordCount == Records.Count`, non-null rows, and unique nonempty Conflict identities. Validate each lifecycle enum/date using the same invariants as `PersistentConflictRecord`.
2. Reconstruct detached `PersistentConflictRecord`, side, and binding values using existing typed ID constructors. Preserve all scalar values and source ordering; reject malformed/duplicate data rather than repairing it.
3. Require the exact staged `ArmedForceStore` that will be installed alongside this Conflict owner. Check all Conflict-side and participant-binding relationships against that authority.
4. Build a new unbound private `PersistentConflictStore` candidate from the fully validated records, insert rows through a store-owned internal staging seam, and set the exact captured owner revision without calling gameplay mutation APIs. Check reconstructed count and `ValidateInvariants()` before exposing the staged result. On failure return no staged store.
5. Stage in dependency order: P12-C identity roots and P12-D/ArmedForce prerequisites first; then `PersistentConflictStore`; then `PersistentWarStore`; then `PersistentBattleStore`. War and Battle retain their own optional Conflict and parent-link validation against the exact staged Conflict instance. Conflict does not gain reverse War/Battle collections or duplicate those authorities' facts. Later factories can only see a successful staged Conflict candidate.
6. Return the owner candidate for the larger private graph. It is not published or bound to the live runtime mutation guard here; P12-G owns whole-graph validation, guard binding, and the single publication boundary.

The implementation seam for inserting rows and restoring revision must remain inside `PersistentConflictStore`, since its dictionary and revision are private. `PersistentConflictWarBattleStores.cs` is also shared by War/Battle implementations, so changes there require serialized hotspot ownership. The snapshot DTO/test can live in distinct new files; no `SimulationRuntime`, bootstrap, or capture-eligibility edits are required for this owner slice.

### Typed Conflict relationship validation

Reject the staged owner unless all existing source invariants hold:

- `ConflictId` is nonempty and unique across rows; `CreatedAbsoluteDay >= 0`; lifecycle enum is defined; Active has no end day; Ended has one with `EndedAbsoluteDay >= CreatedAbsoluteDay`.
- Each record contains at least two sides. Every side has a nonempty unique side ID within its Conflict and its typed parent ID exactly equals the containing Conflict ID.
- Every participant binding has nonempty unique binding ID within its Conflict; its typed Conflict ID equals its containing record; its side ID names one of that record's sides; and its ArmedForce ID resolves in the exact staged ArmedForce store.
- An ended record may retain prior participant bindings. Do not require a referenced Force to still be active during reconstruction and do not change the existing rule that a newly added binding is checked for active Force at write time.
- Preserve all cross-owner references owned by War/Battle for their later ordered factories. No reverse link, outcome, or implicit parent repair is added to Conflict.

## 5. Required focused evidence

Add/extend Conflict owner snapshot tests and the existing E composition regression. No Unity or test execution is part of this docs-only candidate; implementation must provide:

- Empty Daily-v1 owner capture and empty round trip, including exact `p12e.conflicts` Required witness, installed owner identity, schema 1, zero rows/revision for the authored initial fixture, and stable repeat capture.
- Populated round trip containing multiple Conflict rows with distinct typed IDs, at least two sides per Conflict, display names, a registered Force binding, both Active and Ended lifecycle/date forms, exact sorted detached rows, and exact owner revision. Verify value equality and no source object/list alias after export and hydration.
- Per-writer revision/cardinality checks: successful `TryRegister` changes count/revision by +1; successful `TryAddParticipantBinding` and `TryEnd` keep top-level count fixed and advance revision by exactly +1.
- Rejection invariance for duplicate IDs; invalid side/binding parent or identity; too few or repeated sides; repeated binding; missing Conflict/side/Force; inactive Force for a new binding; post-end binding; repeated end; invalid end date; faulted guard; and revision overflow. Assert row values, count, and revision remain byte/value-equivalent after each rejection.
- Import rejection for unsupported schema, negative revision/count, count/list mismatch, duplicate Conflict/side/binding IDs, malformed lifecycle/date, missing staged Force, wrong side/parent IDs, and mismatched staged parent authority. Verify no partial store is returned and the snapshot, staged ArmedForce, active runtime, and previously staged owners remain unchanged.
- Capture rejection for a token/vector mismatch, wrong profile or unsuccessful/missing completed-boundary evidence, missing/duplicate/non-Required/wrong-schema/wrong-owner `p12e.conflicts` witness, mismatched count/revision, or changed owner during value copy. These tests consume existing B token behavior; they add no P12-B implementation or quiescence contract.
- Later War and Battle owner-stage tests confirm they still bind to this exact Conflict authority and apply their own typed Conflict references. Do not duplicate their owner facts in the Conflict section.

## 6. Scope, dependencies, and implementation handoff

**Dependencies:** promoted P12-B token/vector and capture contract; promoted P12-C roots/identity; accepted E core-owner scope; promoted ArmedForce/manpower/position snapshot as the required typed parent. The owner itself is independent of the pending P12-D NPC-root integration and does not modify D-owned City/NPC state. It must be revalidated against current canonical immediately before implementation and again before integration.

**Owned files:** new Conflict owner snapshot DTO/factory facade and focused Editor tests; a narrow store-private hydration seam in `PersistentConflictWarBattleStores.cs`; a targeted E composition test. No changes to `SimulationRuntime.cs`, bootstrap, shared P12-B token/vector, P12-D roots, P12-F Knowledge, or P12-G publication. Serialize only edits to the shared Conflict/War/Battle store source file with concurrent War/Battle work.

**Excluded:** P17 War consumers/semantics, Conflict resolution or new lifecycle behavior, daily writers/operations, new census registration/epoch wiring, global quiescence/capture claims, D/F NPC effects, LocalTopology/Ruin, profile expansion, serialization envelope, whole graph restore/publication, and phase closure.

**Readiness:** This artifact is a bounded design proposal on exact base `a4ce0ab`. It is not its own code review, does not report an implementation review verdict, and does not start implementation. After independent exact-tip design review, recheck canonical and source compatibility; if unchanged and review passes, hand off only this Conflict owner snapshot/staging slice for implementation under the accepted P12-E prerequisite authorization. P12-E remains open until all required Daily-v1 owners are covered and integrated; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`.
