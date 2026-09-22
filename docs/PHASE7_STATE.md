# Phase 7 — Checkpoint D1 — Current State

## D1 baseline, branch, and commit

- Canonical architecture baseline: `775edc758fcb8e0f0baf6768f2c89190eca403fd`.
- Checkpoint branch: `codex/phase7/BattleSpatialBinding`.
- Implementation commit: recorded after validation; this section is kept
  current with the validated branch commit.

## D1 delivered

Checkpoint D1 makes `PersistentBattle` the first explicit consumer of the D0
world-bound `SpatialAuthority`, without implementing battle resolution,
movement, terrain, or military position validation.

- `PersistentBattleRecord` carries an optional typed D0 `SpatialReference`.
- Pending Battles may omit location or carry a valid Hex, Location, or
  SubLocation reference.
- Pending -> Active requires a valid explicit physical reference. The legacy
  `TryStart` overload only reuses a location already explicitly present on the
  Pending record; it never infers one from participants or runtime objects.
- `PersistentBattleStore` validates Hex, Location anchor, and LocalTopology
  SubLocation references against its bound spatial authority before mutation.
  Failed validation leaves both Battle state and store revision unchanged.
- `SimulationRuntime` composes BattleStore against its cloned world-bound
  `SpatialAuthorityStore`; source authority mutation does not mutate the
  composed runtime authority.
- Active Battle location is immutable in this slice. No move/change-location
  operation, resolution transition, winner, result, casualty, aftermath, or
  Battlefield entity was added.
- Battle diagnostics now project location, canonicalize its stable key, diff
  Pending location changes, format it, and validate missing/malformed or
  unresolved Hex/Location/SubLocation references.

## D1 validation

- D1 focused EditMode: `7/7`.
- Persistent Conflict/War/Battle regression: `8/8`.
- SpatialAuthority D0 regression: `8/8`.
- LocalTopology regression: `17/17`.
- ALL EditMode: `1498/1498`.
- Official EditMode `Smoke`: `5/5`.
- `git diff --check`: clean before commit.

## D1 boundaries and known limitations

The LocalTopology bridge retains its existing transitional RuntimeId-based
owner/place contract. SimulationRuntime clones the spatial authority; the
optional LocalTopology bridge remains the supplied consumer boundary and is
not migrated or redesigned here. Diagnostics remain projections, not a
save/load contract.

`AdvanceDay` was not changed. No Battle starts automatically, no location is
assigned automatically, and no daily spatial or military processing was
added. `docs/SIMULATION_ARCHITECTURE.md` was not changed.

## D1 deferred

City/Site legacy mapping, Crossing references, Hex adjacency, terrain,
Travel rewrite, military movement, participant position validation, Battle
resolution, ConflictFoundation adapters, Battlefield/aftermath, operational
grouping/command, casualties, logistics, recruitment, mobilization,
knowledge, save/load, and broad `Simulation.Core` migration remain deferred.
No next checkpoint is started by D1.


## D0 baseline, branch, and commit

- Canonical architecture baseline: `9e922550d1bd0ed6ca6ddffc1538809960208a10`.
- Checkpoint branch: `codex/phase7/SpatialAuthorityBridge`.
- Implementation commit: `86632ea` — minimal Hex/Location spatial authority,
  typed spatial references, LocalTopology bridge, runtime composition, and
  deterministic diagnostics.
- Follow-up commit: `864c600` — include spatial authority in the human
  diagnostics formatter and its focused regression assertion.

## D0 delivered

Checkpoint D0 adds the smallest authoritative world-bound physical reference
layer without implementing a HexGrid, traversal, pathfinding, or Battle
resolution.

- `HexId` and `LocationId` are stable typed identities independent of Unity,
  `NpcRuntime`, `RuntimeIdAllocator`, discovery order, and insertion order.
- `HexRecord` is identity-only. `LocationRecord` has exactly one immutable
  `AnchorHexId`; registration validates the anchor before mutation.
- `SpatialReference` is typed for Hex, Location, and SubLocation. SubLocation
  references use explicit existing LocalTopology owner/place fields rather than
  an opaque string.
- `SpatialAuthorityStore` owns Hex/Location records, explicit topology-owner
  bindings, deterministic ordering, revision, cloning, resolution, and
  invariant validation. Mutations validate before applying and reject revision
  overflow without partial state changes.
- LocalTopology remains the local topology authority. The bridge resolves
  `SubLocation -> LocalTopology -> Location -> AnchorHex` only when the
  consumer supplies the existing `LocalTopologyStore`; City and ExplorableSite
  were not migrated into Location records.
- `SimulationRuntime` composes a cloned spatial authority without adding daily
  processing or changing `AdvanceDay`.
- Snapshot context, snapshot projection, canonical writer, diff, and invariant
  validation expose only the authoritative spatial records and revision.

## D0 invariants and determinism

Containment, same-Hex, and connectivity remain distinct. Hex adjacency,
traversability, distance, travel time, knowledge, and execution are not
introduced. Moving entities remain separate from Location. No terrain, weather,
travel costs, barriers, crossings, RNG, or Unity object identity was added.
Equivalent authoritative state produces sorted, insertion-order-independent
Hex/Location projections and canonical output.

## D0 validation

- D0 focused EditMode: `8/8`.
- ALL EditMode: `1491/1491`.
- Official EditMode `Smoke`: `5/5`.
- `git diff --check`: clean before documentation update.
- Unity `6000.3.9f1` was run in an isolated validation worktree because the
  primary checkout had an interactive Editor instance open.

## D0 deferred and known limitations

Full HexGrid, adjacency, terrain, barriers, crossings, scale/configuration,
route/travel rewrite, knowledge integration, movement, military use, Battle
location assignment, Battle resolution, aftermath, save/load, and broad
`Simulation.Core` migration remain deferred. Spatial authority does not yet
automatically map legacy City/Site runtime locations; such mapping remains an
explicit future integration. Spatial diagnostics are projections, not a
save/load contract.

`AdvanceDay` was not changed. `docs/SIMULATION_ARCHITECTURE.md` was not changed.

## Historical Checkpoint C — Baseline and branch

## Baseline and branch

- Canonical architecture baseline: `2419d786602e8fa1250df7a0ed7326e236218419`.
- Checkpoint branch: `codex/phase7/ConflictWarBattleState`.
- Implementation commits:
  - `e472b2d` — persistent Conflict/War/Battle contracts, stores, and
    `SimulationRuntime` composition.
  - `83ee003` — diagnostics projections, canonical/diff/formatter/invariants,
    and focused tests.

## Delivered

Checkpoint C adds the minimum persistent world-state boundaries for Conflict,
War, and Battle without implementing a war system or battle resolution.

- Stable typed `ConflictId`, `WarId`, and `BattleId` identities are independent
  of `NpcRuntime`, Unity object discovery, and insertion order.
- Conflict, War, and Battle have separate authoritative stores and separate
  domain-owned side and participant-binding types. There is no global SideId,
  universal actor model, or participant hierarchy.
- The only concrete participant kind introduced is explicit `ArmedForceId`.
  Bindings are owned by their Conflict/War/Battle store, require an existing
  force, and new current bindings require an active force.
- Conflict and War support `Active -> Ended`; Battle supports `Pending -> Active`.
  Battle resolution is explicitly deferred; `Resolved` is representable only
  as a diagnostics/historical enum and cannot be registered or transitioned by
  this checkpoint.
- War optionally references a Conflict. Battle optionally references a War
  and/or Conflict. When both Battle references exist, contradictory War-to-
  Conflict links are rejected.
- Store mutations validate before applying, advance only after successful
  application, and leave state/revision unchanged on failure. Historical
  references to terminated ArmedForces remain representable in registered
  records; newly added bindings cannot target terminated forces.
- Stores are cloned into `SimulationRuntime` against the resolved
  `ArmedForceStore`. Snapshot context, canonical output, human formatter, diff,
  and world invariant validation expose the new state deterministically.
- `ConflictFoundation` and `ConflictResolutionService` remain unchanged and
  lower-level; no adapter was introduced.

## Invariants preserved

Armed Force remains distinct from Faction, Institution, Polity, and generic
Organization. Conflict/War/Battle side identity is domain-owned. Command,
loyalty, allegiance, membership, funding, and control remain distinct.
Person identity remains `PersonId`-based and does not require an
`NpcRuntime`. Physical separation does not imply organizational separation;
no hierarchy propagation is performed for participant bindings.

No event sourcing, DomainEvent additions, RNG, Unity-dependent domain state,
global ad-hoc lists, or military daily processing was added.

## Validation

- Checkpoint C focused tests: `8/8`.
- Focused/regression filter covering ArmedForce diagnostics, runtime
  orchestration, ConflictFoundation, Person, and Population: `245/245`.
- ALL EditMode: `1483/1483`.
- Official EditMode `Smoke` filter: `5/5`.
- `git diff --check`: clean before commit.
- Unity `6000.3.9f1` was used directly in batchmode. The repository wrapper
  was not used because its `Get-CimInstance` process snapshot was denied in
  this environment.

## AdvanceDay and architecture conformance

`SimulationRuntime.AdvanceDay` was not changed. No military processing,
autonomy, cadence, or RNG was added. `docs/SIMULATION_ARCHITECTURE.md` was not
changed. Read-only conformance review found no design contradiction requiring
an architecture decision.

## Deferred

Persistent strategic war behavior, battle resolution/result payloads, tactics,
plans, morale, cohesion, readiness, supply, logistics, funding/pay,
requisition, foraging, movement, scouting, military knowledge, recruitment,
population mobilization accounting, casualties, capture/custody, desertion,
defection, mutiny, military control, occupation, war goals, ceasefire, peace,
taxation, diplomacy, Campaign, Polity, and WarAI remain deferred.

Also deferred are participant kinds beyond ArmedForce, side switching/exit and
re-entry semantics, ancestor/descendant overlap policy, Battle location,
continuity operations for secession/schism/absorption/merger, save/load,
replay, networking, and broad `Simulation.Core` migration.

## Known limitations

The stores are composed at the current `SimulationRuntime` boundary. Snapshot
and canonical output are diagnostics projections, not save/load contracts.
No consumer performs military simulation or modifies population/manpower as a
consequence of these records.

## Recommendation for P7-D

Add the next boundary only after choosing and testing the explicit domain
consumer for these persistent records. Keep participant bindings explicit and
domain-owned, and do not add battle resolution, military daily processing, or
continuity heuristics to the foundation layer.
