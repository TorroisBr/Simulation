# P12-E PersistentWarStore Owner Snapshot and Staged Hydration Design

**Status:** Bounded technical-design proposal for independent review. It adds no checkpoint identity, implementation, canonical capability, or Phase closure. It uses the accepted P12-E prerequisite authorization and does not authorize P12-A implementation.

**Exact P12 canonical base:** `a4ce0abcf261226f4b52fbacc8df9ef8f67a0de2` (`codex/phase12/canonical`).

**Architecture authority:** `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`; architecture, Roadmap, and Execution Model blobs `25843842688239cdc3b80988b2e28dbaa16b4987`, `d03e144544ab25371b71db64538c0de47ae8381c`, and `fcd49f34ec8b2f0321cc444bf7b3e612ffd963b2`. The intraday/extensibility and multi-participant alignment records remain current review constraints; this Daily-v1 owner slice introduces neither temporal state nor shared-activity state.

**Accepted P12-E design:** `docs/design/PHASE12_E_TECHNICAL_DESIGN.md`, blob `15aaee09d3295cff81a48e166b620c89f5156346` at the P12 base. **P12 Brief/State:** blobs `31d9e1b4df41fc994f0a747274e35c4ef40b6e3a` and `700a1fad188049f23819c0beae23ea4e34e1ad1f`.

**Conflict dependency:** The typed `PersistentConflictStore` owner design at `9ea0e122ce7916ac4f5cd0b5332db6a980c6346d` passed exact-tip independent design review `f8107d770655b7ba7126a499a55f5bebd2dbb768`. Its implementation is not promoted. Revalidate this War design against current canonical and the promoted Conflict implementation before implementation starts; the War factory requires that exact staged Conflict owner.

## 1. Bounded owner slice

Add detached schema-v1 export and private staged reconstruction for the existing `PersistentWarStore` in the accepted `UnityBootstrap-Daily-v1` profile. The selected profile already admits `p12e.wars` as a **Required** owner section with schema 1, exact installed-owner identity, `Count`, and local `Revision`. The authored initial fixture is empty (`Count == 0`, `Revision == 0`); it is not an empty-only promise. Later supported base War records must round-trip exactly.

The War owner carries its existing lifecycle, side, participant-binding, and parent-reference facts. It does not duplicate Conflict or ArmedForce rows, and it does not include Battle state. It consumes the existing P12-B completed-boundary token and owner vector, P12-C roots, P12-D references, and staged E parents. It does not create capture eligibility, mutation invalidation, a second epoch, or another owner.

Daily-v1 explicitly excludes P17-A strategic War extensions, P16 state/provenance, and P17 Faction relationships. If the source War store contains any `PersistentWarRecord.P17A` value, capture rejects the whole War package; it must not omit the extension. The detached DTO has no P17 field, and hydration rejects any unsupported extension or schema rather than dropping it. Existing Daily-v1 admission and P17 rejection remain authoritative. This does not remove P17-A or preclude a future profile-specific War owner design.

## 2. Section, exact stamp, and detached values

Use the existing owner section without changing its identity:

| Field | Required value |
|---|---|
| Section ID / schema | `p12e.wars` / `1` |
| Runtime owner | Exact installed `PersistentWarStore` witnessed by P12-B; process-local identity only, never serialized |
| Cardinality | `PersistentWarStore.Count == detached Records.Count == recordCount` |
| Local revision | Exact nonnegative `PersistentWarStore.Revision`, equal to the P12-B `p12e.wars` witness and same capture vector |
| Top-level ordering | Ordinal `WarId.Value`, matching `PersistentWarStore.Records` |
| Side ordering | Ordinal `WarSideId.Value`, matching `PersistentWarRecord.Sides` |
| Binding ordering | Ordinal `WarParticipantBindingId.Value`, matching `ParticipantBindings` |

Introduce an immutable War-specific value graph (for example `PersistentWarOwnerSnapshot`) with schema, record count, exact local revision, and one detached row per War. Each row contains only scalar/enum/string values:

- `WarId.Value`, `CreatedAbsoluteDay`, exact `WarLifecycleState`, nullable `EndedAbsoluteDay`, and nullable `ConflictId.Value`;
- every side’s parent `WarId.Value`, `WarSideId.Value`, and normalized `DisplayName`;
- every binding’s `WarParticipantBindingId.Value`, parent `WarId.Value`, `WarSideId.Value`, and `ArmedForceId.Value`.

Copy all fields and all child rows. Preserve nullability, exact strings, enum values, and the source’s ordinal ordering. Do not normalize, repair, filter, or recompute records during capture. Lists are defensive, detached, immutable/read-only copies. The DTO contains no live record, typed-ID object, store, provider, runtime, Unity object, P17 section, P16 value, or mutable source collection. Reconstruct typed IDs from their exact `.Value` strings at the owner boundary.

The existing `PersistentWarStoreSnapshot` returned by `CaptureState()` is not this export contract: it retains `PersistentWarRecord` objects and has no section ID, owner witness, or count stamp. Do not serialize or reuse it as the detached continuation value.

## 3. Existing semantics to preserve

The War owner’s existing invariant validator remains authoritative. Validate the complete package before a staged owner is exposed:

- War IDs are nonempty and unique within the War store. `CreatedAbsoluteDay` is nonnegative; lifecycle is a defined enum; Active has no end day; Ended has an end day not before creation.
- Every War has at least two sides. Side IDs are unique within their War, and each side’s `WarId` equals its containing War. There is no two-side maximum in the base War contract.
- Binding IDs are unique within their War. Each binding names its containing War, a side in that War, and a registered ArmedForce in the exact staged `ArmedForceStore`. Binding count has no newly imposed maximum.
- `ConflictId` is optional. Null remains valid; a non-null ID must resolve in the exact staged `PersistentConflictStore`. Preserve the reference as an ID, not a nested Conflict row or reverse link.
- Hydration requires referenced Forces to exist but does not newly require them to remain active. Existing registration may retain bindings to a registered inactive Force. The existing `TryAddParticipantBinding` rule requires an active Force for a newly added binding; that is a write-time rule, not a restore-time rewrite of historical facts.
- Preserve the owner’s existing local ordering and revision exactly. Do not replay register, binding, end, or P17 operations to rebuild rows or revisions.

The DTO and staged factory must use the already accepted Daily-v1 typed profile and parent authorities. P12-G retains global cross-owner stable-ID checks, remaining graph validation, guard binding, and the single publication boundary. War has no F-owned relationship in this slice, so it emits no unresolved F binding as a substitute for validating its own Conflict/Force references.

## 4. Writers, cardinality, and revision effects

The current base War API audit is in `PersistentConflictWarBattleStores.cs` and `PersistentConflictWarBattleContracts.cs`:

| Existing successful path | Effect | Revision | War count |
|---|---|---:|---:|
| `TryRegister` | Inserts one valid base War, with initial sides and optional initial bindings | `+1` once | `+1` |
| `TryAddParticipantBinding` | Replaces an active base War with one additional binding | `+1` once | unchanged |
| `TryEnd` | Replaces an active base War with its ended lifecycle/day | `+1` once | unchanged |
| Failed validation, runtime guard, duplicate, missing parent, invalid day, ended-War write, or revision saturation | No committed change | unchanged | unchanged |

`TryRegister` validates a supplied record before insertion and permits its existing initial binding rules; `TryAddParticipantBinding` validates the new Force as active. `TryEnd` rejects an already-ended War and an end day before creation. `CanAdvance` rejects `long.MaxValue` before a successful write. There is no War row-removal or rollback API in the inspected owner. Preserve these current rules; do not add event-derived revisions or reinterpret a local revision as a P12-B epoch.

P17-only `TryConfigureP17A` and `TryConcedeP17A` are excluded Daily-v1 paths, not added War writers for this DTO. Their semantics and tests stay intact. Do not serialize P17 participants, goals, concessions, Faction references, or P16 provenance.

## 5. Capture and private staging order

1. Consume the same valid completed-boundary token/vector used by the other P12-E owner snapshots. Require one unique Required `p12e.wars` schema-v1 witness whose installed-owner identity, count, and local revision match the runtime’s exact `WarStore` and detached row list. Read the defensive sorted `Records` view once, copy every field, and compare the owner count/revision and witness to the same capture stamp before accepting the private DTO. Any missing, duplicate, substituted, changed, or mismatched evidence rejects the whole capture.
2. This step relies on the existing P12-B owner-thread/quiescence and capture lifecycle contract. It adds no lock, owner-thread rule, revision notification, shared epoch, eligibility token, bootstrap registration, or claim that local revision alone makes a read atomic.
3. Validate the complete DTO before building a candidate: supported schema, exact count/list equality, nonnegative revision and dates, defined lifecycle, active/ended date consistency, unique identities, deterministic ordering, at least two sides, parent matches, unique sides/bindings, exact side membership, optional Conflict resolution, and Force resolution. Reject the package on the first invalid row; never return a partially staged War store.
4. Build only a new private `PersistentWarStore` bound to the exact already-staged ArmedForce and Conflict stores. The typed Conflict dependency is the candidate returned by the reviewed Conflict snapshot factory, not a raw live Conflict, duplicate owner, or unresolved placeholder. Reconstruct the entire immutable value graph, install it with an owner-private staging seam, restore the exact saved revision, and run `ValidateInvariants` before returning it. Failure discards the private candidate and leaves source, staged parents, and active runtime unchanged.
5. Preserve the existing dependency order: P12-C roots and P12-D references; staged `ArmedForceStore`; staged `PersistentConflictStore`; this `PersistentWarStore`; then `PersistentBattleStore`. Battle remains downstream and validates its own War links against this exact staged instance. No War/Battle cross-facts are duplicated.
6. The Conflict design has passed review but its implementation is not yet canonical. Before implementation, refresh canonical and revalidate that the promoted Conflict snapshot API produces the typed staged owner and exact parent identity this design requires. If the Conflict API or War source changed semantically, update and independently re-review this design before code. Do not create a reverse D/F dependency or wait on P12-F for Conflict/War parent validation.

## 6. Required implementation validation plan

This design prescribes evidence; no tests or Unity validation are run for this docs-only proposal. Implementation should add a focused War owner snapshot/factory suite and retain current War/P17 regression coverage. It must prove:

- Exact empty Daily-v1 `p12e.wars` identity/schema/count/revision capture and round trip; repeated capture preserves the same owner witness. Empty bootstrap is only the initial fixture.
- Populated round trips preserve multiple War IDs, Active and Ended records, all dates/enums, null and valid Conflict references, two-or-more sides, optional bindings, exact display names and ordering, exact count and revision. Include a proving fixture with more than two sides or bindings to prevent accidental P17 cardinality from leaking into base War rules.
- A valid optional Conflict-null War succeeds; a supplied Conflict resolves only against the exact staged Conflict. Registered inactive Forces remain valid for historical bindings, while the existing new-binding active-Force rule remains a domain write rule.
- Successful `TryRegister`, `TryAddParticipantBinding`, and `TryEnd` retain their count/revision effects. Duplicate IDs, missing/wrong Conflict or Force, malformed parent/side/binding identity, too few sides, repeated side/binding, post-end write, invalid end day, faulted guard, and revision saturation preserve the source record values, count, and revision on rejection.
- Export detaches all row/child values from source collections; later source-side writes do not alter a previously captured DTO. Hydration reconstructs typed IDs and exact staged parent references without aliasing source runtime objects.
- Unknown schema, negative/inconsistent stamp, count/list mismatch, duplicate or malformed IDs, invalid enum/lifecycle/date, invalid reference, and any P17A state reject before a staged owner is returned. A failed factory leaves the active runtime and already-staged owners unchanged.
- Existing P17-A configured and Daily-v1 rejection tests remain passing. No new P17 state is made persistent by this War base slice.

Run the implementation’s focused War snapshot/store and relevant Conflict/ArmedForce regressions, ALL EditMode, official Smoke 5/5, and cumulative `git diff --check`. Record exact test artifacts against the tested `Assets` tree. This proposal is not test evidence and does not provide P12-E whole-profile parity or P12-G publication.

## 7. File ownership, collisions, and exclusions

A future implementation owns new War-only detached DTO/export code and focused War tests, plus the smallest private staging seam inside the War region of `Assets/_Project/Scripts/PersistentConflictWarBattleStores.cs`. That shared store file is an explicit serialized hotspot: the Conflict, War, and Battle implementations must not concurrently edit it. Integrate the not-yet-promoted Conflict implementation first, refresh canonical, then revalidate/rebase the War candidate against the actual promoted API. Keep all code changes out of `SimulationRuntime`, bootstrap, and P12-B admission.

Exclude P17-A strategy/participants/goals/concessions/Faction references, P16 provenance, Battle rows and outcomes, P12-D facts, P12-F Knowledge/commitments, P12-G whole-graph validation/publication, P12-B token/vector/quiescence implementation, runtime/bootstrap composition, profile changes, save envelope, replay, new gameplay, P12-A, P13, and Phase closure.

**Readiness:** `READY_FOR_INDEPENDENT_TECHNICAL_REVIEW` on exact P12 base `a4ce0ab`; this proposal is not yet `READY_FOR_IMPLEMENTATION`. If independent review passes and the current canonical/source revalidation after Conflict promotion passes, this bounded War owner slice can proceed under the existing P12-E prerequisite authorization. It does not complete P12-E or P12-D, demonstrate whole-profile export/hydration, make P12-A ready, unblock P13, or close Phase 12.
