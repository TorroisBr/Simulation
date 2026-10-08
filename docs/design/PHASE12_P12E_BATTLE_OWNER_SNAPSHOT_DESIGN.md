# P12-E Persistent Battle Owner Snapshot and Staged Factory Design

**Status:** Proposal for independent technical design review. This document is
not implementation authorization and does not mark this slice
`READY_FOR_IMPLEMENTATION`. It adds no checkpoint identity and changes no
architecture or product semantics.

**Exact design base:** P12 canonical `ef0cafb5848cadcf0ac91a3e1af7ff3faaab1367`.
**Architecture authority:** `codex/architecture/world-identity-projection` at
`47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
**Existing passive Battle census:** `PersistentBattleCensusProvider`, section
`p12e.battles`, schema 1, exact installed `PersistentBattleStore` identity,
`Count`, and local `Revision`; its independent census design review is
`docs/design/PHASE12_P12E_BATTLE_CENSUS_DESIGN_REVIEW.md`.

## 1. Bounded contract

This is one owner-local P12-E value export/private staged reconstruction slice
for the existing `PersistentBattleStore` in the accepted
`UnityBootstrap-Daily-v1` profile. It consumes the already-admitted P12-B
completed-boundary token and owner revision vector, and P12-C/D staged roots;
it does not issue or extend those capabilities. The installed runtime's
`p12e.battles` owner witness must match the exact Battle store instance,
schema 1, record count, and local revision used for capture. Bind the detached
value to the same ephemeral capture stamp/revision vector as the rest of that
runtime; do not serialize object identity, a capture token, or a second
invented epoch.

One top-level row is emitted per retained `BattleId` (`Count == rows.Count`).
Rows are ordered ordinally by `BattleId.Value`; sides by `SideId.Value`; and
participant bindings by `BindingId.Value`, matching the current defensive,
sorted `PersistentBattleRecord` views. Every nullable source field remains
nullable in the DTO. No diagnostic subset or omission-as-empty convention is
allowed.

Daily-v1 requires the typed P10 `LocalTopology` section to remain
`NOT_COMPOSED`: do not instantiate a topology store to hydrate Battles. The
Battle store's optional `LocalTopologyStore` dependency is null for this
profile. A Battle `SpatialReferenceKind.SubLocation` cannot resolve against
this composition and must fail closed. Hex, Location, and Crossing references
are encoded by their existing typed identities and validated against their
already-staged spatial authority. P10 Ruin/LocalTopology remains a separate
proving profile. This design does not change P12-D's required-empty
`p12d.explorable-sites` section.

## 2. Detached schema-v1 value

The owner section uses the existing stable section key `p12e.battles` and
schema version 1. Its immutable detached value contains:

| Value | Exact source and encoding |
|---|---|
| Owner section | `sectionId = p12e.battles`, `schemaVersion = 1`; exact runtime owner object identity is checked through the P12-B witness but is not persisted. |
| Owner stamp | `recordCount` and `storeRevision`, equal to the same capture's Battle census witness and P12-B revision-vector entry. |
| Battle rows | One complete row per `PersistentBattleRecord`, ordered by `BattleId.Value`; duplicate/empty IDs reject. |
| Record identity/lifecycle | `BattleId.Value`, `CreatedAbsoluteDay`, nullable `StartedAbsoluteDay`, and exact `BattleLifecycleState` enum value. No allocated runtime object or display string is an identity. |
| Parent references | Nullable `ConflictId.Value` and `WarId.Value`. These remain references, not nested Conflict or War rows. |
| Spatial reference | Null or a tagged `SpatialReference` value: `Kind` plus only its corresponding typed value (`HexId.Value`, `LocationId.Value`, `CrossingId.Value`, or the existing topology owner kind/runtime ID and sub-location runtime ID). Preserve the existing `StableKey` semantics; do not flatten a reference into an untyped location string. |
| Sides | Complete ordered rows of `BattleId.Value`, `BattleSideId.Value`, and `DisplayName`; one row per distinct side identity. The live owner requires at least two sides, matching parent IDs, and unique side IDs within the Battle. |
| Participant bindings | Complete ordered rows of `BattleParticipantBindingId.Value`, `BattleId.Value`, `BattleSideId.Value`, and `ArmedForceId.Value`; one per distinct binding identity. Each binding must name its containing Battle, a side in that Battle, and a staged ArmedForce. |
| Terminal outcome | Null for Pending/Active. For Resolved, exactly one value containing `BattleId.Value`, exact `BattleOutcomeType`, nullable `WinningBattleSideId.Value`, `ResolvedAbsoluteDay`, and the complete accepted provenance below. |
| D5 provenance | Preserve all eight immutable `BattleResolutionProvenance` strings exactly: `PolicyFingerprint`, `NumericExecutionProfileKey`, `ProjectionVersion`, `CausalResolutionFingerprint`, `SourceContextFingerprint`, `CapabilityRuleKey`, `RandomAuthorityRuleKey`, and `ResolverSettingsIdentity`. |
| D6B2 provenance | Preserve the four non-empty accepted strings exactly: `D6B2PolicyFingerprint`, `D6B2PlanSchemaVersion`, `D6B2CoverageVersion`, and `D6B2PlanFingerprint`. |

For a terminal row, preserve a `Victory` winner exactly and require it to be a
registered side; a `Draw` has no winner. The outcome Battle ID must equal the
row ID, its resolved day cannot precede the start day, and D5+D6B2 provenance
must remain complete. No outcome is recomputed, replayed, or applied to Forces
during export or hydration.

The DTO is a value graph only: immutable scalar/enum/string values and
read-only detached child collections. It contains no `PersistentBattleRecord`,
store, live `SpatialReference`/identity object, Force/Conflict/War object,
service, plan, or Unity object references. Reconstruct typed identity values
from their exact `.Value` fields at the owner boundary. Mutating any source
owner after capture cannot change an existing DTO.

## 3. Writers, revision, cardinality, rollback

The source audit maps the current successful writes as follows:

| Existing successful path | Record effect | Store revision | Battle row cardinality |
|---|---|---:|---:|
| `TryRegister` | Adds one new Pending or Active record accepted by existing invariants; normal registration cannot supply a terminal outcome. | `+1` | `+1` |
| `TryAddParticipantBinding` | Replaces one immutable row with the added binding. | `+1` | unchanged |
| `TryStart` | Replaces Pending row with Active row and retains/resolves its physical location. | `+1` | unchanged |
| Prepared terminal install (`TryPrepareTerminalWrite` then `TryCommitTerminalWrite`) | Replaces Active row with its single Resolved row and accepted outcome. | `+1` on commit | unchanged |
| Failed preflight/validation/guard/duplicate/revision-overflow | No committed row change. | unchanged | unchanged |
| Transaction rollback after a terminal row assignment | `RestoreBattleTransactionSnapshot` restores the exact prior immutable record and its exact saved revision. | Restored to prior value; not a monotone epoch. | unchanged |

There is no row removal writer in the inspected store. `CanAdvance` rejects a
successful write at `long.MaxValue`; snapshot validation rejects negative
revision/count, duplicate record IDs, and count/list mismatch. Do not add a
revision or alter rollback, fault, guard, or transaction semantics. The exact
existing post-terminal-assignment exception test is extended to prove that the
old record, owner identity, count, and exact prior revision are restored.

## 4. Capture and staging boundary

1. P12-B supplies the successful completed-boundary token, exact admitted
   provider composition, owner-thread/quiescence guarantee, and coherent
   component revision vector. This slice accepts that evidence and verifies
   the Battle witness is for the runtime-installed owner. It adds no lock,
   second read protocol, shared-epoch callback, bootstrap registration, or
   capture eligibility claim.
2. Capture `PersistentBattleStore.Records` once; this getter already returns
   an ordinally sorted read-only copy of the retained immutable record
   references. Copy every field and child row to detached DTO values, then
   verify the captured `Count`/`Revision` and exact owner witness still match
   the same P12-B capture stamp. A mismatch rejects the entire private capture.
3. Validate the entire DTO before allocating candidates: supported schema,
   scalar ranges/enums, unique stable IDs/cardinalities, ordering, lifecycle,
   complete terminal provenance, exact count, and owner revision stamp.
   Validate the Daily-v1 typed `LocalTopology NOT_COMPOSED` witness; a composed
   or injected `LocalTopologyStore`, any serialized topology section, or a
   SubLocation Battle reference rejects this profile.
4. Build only a private staged owner graph. Dependency order is: P12-C identity
   roots and P12-D spatial roots; already-staged P12-E `ArmedForceStore` and
   `PersistentConflictStore`; already-staged `PersistentWarStore` (including
   its own exact admitted authority); then `PersistentBattleStore` bound to
   those exact stores, the staged `SpatialAuthorityStore`, and null
   `LocalTopologyStore`. Do not construct duplicate parents or duplicate
   Spatial/Force/Conflict/War authorities.
5. Use an internal, side-effect-free owner factory that installs immutable
   Battle rows and the exact saved local revision into the new private store,
   then runs `ValidateInvariants`. Ordinary `TryRegister` cannot hydrate a
   resolved record, and replaying normal lifecycle/terminal operations would
   change revisions and repeat domain work; neither is a valid restore path.
   The factory does not emit events, resolve outcomes, move Forces, mutate War,
   allocate identities, or touch the live runtime. Failed validation discards
   this private candidate.
6. Resolve all Battle-to-Conflict, Battle-to-War, Battle-to-ArmedForce, and
   spatial references against the already-staged owners and preserve existing
   contradictions as rejection. The Battle slice has no P12-F-owned relation,
   so it creates no unresolved-reference exception. P12-E's documented typed
   unresolved cross-owner evidence remains available only for genuinely
   F-owned bindings; P12-G alone performs whole-graph stable-ID uniqueness,
   shared-sequence validation, remaining D/E/F resolution, and publication.
   No unresolved Battle parent/spatial/force reference is passed onward as a
   substitute for required local validity.

## 5. Validation plan and ownership

Implementation review should extend existing Battle tests and add a focused
`PersistentBattleStore` owner snapshot/factory suite. At minimum it must prove:

- Empty snapshot round-trip has explicit `p12e.battles` schema 1, count 0,
  exact local revision and owner stamp; repeated capture has same facts and
  the same live owner identity.
- Populated Pending and Active row round-trips preserve every field, nullable
  reference, side/binding identity and order, cardinality, and local revision.
- A Resolved row round-trips the exact outcome, winner-or-draw semantics,
  resolved day, all eight D5 fields, and all four D6B2 fields without invoking
  outcome application or direct consequences a second time.
- The DTO and its collections remain unchanged after source-side writes;
  the restored owner uses newly constructed typed IDs/value references and is
  bound to the exact staged parents, not the original runtime objects.
- Registration, side/binding/lifecycle cardinality, revision advancement,
  rejected writes, duplicate IDs, missing/wrong parent, Force, side or
  location, contradictory Conflict/War linkage, and revision saturation all
  fail or pass exactly as the existing store contract specifies.
- Terminal transaction rollback after assignment restores the previous row,
  exact revision, count and census owner witness. A failed private factory
  never changes any active runtime value.
- Unknown schema, invalid enums/ranges, duplicate rows/sides/bindings, count
  mismatch, missing/contradictory refs, malformed terminal provenance, and
  malformed lifecycle reject before staging is returned.
- Daily-v1's Battle spatial forms resolve against P12-D/P8 owners; SubLocation,
  injected topology owner, or non-empty LocalTopology section rejects while
  the manifest remains typed `NOT_COMPOSED`. The existing required-empty
  `p12d.explorable-sites` section stays present. P10-A and its own topology
  behavior are neither changed nor declared unsupported in the long term.

This proposal prescribes coverage, not test results. Implementation validation
is owned by the Battle owner implementation and the eventual P12-E integration:
focused Battle/store/export/factory tests; relevant Conflict/War/Force/spatial
regressions; ALL EditMode; official Smoke 5/5; and `git diff --check`. Full
profile parity and publication remain P12-G responsibilities.

## 6. File/hotspot ownership and exclusions

The implementation should own the Battle section of
`Assets/_Project/Scripts/PersistentConflictWarBattleStores.cs` only for its
factory, plus new Battle-only immutable DTO/export code and focused Battle
tests. That source file is shared with `PersistentConflictStore` and
`PersistentWarStore`; **Battle and War edits to the shared file are a serialized
hotspot**. P17-A's `PersistentWarRecord.P17A` meaning, authority, copying,
invariants, and tests remain untouched. The Battle snapshot does not export,
clear, reconstruct, or reinterpret the P17-A War section. Coordinate with P17
owners before any edit that crosses the Battle/War region boundary.

Do not edit `SimulationRuntime`, `TesteSimulacao`, bootstrap composition,
Daily-v1 profile configuration, P12-B token/vector/quiescence or operation
wiring, P12-D City/NPC assembly, P12-G whole-graph validation/publication,
P12-F, the persistence envelope, save/load, restore parity, P12-A, P13,
`SIMULATION_ARCHITECTURE.md`, or any P10/P14/P17 gameplay. Do not add Battle
gameplay, new participant/cardinality semantics, or a LocalTopology owner.

**Readiness:** the existing source, census, and accepted P12-E contracts support
this bounded proposal for independent review. Implementation readiness is
withheld until this exact proposal passes independent design review and the
review confirms current canonical assumptions and the approved P12-E
authorization path. A passing design review would not itself promote code,
complete P12-E, establish whole-profile readiness, make P12-A ready, complete
P12-B, or unblock P13.
