# P17-A — Explicit Withdrawal Demand and Explicit War Termination

**Status:** independent technical-design review PASS at content tip
`4088d3485f360144cc60ee301cc4ecb2550ef5bc`; see the
[review record](PHASE17A_EXPLICIT_WITHDRAWAL_TECHNICAL_DESIGN_REVIEW.md).
This design does not implement P17-A. **Architecture base:**
`ffd75652d89d862b83d634868c560f8540869b89`
on `codex/architecture/world-identity-projection`. **Implementation baseline
audited:** promoted `codex/phase16/canonical` at
`75a27d7ac97e66c2762835ccea7950a945c2f20d`. Revalidate the owning
implementation ref, interfaces and integration hotspots before Master starts.
This design is subordinate to architecture §68/§92A, the P17 direction,
Roadmap and Phase 17 Brief. It does not change those authorities.

## Selected profile and completed-boundary example

P17-A composes one pre-existing P7 War, exactly two existing distinct
`FactionId`s, two `WarSideId`s, one existing P7 operational force binding on
each side, and the promoted P16-A one-selected-force/one-passage profile. The
War is already registered in the existing `PersistentWarStore`; the bounded
P17-A strategic relations and A's actual demand are installed as complete
initial World Truth before publication of the runtime and before P16's target
day. The target force is B's bound force in this **one Faction per side**
profile. This association does not become a general force command or allegiance
model.

Example: at initial day 0, A demands B's bound force leave registered Hex H.
The P16-A selected force is at H, with finite carried supply and a target
crossing day 1. At day 1 a controlled scenario/GM crossing input selects the
validated H→J passage; P16 commits position J, supply debit and one retained
receipt. A coherent P17 read derives A's demand achieved from that receipt;
the War remains Active. On a later day, B's explicit controlled scenario/GM
concession ends this two-participant War with reason and input provenance.
Concession is permitted even if the passage failed or A's goal stayed
unfulfilled. No Battle, territorial-control or political mutation occurs.

The profile is `P17AWithdrawalWar`, distinct from `Standard`,
`P16AOneHopMilitary` and P12's `UnityBootstrap-Daily-v1`. It reuses P16's
selected military spatial owner; it does not create another movement Store.
Its effective initial configuration includes one stable scenario/GM authority
ID. The runtime binds an opaque capability for that authority to its composing
host; a request's caller-supplied string alone never proves authority. The
stable authority ID is retained in accepted command provenance and exact
profile state. The immutable P17-A composition/configuration capture includes
that authority ID even before any crossing or concession has occurred, so a
continuation at the initial boundary can reconstruct the same trusted-input
contract. This is a bounded scenario trust contract, not a public
player/mod authorization framework.
The selected profile admits one P17-A War. Other P7 Wars may coexist only if
they preserve their legacy behavior and are not presented as P17-A participants
or goals. The profile restriction is an admission rule for this proof, not a
general War cardinality rule.
Any War carrying the P17-A section is admissible **only** under
`P17AWithdrawalWar`, even without P12 admission. Conversely, that profile
requires exactly one P17-A War, its matched P16-A selected owner/profile and
the trusted scenario/GM authority binding; missing or additional mismatched
profile state rejects construction before runtime publication.

## Owner state and identities

Extend `PersistentWarRecord` inside the existing `PersistentWarStore` with an
optional immutable P17-A strategic section. Its presence marks the bounded
profile, so legacy `TryEnd(WarId, day)` rejects that War before any mutation;
legacy Wars retain their existing P7 end behavior. The section holds:

| Retained War-owned fact | Exact bounded meaning |
|---|---|
| `WarStrategicParticipantId` and relation | Stable typed ID, parent `WarId`, existing `FactionId`, existing `WarSideId`; two distinct Factions on distinct sides. |
| `WarActualGoalId` and withdrawal demand | Stable typed ID, parent `WarId`, owner participant A, opposing participant B, B's `WarParticipantBindingId`, source `HexId` H and initial activation day. One actual goal in this profile. |
| Optional terminal concession | One operation ID, conceding participant B, reason code `Concession`, trusted scenario/GM origin and authority ID, accepted logical day and order, plus the P7 Ended lifecycle/day. |

The strategic participant ID is distinct from `FactionId`, `WarSideId` and
P7's `WarParticipantBindingId`. The goal references the P7 force binding by
ID, resolves its `ArmedForceId` at validation/evaluation and does not copy
force ownership or position into War state. The goal describes departure from
H by a valid P16-A crossing; it does not require a particular destination or
claim control of H. Stable IDs are caller-authored in initial configuration,
not generated from mutable collection position or display name. Participant
and goal lists are sorted ordinally by typed ID for reads, clone and export.

No achieved flag, pressure scalar or derived `War.Winner` is stored. A goal
assessment returns `Pending` or `Achieved` with the matching P16 receipt's
operation ID, boundary and accepted order as evidence. This assessment is a
read-only derivation; P16 retains the crossing receipt, current position and
carried-supply truth. The bounded retained receipt is sufficient because P16-A
permits only one successful crossing. A later multi-leg movement profile must
revisit evidence retention before using this derivation.

## Initial installation and relational validation

Add an initial-only `TryConfigureP17A(WarId, participants, goal, ...)` operation
on the **same** War Store. It requires an already registered Active War and an
unbound/prepublication store. It stages an immutable replacement record and
advances the War revision once only after all checks pass. Invalid input leaves
War records, indexes and revision unchanged. Reconfiguration, duplicate IDs
and runtime addition of a P17-A goal reject; later strategic goal amendments
are outside P17-A. A bounded composition helper may create the source stores,
but is not another War authority.
Existing `TryAddParticipantBinding` also rejects a configured P17-A War, so
its verified one-force-per-side target attribution cannot drift after
publication. P7 binding additions for legacy Wars retain their contract.

Validate atomically: exactly two distinct P7 sides and strategic participants;
registered distinct Factions whose creation days do not exceed the War's day;
participant/side/War parent IDs; one registered active force binding on each
side; target binding belongs to B's side and resolves to P16's selected active
`ArmedForceId`; source Hex H is registered and equals that force's initial P16
position; goal owner A differs from target B; goal activation is initial truth
and strictly precedes P16's target crossing day; the War and P16 day/profile
are compatible. Validate all IDs and relationship cardinality before write.
Do not infer Faction command over the force from any broader P7 binding.

`PersistentWarStore` receives optional Faction, spatial and P16-state
dependencies only for this P17-A section. Existing two-argument P7 construction
remains valid for legacy Wars. Runtime composition must clone `FactionStore`
before the War clone (it currently clones Faction later), then clone/validate
the P17-A War against the **resolved** Faction, spatial and P16 owners. Assign
the resolved Faction to its existing runtime field at the current later point
to minimize political-owner ordering changes. `PersistentBattleStore` still
clones after War. The source and runtime-cloned War stores must each validate
their own references; no cross-world Store reference may survive cloning.

## Crossing input, observation and concession

P17-A selects an **external controlled scenario/GM crossing input**, not an
unrecorded or predetermined internal movement plan. Add a P17-A runtime
crossing entry that owns the application admission sequence and calls P16's
existing crossing authority. The public P16-A-only runtime entry remains
restricted to `P16AOneHopMilitary`; it cannot bypass provenance in the P17-A
composition. The P17-A entry requires the runtime owner thread and exclusive
advance/operation lease, `CurrentDay == P16.TargetBoundaryDay`, a trusted
scenario/GM capability supplied by the composed host rather than trusted from
a caller string, stable operation ID, selected force, source H, registered
destination, explicit traversal option and the P16-required order 0. It
revalidates the same P16 owner/force/passage revisions and supply immediately
before commit. P16 remains the sole mutator of position/supply/receipt.

Extend the P16-A committed receipt/state semantic boundary for this profile
with the accepted request's trusted origin and authority ID. The receipt
already retains operation ID, force, endpoints, option/content/context,
boundary/order, relevant revisions and debit. P16 commits the added provenance
atomically with its existing position/supply/receipt replacement. A failed
crossing produces no accepted world mutation or accepted-input record; a
repeat request is rejected by P16's one-shot guard. Legacy P16-A receipts
remain valid in their own composition but cannot satisfy P17-A's provenance
validation. This avoids a second movement/input authority and prevents a
later persistence adapter from having to guess who caused the crossing.

Provide one bounded immutable capability-oriented P17-A observation API
through `SimulationRuntime`, not a live Store or diagnostic snapshot. On the
owner thread under one operation lease, capture the War section/lifecycle,
target binding and P16 selected state/receipt as one coherent completed-
boundary observation. Return immutable IDs, goal definition, derived status,
evidence and terminal reason/provenance in deterministic order. `Achieved`
requires a committed receipt whose force equals the target binding's force,
source equals H, destination differs from H, boundary is after goal
activation, provenance is valid for P17-A, and whose operation/boundary/order
match the P16 selected profile. Wrong force/source, stale or missing evidence
reports `Pending`; it never writes an achievement flag. Expose a separate
minimal P16 position/supply read in the same observation for the eventual Lab
scenario, through immutable values rather than Store references.

The explicit concession entry is a bounded runtime command, not strategic
AI, diplomatic negotiation or a general public permission system. Its
trusted scenario/GM capability is supplied by the host composition and
validated by identity against the runtime's bound capability. The
payload names War, conceding strategic participant B, a stable operation ID,
reason `Concession`, authority ID and current logical day; the accepted order
is 0 for the War's sole terminal command on that later day. Accept only after
the P16 crossing target day has passed (`CurrentDay > TargetBoundaryDay`),
for the Active two-participant P17-A War, on the runtime owner thread and under
the exclusive operation lease. No achieved-goal condition is required.
Revalidate the participant/side/War relation, current day, mutation guard and
War revision immediately before one immutable War-record replacement. Commit
Ended day and complete concession payload/provenance together in the existing
War Store, advancing its revision once. Null/missing authority, stale expected
revision/day, wrong participant, duplicate operation or already Ended War
reject without altering records, revision or P16 state. P7 raw `TryEnd`
rejects a P17-A War before this operation can be bypassed. The terminal
record itself retains all accepted external causal input for future replay.

The crossing and concession occur on different logical days in this profile.
P16's accepted order 0 and the War terminal order 0 are therefore unambiguous
within their respective day boundaries. No same-day cross-owner ordering rule
or general command scheduler is introduced. The accepted operation IDs and
provenance must survive clone and exact state export. Rejected calls need not
be retained as authoritative state because they have no committed consequence.

## Continuation, P12 and implementation sequence

The P17-A strategic section and terminal input are War-owned reconstruction-
sensitive state. The P16 receipt's new provenance is P16-owned reconstruction-
sensitive state. Initial Factions, War/sides/force bindings, participants,
goal, Hex/passage, selected P16 item/quantity/target day and effective content
are initial World Truth. At a fork before the crossing, no receipt exists and
the goal assesses Pending. At a fork after crossing, the retained receipt
proves Achieved while War may still be Active. At a fork after concession,
the War is Ended with reason and authority. This design supplies semantic
boundaries, not P12 save/load or P13 fork implementation.

The War Store must expose exact immutable capture of the **entire** P17-A War
record and owner revision, not just the new strategic section: existing P7
`WarId`, creation day, optional `ConflictId`, lifecycle/end day, all sides and
operational force bindings, plus strategic participants, actual goal and
terminal concession metadata. A private staged hydration/relational validator
checks the whole draft against P7 Conflict/ArmedForce references and the P17-A
Faction/P16/spatial references, ID uniqueness, lifecycle/terminal consistency,
accepted ordering and revision before publication. Do not publish a partially
validated draft or treat the passive census as semantic export. Clone
preserves the full record and validates it against the cloned Conflict,
ArmedForce, Faction, P16 and spatial owners. P16's existing exact state
capture/hydration validator must include
the new provenance fields and reject missing P17-A provenance when that
composition is selected. P12's passive War census continues to witness count
and revision, but is not exact state coverage.

`UnityBootstrap-Daily-v1` must reject a runtime containing P17-A War state,
even if the caller requests `Standard` composition or omits the P16 owner.
The dedicated `P17AWithdrawalWar` profile cannot be paired with P12 admission.
Add negative admission tests for both attempts before P17-A domain promotion;
do not silently omit the strategic section or P16 provenance from a P12
snapshot. Any edit to P12-B admission hooks is serialized with the active
P12 owner. Full P12/P13/P19/P20 delivery is not a P17-A prerequisite.

Suggested implementation order and owned hotspots:

1. Add immutable P17-A IDs/records and War Store initial configuration,
   validation, raw-end rejection, terminal commit, exact capture/validator and
   clone. Touch `PersistentConflictWarBattleContracts.cs` and
   `PersistentConflictWarBattleStores.cs`; keep P7 standalone tests green.
2. Add P17-A accepted-request provenance to P16 receipt/capture/validator and
   the bounded crossing call. Touch `ArmedForceSpatialPosition.cs`; preserve
   standalone P16-A behavior and one-shot atomic movement.
3. Add explicit runtime composition/admission, Faction clone ordering,
   owner-thread/lease operations and immutable observation in
   `SimulationRuntime.cs`; keep P12 admission integration serial.
4. Add focused EditMode tests and the P12 negative admission proof, then run
   affected P7/P8/P16/P12 regressions and invariant/clone checks. Master owns
   actual implementation/integration scheduling; the General Architect does
   not implement this checkpoint.

Validation must include valid before/crossing/after/concession sequence;
achievement without termination; concession with unmet goal; failed or stale
P16 movement with no War/P16 mutation; wrong force/source/evidence; duplicate
IDs/sides/Factions and missing references; initial-only configuration; stale
concession and repeated end; raw P7 end rejection; Battle outcome without War,
goal or control mutation; guard fault; clone, deterministic read/export,
staged invalid-state rejection; and P12 selected-profile rejection. Exact
external crossing/concession provenance is asserted after clone and in state
capture. Documentation-only design needs no Unity run; code validation is a
later implementation gate.

## Demonstrability and exclusions

`FOLLOW-UP_DEMONSTRATION`: a future non-Unity-capable Lab host should show
active War, A's goal and B's force at H; invoke the real controlled P17-A
crossing entry; show P16 position/supply and goal evidence while War stays
Active; then invoke explicit concession and show Ended reason/provenance.
The bounded immutable runtime read above is the future observation seam.
Current Lab composition is not delivered and is not a checkpoint-code gate;
prefer the scenario before formal Phase 17 closure when available. A later
Master handoff records `HOW TO TRY IT` only when that path exists.

Excluded: autonomous Faction strategy, public player/mod permission policy,
Battle-to-War pressure, territorial control or occupation, ownership or
jurisdiction changes, attrition formula, multiple goals/participants in one
side, participant withdrawal, diplomacy/treaties, ceasefire/peace,
capitulation, Campaign, generic goal engine, P20 Activity, new P14 material
flow, multi-leg military movement, general command journaling, Save and fork.

**Implementation readiness:** `READY_FOR_IMPLEMENTATION` as a reviewed
technical contract against the audited baseline, subject to Master current-
base dependency/hotspot preflight and normal candidate validation/promotion.
No new product decision is required by this design's selected bounded
semantics.
